using HtmlAgilityPack;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace WS.WEB.Modules.Search.Core
{
    public class SitemapHelper(HttpClient http, string baseUrl, bool ignoreNoFollow = true, string? ignoreTarget = "_blank", int maxDepth = 3)
    {
        private readonly Uri _baseUri = new(baseUrl);

        private readonly List<string> _visited = [];

        public async Task<string?> RunAsync()
        {
            await CrawlAsync(_baseUri.ToString());
            return GenerateSitemap();
        }

        private async Task CrawlAsync(string startUrl)
        {
            var queue = new Queue<(string url, int depth)>();
            _visited.Clear();

            queue.Enqueue((startUrl, 0));

            while (queue.Count > 0)
            {
                var (url, depth) = queue.Dequeue();

                if (_visited.Contains(url) || depth > maxDepth)
                    continue;

                var response = await http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _visited.Add(url + " - error");
                    continue;
                }

                var html = await response.Content.ReadAsStringAsync();
                _visited.Add(url);

                if (depth == maxDepth)
                    continue;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var links = doc.DocumentNode.SelectNodes("//a[@href]")
                    ?.Select(a => new
                    {
                        href = a.GetAttributeValue("href", ""),
                        rel = a.GetAttributeValue("rel", ""),
                        target = a.GetAttributeValue("target", "")
                    })
                    .Where(link =>
                        Uri.TryCreate(_baseUri, link.href, out var absUri)
                        && absUri.Host == _baseUri.Host
                        && (!ignoreNoFollow || !link.rel.Contains("nofollow", StringComparison.OrdinalIgnoreCase))
                        && (ignoreTarget == null || !link.target.Equals(ignoreTarget, StringComparison.OrdinalIgnoreCase))
                    )
                    .Select(link => new Uri(_baseUri, link.href).ToString())
                    .Distinct()
                    .ToList();

                if (links != null)
                {
                    foreach (var link in links)
                    {
                        if (!_visited.Contains(link))
                            queue.Enqueue((link, depth + 1));
                    }
                }
            }
        }

        private string? GenerateSitemap()
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            var sitemap = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "urlset",
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(xsi + "schemaLocation",
                        "http://www.sitemaps.org/schemas/sitemap/0.9 " +
                        "http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"),
                    _visited.Select(url =>
                        new XElement(ns + "url",
                            new XElement(ns + "loc", url),
                            new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd"))
                        )
                    )
                )
            );

            var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };

            using var stringWriter = new Utf8StringWriter();
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                sitemap.WriteTo(xmlWriter);
            }

            return stringWriter.ToString();
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}