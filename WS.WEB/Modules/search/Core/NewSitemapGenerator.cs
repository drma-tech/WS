using HtmlAgilityPack;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace WS.WEB.Modules.Search.Core
{
    public class NewSitemapGenerator(HttpClient http, string baseUrl)
    {
        private readonly HttpClient _http = http ?? throw new ArgumentNullException(nameof(http));
        private readonly Uri _baseUri = new(baseUrl ?? throw new ArgumentNullException(nameof(baseUrl)));

        public async Task<string> GenerateAsync(bool includeAlternates = false, int maxDepth = 5)
        {
            var pages = await CrawlAsync(includeAlternates, maxDepth);
            var json = System.Text.Json.JsonSerializer.Serialize(pages);
            return BuildSitemap(pages, includeAlternates);
        }

        private async Task<Dictionary<string, Page>> CrawlAsync(bool includeAlternates, int maxDepth)
        {
            var pages = new Dictionary<string, Page>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<(string url, int depth)>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            queue.Enqueue((_baseUri.ToString(), 0));

            while (queue.Count > 0)
            {
                var (url, depth) = queue.Dequeue();
                var normKey = NormalizeFullUrl(url);
                if (visited.Contains(normKey) || depth > maxDepth) continue;

                string? html = null;
                html = await FetchHtmlAsync(url);

                visited.Add(normKey);
                if (html == null) continue;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // respect meta robots noindex for inclusion, but still follow links
                var metas = doc.DocumentNode.SelectNodes("//meta[@name]") ?? new HtmlNodeCollection(null);
                var hasNoIndex = metas.Any(m => string.Equals(m.GetAttributeValue("name", ""), "robots", StringComparison.OrdinalIgnoreCase)
                                                 && m.GetAttributeValue("content", "").Contains("noindex", StringComparison.OrdinalIgnoreCase));

                // Extract alternates declared on this page
                var alternates = ExtractAlternates(doc);

                // store page only if not noindex
                if (!hasNoIndex)
                {
                    var normalized = NormalizeFullUrl(url);
                    if (!pages.ContainsKey(normalized))
                        pages[normalized] = new Page { Url = normalized, Alternates = alternates };
                    else
                    {
                        // merge alternates
                        var existing = pages[normalized];
                        existing.Alternates.AddRange(alternates.Where(a => !existing.Alternates.Any(e => string.Equals(e.Href, a.Href, StringComparison.OrdinalIgnoreCase) && string.Equals(e.Hreflang, a.Hreflang, StringComparison.OrdinalIgnoreCase))));
                    }
                }

                if (depth >= maxDepth) continue;

                // Enqueue normal links
                var links = ExtractLinks(doc);
                foreach (var l in links)
                {
                    var lk = NormalizeFullUrl(l);

                    if (!visited.Contains(lk) && !queue.Any(q => string.Equals(NormalizeFullUrl(q.url), lk, StringComparison.OrdinalIgnoreCase)))
                        queue.Enqueue((l, depth + 1));
                }

                // Enqueue alternates if requested
                if (includeAlternates)
                {
                    foreach (var a in alternates)
                    {
                        var ak = NormalizeFullUrl(a.Href);
                        if (!visited.Contains(ak) && !queue.Any(q => string.Equals(NormalizeFullUrl(q.url), ak, StringComparison.OrdinalIgnoreCase)))
                            queue.Enqueue((a.Href, depth + 1));
                    }
                }
            }

            return pages;
        }

        private async Task<string?> FetchHtmlAsync(string url)
        {
            var r = await _http.GetAsync(url);
            if (!r.IsSuccessStatusCode) return null;
            return await r.Content.ReadAsStringAsync();
        }

        private List<Alternate> ExtractAlternates(HtmlDocument doc)
        {
            var nodes = doc.DocumentNode.SelectNodes("//link[@href]") ?? new HtmlNodeCollection(null);
            var list = new List<Alternate>();
            foreach (var n in nodes)
            {
                var rel = n.GetAttributeValue("rel", "");
                if (!(rel ?? string.Empty).Split([' '], StringSplitOptions.RemoveEmptyEntries).Any(r => r.Equals("alternate", StringComparison.OrdinalIgnoreCase)))
                    continue;

                var hreflang = n.GetAttributeValue("hreflang", "")?.Trim();
                var href = n.GetAttributeValue("href", "")?.Trim();
                if (string.IsNullOrWhiteSpace(hreflang) || string.IsNullOrWhiteSpace(href)) continue;

                var abs = new Uri(_baseUri, href);
                if (!string.Equals(abs.Host, _baseUri.Host, StringComparison.OrdinalIgnoreCase)) continue;

                list.Add(new Alternate { Hreflang = hreflang!, Href = NormalizeFullUrl(abs.ToString()) });
            }
            return list;
        }

        private List<string> ExtractLinks(HtmlDocument doc)
        {
            var nodes = doc.DocumentNode.SelectNodes("//a[@href]") ?? new HtmlNodeCollection(null);
            var list = new List<string>();
            foreach (var a in nodes)
            {
                var href = a.GetAttributeValue("href", "")?.Trim();
                if (string.IsNullOrWhiteSpace(href)) continue;
                if (href.StartsWith('#') || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) continue;
                var target = a.GetAttributeValue("target", "");
                if (target == "_blank") continue;
                if (!Uri.TryCreate(_baseUri, href, out var abs)) continue;
                if (!string.Equals(abs.Host, _baseUri.Host, StringComparison.OrdinalIgnoreCase)) continue;
                if (abs.Scheme != Uri.UriSchemeHttp && abs.Scheme != Uri.UriSchemeHttps) continue;

                list.Add(abs.ToString());
            }
            return [.. list.Distinct(StringComparer.OrdinalIgnoreCase)];
        }

        // Normalizes to scheme://host[:port]/path (no query, no fragment) and preserves language segment
        private static string NormalizeFullUrl(string href)
        {
            try
            {
                var u = new Uri(href);
                var scheme = u.Scheme.ToLowerInvariant();
                var host = u.Host.ToLowerInvariant();
                var port = u.IsDefaultPort ? string.Empty : ":" + u.Port;
                var path = u.AbsolutePath.TrimEnd('/');
                if (string.IsNullOrEmpty(path)) path = "/";
                return scheme + "://" + host + port + path;
            }
            catch { return href?.TrimEnd('/') ?? string.Empty; }
        }

        private static string BuildSitemap(Dictionary<string, Page> pages, bool includeAlternates)
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace xhtml = "http://www.w3.org/1999/xhtml";

            var urlset = new XElement(ns + "urlset",
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute(XNamespace.Xmlns + "xhtml", xhtml),
                new XAttribute(xsi + "schemaLocation", "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd")
            );

            foreach (var p in pages.Values)
            {
                var el = new XElement(ns + "url",
                    new XElement(ns + "loc", p.Url),
                    new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd"))
                );

                if (includeAlternates && p.Alternates != null)
                {
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var a in p.Alternates)
                    {
                        var key = (a.Hreflang ?? "") + "|" + (a.Href ?? "");
                        if (seen.Contains(key)) continue;
                        seen.Add(key);
                        el.Add(new XElement(xhtml + "link",
                            new XAttribute("rel", "alternate"),
                            new XAttribute("hreflang", a.Hreflang),
                            new XAttribute("href", a.Href)
                        ));
                    }
                }

                urlset.Add(el);
            }

            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), urlset);
            using var sw = new Utf8StringWriter();
            using var xw = XmlWriter.Create(sw, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true });
            doc.WriteTo(xw);
            xw.Flush();
            return sw.ToString();
        }

        private sealed class Page
        {
            public string? Url { get; set; }
            public List<Alternate> Alternates { get; set; } = [];
        }

        private sealed class Alternate
        {
            public string Hreflang { get; set; } = string.Empty; public string Href { get; set; } = string.Empty;
        }

        private sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }
    }
}