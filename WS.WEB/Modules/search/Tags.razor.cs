using Microsoft.AspNetCore.Components;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WS.WEB.Modules.Search
{
    public partial class Tags
    {
        [Parameter][SupplyParameterFromQuery(Name = "language")] public string? Language { get; set; }
        [Parameter][SupplyParameterFromQuery(Name = "platform")] public string? Platform { get; set; }

        public string? Title { get; set; } = "My page title";
        public string? Description { get; set; } = "A short description of the page for search engines and previews.";
        public string? PageUrl { get; set; } = "https://www.example.com/page";
        public string? ImageUrl { get; set; } = "https://www.example.com/og-image.png";
        public string? SiteName { get; set; } = "My Site";
        public string? TwitterSite { get; set; } = "@mysite";
        public string? TwitterCreator { get; set; } = "@author";
        public string OgType { get; set; } = "website";
        public string? Locale { get; set; } = "en_US";
        public string? ThemeColor { get; set; } = "#0d6efd";

        public string? Preview { get; set; }

        private void GenerateTags()
        {
            var sb = new System.Text.StringBuilder();

            // Basic meta
            sb.AppendLine("<!-- Basic meta -->");
            sb.AppendLine($"<title>{HtmlEncode(Title)}</title>");
            sb.AppendLine($"<meta name=\"description\" content=\"{HtmlEncode(Description)}\" />");
            sb.AppendLine($"<meta name=\"theme-color\" content=\"{HtmlEncode(ThemeColor)}\" />");
            sb.AppendLine();

            // Open Graph
            sb.AppendLine("<!-- Open Graph -->");
            sb.AppendLine($"<meta property=\"og:type\" content=\"{OgType}\" />");
            sb.AppendLine($"<meta property=\"og:title\" content=\"{HtmlEncode(Title)}\" />");
            sb.AppendLine($"<meta property=\"og:description\" content=\"{HtmlEncode(Description)}\" />");
            sb.AppendLine($"<meta property=\"og:url\" content=\"{HtmlEncode(PageUrl)}\" />");
            sb.AppendLine($"<meta property=\"og:site_name\" content=\"{HtmlEncode(SiteName)}\" />");
            if (!string.IsNullOrWhiteSpace(ImageUrl))
                sb.AppendLine($"<meta property=\"og:image\" content=\"{HtmlEncode(ImageUrl)}\" />");
            if (!string.IsNullOrWhiteSpace(Locale))
                sb.AppendLine($"<meta property=\"og:locale\" content=\"{HtmlEncode(Locale)}\" />");
            sb.AppendLine();

            // Twitter
            sb.AppendLine("<!-- Twitter Cards -->");
            sb.AppendLine("<meta name=\"twitter:card\" content=\"summary_large_image\" />");
            if (!string.IsNullOrWhiteSpace(TwitterSite)) sb.AppendLine($"<meta name=\"twitter:site\" content=\"{HtmlEncode(TwitterSite)}\" />");
            if (!string.IsNullOrWhiteSpace(TwitterCreator)) sb.AppendLine($"<meta name=\"twitter:creator\" content=\"{HtmlEncode(TwitterCreator)}\" />");
            sb.AppendLine($"<meta name=\"twitter:title\" content=\"{HtmlEncode(Title)}\" />");
            sb.AppendLine($"<meta name=\"twitter:description\" content=\"{HtmlEncode(Description)}\" />");
            if (!string.IsNullOrWhiteSpace(ImageUrl)) sb.AppendLine($"<meta name=\"twitter:image\" content=\"{HtmlEncode(ImageUrl)}\" />");
            sb.AppendLine();

            // Link rel manifest
            sb.AppendLine("<!-- Manifest -->");
            sb.AppendLine("<link rel=\"manifest\" href=\"/en/manifest.webmanifest\" />");
            sb.AppendLine();

            // JSON-LD example
            sb.AppendLine("<!-- Structured data (JSON-LD) example -->");
            var jsonLd = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["@context"] = "https://schema.org",
                ["@type"] = string.Equals(OgType, "article", StringComparison.OrdinalIgnoreCase) ? "Article" : "WebSite",
                ["name"] = Title,
                ["url"] = PageUrl,
                ["description"] = Description,
                ["publisher"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["@type"] = "Organization", ["name"] = SiteName }
            };
            if (!string.IsNullOrWhiteSpace(ImageUrl)) jsonLd["image"] = ImageUrl;

            var json = JsonSerializer.Serialize(jsonLd, new JsonSerializerOptions { WriteIndented = true });
            sb.AppendLine("<script type=\"application/ld+json\">\n" + json + "\n</script>");
            sb.AppendLine();

            // Usage note
            sb.AppendLine("<!-- Paste the tags above into the <head> of your HTML page. -->");

            Preview = sb.ToString();
        }

        private async Task CopyToClipboard()
        {
            if (Preview.Empty())
            {
                await ShowError("No content to copy. Generate tags first.");
                return;
            }

            try
            {
                await JsRuntime.Window().InvokeVoidAsync("navigator.clipboard.writeText", Preview);
                await ShowInfo("Copied to clipboard");
            }
            catch (Exception ex)
            {
                await ProcessException(ex);
            }
        }

        private async Task DownloadTags()
        {
            if (Preview.Empty())
            {
                await ShowError("No content to download. Generate tags first.");
                return;
            }

            try
            {
                await JsRuntime.Utils().DownloadFile("tags.html", "text/html", Preview, Cts.Token);
            }
            catch (Exception ex)
            {
                await ProcessException(ex);
            }
        }

        private static string HtmlEncode(string? value) => HtmlEncoder.Default.Encode(value ?? string.Empty);
    }
}