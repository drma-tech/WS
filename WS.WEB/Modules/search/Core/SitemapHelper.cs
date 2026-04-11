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

        private static bool IsLanguageSegment(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (s.Length == 2 && s.All(char.IsLetter)) return true; // en, pt, es
            if (s.Length == 5 && s[2] == '-' && char.IsLetter(s[0]) && char.IsLetter(s[1])) return true; // pt-BR
            return false;
        }

        public async Task<string?> RunAsync()
        {
            await CrawlAsync(_baseUri.ToString());
            return GenerateSitemap();
        }

        private async Task CrawlAsync(string startUrl)
        {
            var queue = new Queue<(string url, int depth)>();
            // visited stores normalized visit keys (path without language prefix) to avoid crawling every language variant
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            queue.Enqueue((startUrl, 0));

            while (queue.Count > 0)
            {
                var (url, depth) = queue.Dequeue();
                var visitKey = NormalizeUrlForVisit(url);
                if (visited.Contains(visitKey) || depth > maxDepth)
                    continue;

                var html = await FetchHtmlAsync(url);
                if (html == null)
                {
                    visited.Add(visitKey);
                    continue;
                }

                visited.Add(visitKey);

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

                // do not enqueue alternates for crawling — we only need the alternates declared on pages
                // enqueuing alternate-language pages causes redundant downloads for each language and is not necessary
                // if (_includeAlternates && page.Alternates != null)
                //     EnqueueAlternateUrls(queue, visited, page.Alternates, depth + 1);
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
                if (!(rel ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
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
                if (ignoreNoFollow && rel.Contains("nofollow", StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(abs.ToString());
            }
            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        // Normalize url for visit uniqueness: strip language prefix from path to avoid crawling every language variant
        private string NormalizeUrlForVisit(string url)
        {
            try
            {
                var u = new Uri(url);
                var path = u.AbsolutePath.Trim('/');
                if (string.IsNullOrEmpty(path)) return u.Scheme + "://" + u.Host;
                var segs = path.Split('/');
                // drop first language segment if present
                if (IsLanguageSegment(segs[0]))
                {
                    var rest = string.Join('/', segs.Skip(1));
                    return u.Scheme + "://" + u.Host + "/" + rest;
                }
                return u.Scheme + "://" + u.Host + "/" + string.Join('/', segs);
            }
            catch
            {
                return url;
            }
        }

        private void EnqueueLinks(Queue<(string url, int depth)> queue, HashSet<string> visited, List<string> links, int depth)
        {
            foreach (var link in links)
            {
                var key = NormalizeUrlForVisit(link);
                if (visited.Contains(key) || queue.Any(q => NormalizeUrlForVisit(q.url).Equals(key, StringComparison.OrdinalIgnoreCase))) continue;
                queue.Enqueue((link, depth));
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
            // key: normalized path (without language prefix), value: list of variants (normalized href, hreflang)
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

            string NormalizeHref(string href)
            {
                try
                {
                    var u = new Uri(href);
                    var scheme = u.Scheme.ToLowerInvariant();
                    var host = u.Host.ToLowerInvariant();
                    var port = u.IsDefaultPort ? string.Empty : ":" + u.Port;
                    var path = u.AbsolutePath.TrimEnd('/');
                    if (string.IsNullOrEmpty(path)) path = "/";
                    // intentionally drop query and fragment to avoid duplicates caused by tracking params
                    return scheme + "://" + host + port + path;
                }
                catch
                {
                    return href?.TrimEnd('/') ?? string.Empty;
                }
            }

            // add a variant to a group using normalized href for comparisons
            void AddVariant(string href, string? hreflang)
            {
                if (string.IsNullOrWhiteSpace(href)) return;
                href = href.Trim();
                var key = NormalizePath(href);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<(string, string?)>();
                    groups[key] = list;
                }

                string? normH = null;
                if (!string.IsNullOrWhiteSpace(hreflang))
                    normH = hreflang!.Trim();

                var normHref = NormalizeHref(href);

                // avoid duplicates by normalized href + hreflang
                if (!list.Any(v => NormalizeHref(v.Href).Equals(normHref, StringComparison.OrdinalIgnoreCase)
                                   && string.Equals((v.Hreflang ?? string.Empty).Trim(), (normH ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add((normHref, normH));
                }
            }

            foreach (var p in pagesByUrl.Values)
            {
                if (string.IsNullOrWhiteSpace(p.Url)) continue;
                // include the page itself
                var pageKey = NormalizePath(p.Url!);
                AddVariant(p.Url!, null);
                if (p.Alternates == null) continue;
                foreach (var a in p.Alternates)
                {
                    if (string.IsNullOrWhiteSpace(a.Href)) continue;
                    // include alternates declared on the page; grouping/dedupe happens later
                    AddVariant(a.Href, a.Hreflang);
                }
            }

            // try to infer hreflang for items lacking it by checking alternates pointing to same normalized href
            foreach (var kv in groups.ToList())
            {
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(list[i].Hreflang)) continue;
                    // search across pages for declared hreflang
                    string? found = null;
                    var targetNorm = NormalizeHref(list[i].Href);
                    foreach (var p in pagesByUrl.Values)
                    {
                        if (p.Alternates == null) continue;
                        var f = p.Alternates.FirstOrDefault(x => NormalizeHref(x.Href).Equals(targetNorm, StringComparison.OrdinalIgnoreCase));
                        if (f != null && !string.IsNullOrWhiteSpace(f.Hreflang)) { found = f.Hreflang; break; }
                    }
                    if (found != null)
                    {
                        list[i] = (list[i].Href, found);
                    }
                }

                // remove duplicates and prefer entries that include a hreflang when both
                // hreflang-less and hreflangful variants point to the same normalized href.
                // Also ensure each hreflang appears at most once per group.
                var hrefMap = new Dictionary<string, (string Href, string? Hreflang)>(StringComparer.OrdinalIgnoreCase);
                var seenHreflang = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var v in list)
                {
                    var hre = (v.Hreflang ?? string.Empty).Trim();
                    var hrefNorm = NormalizeHref(v.Href);

                    if (!string.IsNullOrWhiteSpace(hre))
                    {
                        // skip duplicate hreflang values
                        if (seenHreflang.Contains(hre)) continue;
                        seenHreflang.Add(hre);

                        if (hrefMap.TryGetValue(hrefNorm, out var existing))
                        {
                            // prefer entry that has hreflang over one without
                            if (string.IsNullOrWhiteSpace(existing.Hreflang))
                                hrefMap[hrefNorm] = v;
                            // otherwise keep existing (first)
                        }
                        else
                        {
                            hrefMap[hrefNorm] = v;
                        }
                    }
                    else
                    {
                        // add hreflang-less only if there's no entry yet for this href
                        if (!hrefMap.ContainsKey(hrefNorm))
                            hrefMap[hrefNorm] = v;
                    }
                }

                groups[kv.Key] = hrefMap.Values.ToList();
            }

            return groups;
        }

        private void EmitGroup(XElement urlset, XNamespace ns, XNamespace xhtml, List<(string Href, string? Hreflang)> variants)
        {
            if (variants == null || variants.Count == 0) return;

            // Emit one <url> entry per variant. Each entry's <loc> is the variant href and,
            // if alternates are enabled, includes xhtml:link entries for all variants
            // (deduplicated). This ensures every language variant appears as a main URL
            // with its full set of alternates (including itself when it has a hreflang).
            foreach (var current in variants)
            {
                var el = new XElement(ns + "url",
                    new XElement(ns + "loc", current.Href),
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