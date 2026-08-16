using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using WS.WEB.Modules.Features.Core;

namespace WS.WEB.Modules.Features
{
    public partial class Sitemap
    {
        [Parameter][SupplyParameterFromQuery(Name = "language")] public string? Language { get; set; }
        [Parameter][SupplyParameterFromQuery(Name = "platform")] public string? Platform { get; set; }

        public string? Search { get; set; }

        private int MaxDepth { get; set; } = 8;
        private IReadOnlyCollection<string> IgnoreRel { get; set; } = ["nofollow"];
        private IReadOnlyCollection<string> IgnoreTarget { get; set; } = ["_blank"];

        private Uri? _baseUri { get; set; }
        private string? ResultXml { get; set; }
        private bool IncludeAlternates { get; set; }

        private async Task KeyPress(KeyboardEventArgs args)
        {
            if (Search.Empty()) return;

            if (string.Equals(args.Key, "Enter", StringComparison.OrdinalIgnoreCase))
            {
                await StartCrawling();
            }
        }

        private async Task StartCrawling()
        {
            try
            {
                if (!Uri.TryCreate(Search, UriKind.Absolute, out var uri))
                {
                    await ShowError("Invalid URL");
                    return;
                }

                _baseUri = uri;
                ResultXml = null;

                var http = HttpClientFactory.CreateClient();
                //var helper = new SitemapHelper(http, Search, includeAlternates: IncludeAlternates, noIndex: true, ignoreRel: IgnoreRel?.ToList(), ignoreTarget: IgnoreTarget?.ToList(), maxDepth: MaxDepth);
                var helper = new NewSitemapGenerator(http, Search);

                await AppStateStatic.ProcessingStarted.PublishAsync();
                ResultXml = await helper.GenerateAsync(IncludeAlternates, MaxDepth);
                await AppStateStatic.ProcessingFinished.PublishAsync();

                await JsRuntime.Utils().DownloadFile("sitemap.xml", "application/xml", ResultXml, Cts.Token);
            }
            catch (Exception ex)
            {
                await ProcessException(ex);
            }
        }
    }
}