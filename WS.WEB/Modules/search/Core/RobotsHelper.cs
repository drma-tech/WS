using System.Text;

namespace WS.WEB.Modules.Search.Core
{
    public class RobotsRule
    {
        public string UserAgent { get; set; } = "*";
        public List<string> Allow { get; set; } = [];
        public List<string> Disallow { get; set; } = [];

        public string? NewDisallow { get; set; }
        public string? NewAllow { get; set; }

        public Guid Id { get; set; }
    }

    public class RobotsConfig
    {
        public List<RobotsRule> Rules { get; set; } = [];
        public List<string> Sitemaps { get; set; } = [];
    }

    public class RobotsHelper
    {
        //https://www.rfc-editor.org/rfc/rfc9309
        //https://developers.google.com/search/docs/crawling-indexing/robots/robots_txt
        public string Generate(RobotsConfig config)
        {
            var sb = new StringBuilder();

            foreach (var rule in config.Rules)
            {
                sb.AppendLine($"User-agent: {rule.UserAgent}");

                foreach (var path in rule.Disallow)
                    sb.AppendLine($"Disallow: {path}");

                foreach (var path in rule.Allow)
                    sb.AppendLine($"Allow: {path}");

                sb.AppendLine();
            }

            foreach (var sitemap in config.Sitemaps)
                sb.AppendLine($"Sitemap: {sitemap}");

            return sb.ToString();
        }
    }
}