using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Xml.Linq;
using WS.Shared.Models;

namespace WS.WEB.Modules.Search
{
    public partial class IndexNow
    {
        [Parameter][SupplyParameterFromQuery(Name = "language")] public string? Language { get; set; }
        [Parameter][SupplyParameterFromQuery(Name = "platform")] public string? Platform { get; set; }

        public string? Api { get; set; } = "https://www.bing.com/indexnow";
        public string? Host { get; set; } = "www.mywebsite.com";
        public string? Key { get; set; } = "123abc";

        public string? SitemapRoute { get; set; }

        private HashSet<string> urls { get; set; } = [];
        private List<string> SitemapUrls { get; set; } = [];
        private MudDataGrid<string>? dataGridRef;
        private string? ManualUrlsText { get; set; }

        private async Task GenerateList()
        {
            SitemapUrls.Clear();

            try
            {
                var client = HttpClientFactory.CreateClient();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                await LoadSitemap(client, SitemapRoute!, seen);
                SitemapUrls = [.. seen.Order(StringComparer.OrdinalIgnoreCase)];
            }
            catch (Exception ex)
            {
                await ShowError(ex.Message);
            }
        }

        private async Task LoadSitemap(HttpClient client, string url, HashSet<string> accumulator)
        {
            var content = await client.GetStringAsync(url, Cts.Token);
            var doc = XDocument.Parse(content);
            if (doc.Root == null) return;
            var rootName = doc.Root.Name.LocalName.ToLowerInvariant();
            if (string.Equals(rootName, "sitemapindex", StringComparison.OrdinalIgnoreCase))
            {
                var sitemapNodes = doc.Root.Elements().Where(e => string.Equals(e.Name.LocalName, "sitemap", StringComparison.OrdinalIgnoreCase));
                foreach (var s in sitemapNodes)
                {
                    var loc = s.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, "loc", StringComparison.OrdinalIgnoreCase))?.Value?.Trim();
                    if (!string.IsNullOrWhiteSpace(loc) && !accumulator.Contains(loc))
                    {
                        await LoadSitemap(client, loc, accumulator);
                    }
                }
                return;
            }

            var urlList = doc.Root.Elements().Where(e => string.Equals(e.Name.LocalName, "url", StringComparison.OrdinalIgnoreCase));
            foreach (var u in urlList)
            {
                var loc = u.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, "loc", StringComparison.OrdinalIgnoreCase))?.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(loc)) accumulator.Add(loc);
            }
        }

        private async Task SendToIndexNowGrid()
        {
            urls = dataGridRef!.Selection;
            await SendToIndexNow();
        }

        private async Task SendToIndexNowUrls()
        {
            urls.Clear();

            if (string.IsNullOrWhiteSpace(ManualUrlsText)) return;
            var parts = ManualUrlsText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var t = p.Trim();
                if (string.IsNullOrWhiteSpace(t)) continue;
                if (Uri.TryCreate(t, UriKind.Absolute, out var u) && (string.Equals(u.Scheme, "http", StringComparison.OrdinalIgnoreCase) || string.Equals(u.Scheme, "https", StringComparison.OrdinalIgnoreCase)))
                {
                    urls.Add(t);
                }
            }
            await SendToIndexNow();
        }

        private async Task SendToIndexNow()
        {
            try
            {
                try
                {
                    if (urls.Empty())
                    {
                        await ShowWarning("Provide at least one URL to submit.");
                        return;
                    }
                    if (Api == null)
                    {
                        await ShowError("IndexNow API is not configured.");
                        return;
                    }

                    var payload = new IndexNowModel { host = Host, key = Key, urlList = urls };

                    var response = await IndexNowApi.SendUrls(Api, payload, Cts.Token);

                    if (response?.StatusCode == System.Net.HttpStatusCode.OK)
                        await ShowSuccess(response?.ReasonPhrase ?? "Request sent");
                    else
                        await ShowError(response?.ReasonPhrase ?? "Something get wrong");
                }
                catch (Exception ex)
                {
                    await ShowError(ex.Message);
                }
            }
            catch (Exception ex)
            {
                await ShowError(ex.Message);
            }
        }
    }
}