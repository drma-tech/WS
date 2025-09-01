using HtmlAgilityPack;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace WS.WEB.Modules.Search.Core
{
    public class SitemapHelper(HttpClient http,
        string baseUrl,
        bool includeImages = false,
        bool includeVideos = false,
        bool includeNews = false,
        bool ignoreNoFollow = true,
        string? ignoreTarget = "_blank",
        int maxDepth = 3)
    {
        private readonly Uri _baseUri = new(baseUrl);

        private readonly bool _includeImages = includeImages;
        private readonly bool _includeVideos = includeVideos;
        private readonly bool _includeNews = includeNews;

        private readonly List<PageData> _pages = [];

        public async Task<string?> RunAsync()
        {
            await CrawlAsync(_baseUri.ToString());
            return GenerateSitemap();
        }

        private async Task CrawlAsync(string startUrl)
        {
            var queue = new Queue<(string url, int depth)>();
            var _visited = new HashSet<string>();

            queue.Enqueue((startUrl, 0));

            while (queue.Count > 0)
            {
                var (url, depth) = queue.Dequeue();

                if (_visited.Contains(url) || depth > maxDepth)
                    continue;

                var response = await http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _visited.Add(url);
                    continue;
                }

                var html = await response.Content.ReadAsStringAsync();
                _visited.Add(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var page = new PageData { Url = url };

                if (_includeImages)
                {
                    var images = doc.DocumentNode
                        .SelectNodes("//img[@src]")
                        ?.Select(img => new Uri(_baseUri, img.GetAttributeValue("src", "")).ToString())
                        .Distinct()
                        .ToList() ?? [];
                    page.Images = images;
                }

                if (_includeVideos)
                {
                    var videos = doc.DocumentNode
                        .SelectNodes("//video/source[@src] | //video[@src]")
                        ?.Select(v =>
                            v.GetAttributeValue("src", "") is { } s && !string.IsNullOrWhiteSpace(s)
                                ? new Uri(_baseUri, s).ToString()
                                : null
                        )
                        .Where(s => s != null)
                        .Distinct()
                        .ToList() ?? [];
                    page.Videos = videos;
                }

                if (_includeNews)
                {
                    var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
                    var pubDate = doc.DocumentNode.SelectSingleNode("//meta[@name='pubdate']")?.GetAttributeValue("content", null)
                                  ?? doc.DocumentNode.SelectSingleNode("//meta[@property='article:published_time']")?.GetAttributeValue("content", null);

                    if (pubDate != null)
                    {
                        page.News = new NewsData
                        {
                            Title = title ?? "",
                            PublicationDate = pubDate,
                            PublicationName = _baseUri.Host
                        };
                    }
                }

                _pages.Add(page);

                if (depth == maxDepth)
                    continue;

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
                    foreach (var link in links.Where(link => !_visited.Contains(link)))
                    {
                        queue.Enqueue((link, depth + 1));
                    }
                }
            }
        }

        private string? GenerateSitemap()
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace nsImg = "http://www.google.com/schemas/sitemap-image/1.1";
            XNamespace nsVid = "http://www.google.com/schemas/sitemap-video/1.1";
            XNamespace nsNews = "http://www.google.com/schemas/sitemap-news/0.9";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            var urlset = new XElement(ns + "urlset",
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute(xsi + "schemaLocation",
                    "http://www.sitemaps.org/schemas/sitemap/0.9 " +
                    "http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd")
            );

            if (_includeImages) urlset.Add(new XAttribute(XNamespace.Xmlns + "image", nsImg));
            if (_includeVideos) urlset.Add(new XAttribute(XNamespace.Xmlns + "video", nsVid));
            if (_includeNews) urlset.Add(new XAttribute(XNamespace.Xmlns + "news", nsNews));

            foreach (var page in _pages)
            {
                var urlEl = new XElement(ns + "url",
                    new XElement(ns + "loc", page.Url),
                    new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd"))
                );

                if (_includeImages)
                {
                    foreach (var img in page.Images)
                    {
                        urlEl.Add(new XElement(nsImg + "image",
                            new XElement(nsImg + "loc", img)
                        ));
                    }
                }

                if (_includeVideos)
                {
                    foreach (var vid in page.Videos)
                    {
                        urlEl.Add(new XElement(nsVid + "video",
                            new XElement(nsVid + "content_loc", vid),
                            new XElement(nsVid + "thumbnail_loc", vid + "?thumb=1") // placeholder
                        ));
                    }
                }

                if (_includeNews && page.News != null)
                {
                    urlEl.Add(new XElement(nsNews + "news",
                        new XElement(nsNews + "publication",
                            new XElement(nsNews + "name", page.News.PublicationName),
                            new XElement(nsNews + "language", "en")
                        ),
                        new XElement(nsNews + "publication_date", page.News.PublicationDate),
                        new XElement(nsNews + "title", page.News.Title)
                    ));
                }

                urlset.Add(urlEl);
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
    }

    public class PageData
    {
        public string? Url { get; set; }
        public List<string> Images { get; set; } = [];
        public List<string> Videos { get; set; } = [];
        public NewsData? News { get; set; }
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