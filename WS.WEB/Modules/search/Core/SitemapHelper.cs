using HtmlAgilityPack;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace WS.WEB.Modules.Search.Core
{
    public class SitemapHelper(HttpClient http,
        string baseUrl,
        bool includeAlternates = false,
        bool ignoreNoFollow = true,
        string? ignoreTarget = "_blank",
        int maxDepth = 3)
    {
        private readonly Uri _baseUri = new(baseUrl);
        private readonly bool _includeAlternates = includeAlternates;
        private readonly List<PageData> _pages = new();

        public async Task<string?> RunAsync()
        {
            await CrawlAsync(_baseUri.ToString());
            return GenerateSitemap();
        }

        private async Task CrawlAsync(string startUrl)
        {
            var queue = new Queue<(string url, int depth)>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            queue.Enqueue((startUrl, 0));

            while (queue.Count > 0)
            {
                var (url, depth) = queue.Dequeue();
                if (visited.Contains(url) || depth > maxDepth)
                    continue;

                var html = await FetchHtmlAsync(url);
                if (html == null)
                {
                    visited.Add(url);
                    continue;
                }

                visited.Add(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var page = new PageData { Url = url };

                if (_includeAlternates)
                    page.Alternates = ExtractAlternates(doc);

                _pages.Add(page);

                if (depth >= maxDepth)
                    continue;

                // enqueue ordinary links
                var links = ExtractLinks(doc);
                EnqueueLinks(queue, visited, links, depth + 1);

                // enqueue alternates as pages to crawl
                if (_includeAlternates && page.Alternates != null)
                    EnqueueAlternateUrls(queue, visited, page.Alternates, depth + 1);
            }
        }

        private async Task<string?> FetchHtmlAsync(string url)
        {
            try
            {
                var response = await http.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        private List<AlternateData> ExtractAlternates(HtmlDocument doc)
        {
            var nodes = doc.DocumentNode.SelectNodes("//link[@href]") ?? new HtmlNodeCollection(null);
            var list = new List<AlternateData>();
            foreach (var n in nodes)
            {
                var rel = n.GetAttributeValue("rel", "");
                if (!(rel ?? string.Empty).Split([' '], StringSplitOptions.RemoveEmptyEntries)
                        .Any(r => r.Equals("alternate", StringComparison.OrdinalIgnoreCase)))
                    continue;

                var hreflang = n.GetAttributeValue("hreflang", "");
                var href = n.GetAttributeValue("href", "");
                if (string.IsNullOrWhiteSpace(hreflang) || string.IsNullOrWhiteSpace(href))
                    continue;

                try
                {
                    var abs = new Uri(_baseUri, href).ToString();
                    if (!list.Any(a => a.Href.Equals(abs, StringComparison.OrdinalIgnoreCase) && a.Hreflang.Equals(hreflang, StringComparison.OrdinalIgnoreCase)))
                        list.Add(new AlternateData { Hreflang = hreflang, Href = abs });
                }
                catch
                {
                    // ignore invalid hrefs
                }
            }

            return list;
        }

        private List<string> ExtractLinks(HtmlDocument doc)
        {
            var nodes = doc.DocumentNode.SelectNodes("//a[@href]") ?? new HtmlNodeCollection(null);
            var result = new List<string>();
            foreach (var a in nodes)
            {
                var href = a.GetAttributeValue("href", "");
                var rel = a.GetAttributeValue("rel", "");
                if (!Uri.TryCreate(_baseUri, href, out var abs)) continue;
                if (abs.Host != _baseUri.Host) continue;
                if (abs.Scheme != Uri.UriSchemeHttp && abs.Scheme != Uri.UriSchemeHttps) continue;
                if (!ignoreNoFollow && rel.Contains("nofollow", StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(abs.ToString());
            }
            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void EnqueueLinks(Queue<(string url, int depth)> queue, HashSet<string> visited, List<string> links, int depth)
        {
            foreach (var link in links)
            {
                if (visited.Contains(link) || queue.Any(q => q.url.Equals(link, StringComparison.OrdinalIgnoreCase))) continue;
                queue.Enqueue((link, depth));
            }
        }

        private void EnqueueAlternateUrls(Queue<(string url, int depth)> queue, HashSet<string> visited, List<AlternateData> alternates, int depth)
        {
            foreach (var alt in alternates)
            {
                if (string.IsNullOrWhiteSpace(alt.Href)) continue;
                if (!Uri.TryCreate(alt.Href, UriKind.Absolute, out var altUri)) continue;
                if (altUri.Host != _baseUri.Host) continue;
                if (visited.Contains(alt.Href) || queue.Any(q => q.url.Equals(alt.Href, StringComparison.OrdinalIgnoreCase))) continue;
                queue.Enqueue((alt.Href, depth));
            }
        }

        private string? GenerateSitemap()
        {
            //todo: only these two is required
            //<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9" xmlns:xhtml = "http://www.w3.org/1999/xhtml" >

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace xhtml = "http://www.w3.org/1999/xhtml";

            var urlset = new XElement(ns + "urlset",
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute(XNamespace.Xmlns + "xhtml", xhtml),
                new XAttribute(xsi + "schemaLocation", "http://www.sitemaps.org/schemas/sitemap/0.9 " + "http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd")
            );

            var pagesByUrl = BuildPagesByUrl();

            // Build groups based on path (remove language prefix) to avoid mixing alternates from site-wide links
            var groups = BuildGroups(pagesByUrl);
            foreach (var group in groups.Values)
            {
                EmitGroup(urlset, ns, xhtml, group);
            }

            var sitemap = new XDocument(new XDeclaration("1.0", "UTF-8", null), urlset);

            var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };

            using var stringWriter = new Utf8StringWriter();
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                sitemap.WriteTo(xmlWriter);
            }

            return stringWriter.ToString();
        }

        private Dictionary<string, PageData> BuildPagesByUrl()
        {
            return _pages
                .Where(p => !string.IsNullOrWhiteSpace(p.Url))
                .GroupBy(p => p.Url)
                .Select(g => g.First())
                .ToDictionary(p => p.Url!, StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, List<(string Href, string? Hreflang)>> BuildGroups(Dictionary<string, PageData> pagesByUrl)
        {
            // key: normalized path (without language prefix), value: list of variants (href, hreflang)
            var groups = new Dictionary<string, List<(string Href, string? Hreflang)>>(StringComparer.OrdinalIgnoreCase);

            string NormalizePath(string href)
            {
                try
                {
                    var u = new Uri(href);
                    var path = u.AbsolutePath.TrimEnd('/');
                    if (path == string.Empty) return "/";
                    var segs = path.TrimStart('/').Split('/');
                    // if first segment looks like a language code, remove it
                    var first = segs[0];
                    if (IsLanguageSegment(first))
                    {
                        var rest = string.Join('/', segs.Skip(1));
                        return "/" + rest;
                    }
                    return "/" + string.Join('/', segs);
                }
                catch
                {
                    return href;
                }
            }

            bool IsLanguageSegment(string s)
            {
                if (string.IsNullOrEmpty(s)) return false;
                if (s.Length == 2 && s.All(char.IsLetter)) return true; // en, pt, es
                if (s.Length == 5 && s[2] == '-' && char.IsLetter(s[0]) && char.IsLetter(s[1])) return true; // pt-BR
                return false;
            }

            // add a variant to a group
            void AddVariant(string href, string? hreflang)
            {
                var key = NormalizePath(href);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<(string, string?)>();
                    groups[key] = list;
                }
                if (!list.Any(v => v.Href.Equals(href, StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(v.Hreflang ?? string.Empty, hreflang ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
                    list.Add((href, hreflang));
            }

            foreach (var p in pagesByUrl.Values)
            {
                // include the page itself
                var pageKey = NormalizePath(p.Url!);
                AddVariant(p.Url!, null);
                if (p.Alternates == null) continue;
                foreach (var a in p.Alternates)
                {
                    if (string.IsNullOrWhiteSpace(a.Href)) continue;
                    // only include alternates that refer to the same normalized path
                    if (NormalizePath(a.Href) != pageKey) continue;
                    AddVariant(a.Href, a.Hreflang);
                }
            }

            // try to infer hreflang for items lacking it by checking alternates pointing to same href
            foreach (var kv in groups.ToList())
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(list[i].Hreflang)) continue;
                    // search across pages for declared hreflang
                    string? found = null;
                    foreach (var p in pagesByUrl.Values)
                    {
                        if (p.Alternates == null) continue;
                        var f = p.Alternates.FirstOrDefault(x => x.Href.Equals(list[i].Href, StringComparison.OrdinalIgnoreCase));
                        if (f != null && !string.IsNullOrWhiteSpace(f.Hreflang)) { found = f.Hreflang; break; }
                    }
                    if (found != null)
                    {
                        list[i] = (list[i].Href, found);
                    }
                }

                // remove duplicate hreflang entries keeping first occurrence
                var dedup = new List<(string Href, string? Hreflang)>();
                var seenHreflang = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var v in list)
                {
                    var key = v.Hreflang ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        if (!dedup.Any(x => x.Href.Equals(v.Href, StringComparison.OrdinalIgnoreCase)))
                            dedup.Add(v);
                        continue;
                    }
                    if (!seenHreflang.Contains(key))
                    {
                        seenHreflang.Add(key);
                        dedup.Add(v);
                    }
                }
                groups[kv.Key] = dedup;
            }

            return groups;
        }

        private void EmitGroup(XElement urlset, XNamespace ns, XNamespace xhtml, List<(string Href, string? Hreflang)> variants)
        {
            if (variants == null || variants.Count == 0) return;

            // choose canonical: prefer x-default, then en, then first
            var canonical = variants.First().Href;
            var xdef = variants.FirstOrDefault(v => string.Equals(v.Hreflang, "x-default", StringComparison.OrdinalIgnoreCase));
            if (xdef.Href != null && !string.IsNullOrWhiteSpace(xdef.Href)) canonical = xdef.Href;
            else
            {
                var en = variants.FirstOrDefault(v => string.Equals(v.Hreflang, "en", StringComparison.OrdinalIgnoreCase));
                if (en.Href != null && !string.IsNullOrWhiteSpace(en.Href)) canonical = en.Href;
            }

            var el = new XElement(ns + "url",
                new XElement(ns + "loc", canonical),
                new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd"))
            );

            if (_includeAlternates)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var v in variants)
                {
                    if (string.IsNullOrWhiteSpace(v.Hreflang)) continue;
                    var hreflang = v.Hreflang!.Trim();
                    var href = v.Href.Trim();
                    var key = hreflang + "|" + href;
                    if (seen.Contains(key)) continue;
                    seen.Add(key);
                    el.Add(new XElement(xhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", hreflang),
                        new XAttribute("href", href)
                    ));
                }
            }

            urlset.Add(el);
        }

        private List<(string Href, string? Hreflang)> BuildVariantsForComponent(List<string> comp)
        {
            static string? InferHreflangFromUrl(string url)
            {
                try
                {
                    var u = new Uri(url);
                    var path = u.AbsolutePath.Trim('/');
                    if (string.IsNullOrEmpty(path)) return null;
                    var seg = path.Split('/')[0];
                    if (seg.Length == 2 || (seg.Length == 5 && seg.Contains('-')))
                        return seg.ToLowerInvariant();
                }
                catch { }
                return null;
            }

            var variants = new List<(string Href, string? Hreflang)>();
            foreach (var href in comp)
            {
                string? hreflang = null;
                foreach (var p in _pages)
                {
                    if (p.Alternates == null) continue;
                    var found = p.Alternates.FirstOrDefault(a => a.Href.Equals(href, StringComparison.OrdinalIgnoreCase));
                    if (found != null && !string.IsNullOrWhiteSpace(found.Hreflang))
                    {
                        hreflang = found.Hreflang;
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(hreflang))
                    hreflang = InferHreflangFromUrl(href);

                variants.Add((href, hreflang));
            }

            var xDefaultHref = (from p in _pages
                                where p.Alternates != null
                                from a in p.Alternates
                                where a.Hreflang != null && a.Hreflang.Equals("x-default", StringComparison.OrdinalIgnoreCase)
                                && comp.Contains(a.Href, StringComparer.OrdinalIgnoreCase)
                                select a.Href).FirstOrDefault();

            if (xDefaultHref != null)
            {
                var idx = variants.FindIndex(v => v.Href.Equals(xDefaultHref, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                    variants[idx] = (variants[idx].Href, "x-default");
                else
                    variants.Add((xDefaultHref, "x-default"));
            }

            return variants;
        }

        private void EmitComponent(XElement urlset, XNamespace ns, XNamespace xhtml, List<(string Href, string? Hreflang)> variants)
        {
            // To reduce sitemap size, emit a single <url> per hreflang group.
            // Choose canonical href: prefer x-default, then 'en', then first variant.
            if (variants == null || variants.Count == 0) return;

            string canonical = variants.First().Href;
            var xDefault = variants.FirstOrDefault(v => string.Equals(v.Hreflang, "x-default", StringComparison.OrdinalIgnoreCase));
            if (xDefault.Href != null && !string.IsNullOrWhiteSpace(xDefault.Href))
                canonical = xDefault.Href;
            else
            {
                var en = variants.FirstOrDefault(v => string.Equals(v.Hreflang, "en", StringComparison.OrdinalIgnoreCase));
                if (en.Href != null && !string.IsNullOrWhiteSpace(en.Href))
                    canonical = en.Href;
            }

            var el = new XElement(ns + "url",
                new XElement(ns + "loc", canonical),
                new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd"))
            );

            if (_includeAlternates)
            {
                foreach (var v in variants)
                {
                    if (string.IsNullOrWhiteSpace(v.Hreflang)) continue;
                    el.Add(new XElement(xhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", v.Hreflang),
                        new XAttribute("href", v.Href)
                    ));
                }
            }

            urlset.Add(el);
        }
    }

    public class PageData
    {
        public string? Url { get; set; }
        public List<string> Images { get; set; } = new();
        public List<string> Videos { get; set; } = new();
        public List<AlternateData>? Alternates { get; set; }
        public NewsData? News { get; set; }
    }

    public class AlternateData
    {
        public string Hreflang { get; set; } = "";
        public string Href { get; set; } = "";
    }

    public class NewsData
    {
        public string? Title { get; set; }
        public string? PublicationDate { get; set; }
        public string? PublicationName { get; set; }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}