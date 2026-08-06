using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;
using WS.WEB.Modules.Search.Models;

namespace WS.WEB.Modules.Search
{
    public partial class Manifest
    {
        [Parameter][SupplyParameterFromQuery(Name = "language")] public string? Language { get; set; }
        [Parameter][SupplyParameterFromQuery(Name = "platform")] public string? Platform { get; set; }

        public WebAppManifest Model { get; set; } = new();

        public ICollection<string> Icons { get; set; } = [];

        private string NewDisplayOverride = "";
        private string NewCategory = "";
        private string NewIcon = "";
        private string NewRelatedAppPlatform = "";
        private string NewRelatedAppUrl = "";
        private string NewRelatedAppId = "";
        private string NewScreenshotSrc = "";
        private string NewScreenshotType = "";
        private string NewScreenshotSizes = "";
        private string NewScreenshotFormFactor = "";
        private string NewScreenshotLabel = "";
        private string NewClientMode = "";

        public string? PreviewJson { get; set; }
        public string? Search { get; set; }

        private async Task KeyPress(KeyboardEventArgs args)
        {
            if (Search.Empty()) return;

            if (string.Equals(args.Key, "Enter", StringComparison.OrdinalIgnoreCase))
            {
                await PopulateData(Search);
            }
        }

        private async Task PopulateData(string? url)
        {
            using var http = new HttpClient();
            var html = await http.GetStringAsync(Search, Cts.Token);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var manifestNode = doc.DocumentNode.SelectSingleNode("//link[@rel='manifest']");

            if (manifestNode == null)
            {
                await ShowError("No manifest link found in the page.");
                return;
            }

            var manifestUrl = manifestNode.GetAttributeValue("href", "");

            // http.DefaultRequestHeaders.UserAgent.ParseAdd(
            //     "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
            // );

            var json = await http.GetStringAsync(url, Cts.Token);

            Model = JsonSerializer.Deserialize<WebAppManifest>(json, ApiContext.Default.WebAppManifest) ?? new();
        }

        private void GenerateManifest()
        {
            // var manifest = new Dictionary<string, object?>
            // {
            //     { "name", Name },
            //     { "short_name", ShortName },
            //     { "start_url", StartUrl },
            //     { "display", Display },
            //     { "background_color", BackgroundColor },
            //     { "theme_color", ThemeColor },
            //     { "scope", Scope }
            // };

            // if (Icons.Any())
            // {
            //     var icons = Icons.Select(u => new Dictionary<string, object?> { { "src", u }, { "sizes", "512x512" }, { "type", "image/png" } }).ToList();
            //     manifest.Add("icons", icons);
            // }

            PreviewJson = JsonSerializer.Serialize(Model, ApiContext.Default.WebAppManifest);
        }

        private async Task DownloadManifest()
        {
            try
            {
                GenerateManifest();
                var content = PreviewJson ?? string.Empty;
                await JsRuntime.Utils().DownloadFile("manifest.webmanifest", "application/manifest+json", content, Cts.Token);
            }
            catch (Exception ex)
            {
                await ProcessException(ex);
            }
        }
    }
}
