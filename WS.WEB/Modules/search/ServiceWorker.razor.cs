using Microsoft.AspNetCore.Components;
using System.Text;

namespace WS.WEB.Modules.Search
{
    public partial class ServiceWorker
    {
        [Parameter][SupplyParameterFromQuery(Name = "language")] public string? Language { get; set; }
        [Parameter][SupplyParameterFromQuery(Name = "platform")] public string? Platform { get; set; }

        public string CacheName { get; set; } = "site-cache";
        public string Version { get; set; } = "v1";
        public string AssetsText { get; set; } = "/\n/index.html\n/styles.css\n/app.js";
        public string Strategy { get; set; } = "cache-first";
        public string? OfflinePage { get; set; }

        public string? PreviewScript { get; set; }

        private void GenerateScript()
        {
            var assets = AssetsText?.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? [];
            var cacheKey = $"{CacheName}-{Version}";
            var sb = new StringBuilder();

            sb.AppendLine("const CACHE_NAME = '" + cacheKey + "';");
            sb.AppendLine("const ASSETS_TO_CACHE = [");
            foreach (var a in assets)
            {
                sb.AppendLine("  '" + a.Replace("'", "\\'") + "',");
            }
            sb.AppendLine("];\n");

            sb.AppendLine("self.addEventListener('install', event => {");
            sb.AppendLine("  event.waitUntil((async () => {");
            sb.AppendLine("    const cache = await caches.open(CACHE_NAME);");
            sb.AppendLine("    await cache.addAll(ASSETS_TO_CACHE);");
            sb.AppendLine("  })());");
            sb.AppendLine("});\n");

            sb.AppendLine("self.addEventListener('activate', event => {");
            sb.AppendLine("  event.waitUntil((async () => {");
            sb.AppendLine("    const keys = await caches.keys();");
            sb.AppendLine("    await Promise.all(keys.map(k => { if (k !== CACHE_NAME) return caches.delete(k); }));");
            sb.AppendLine("  })());");
            sb.AppendLine("});\n");

            // Fetch handler based on strategy
            if (string.Equals(Strategy, "network-first", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("self.addEventListener('fetch', event => {");
                sb.AppendLine("  event.respondWith((async () => {");
                sb.AppendLine("    try { const response = await fetch(event.request); return response; } catch (err) { return caches.match(event.request); }");
                sb.AppendLine("  })());");
                sb.AppendLine("});\n");
            }
            else if (string.Equals(Strategy, "stale-while-revalidate", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("self.addEventListener('fetch', event => {");
                sb.AppendLine("  event.respondWith((async () => {");
                sb.AppendLine("    const cacheResponse = await caches.match(event.request);");
                sb.AppendLine("    const networkFetch = fetch(event.request).then(resp => { caches.open(CACHE_NAME).then(c => c.put(event.request, resp.clone())); return resp; }).catch(() => {});");
                sb.AppendLine("    return cacheResponse || networkFetch;");
                sb.AppendLine("  })());");
                sb.AppendLine("});\n");
            }
            else // cache-first
            {
                sb.AppendLine("self.addEventListener('fetch', event => {");
                sb.AppendLine("  event.respondWith((async () => {");
                sb.AppendLine("    const cached = await caches.match(event.request);");
                sb.AppendLine("    if (cached) return cached;");
                sb.AppendLine("    const network = await fetch(event.request);");
                sb.AppendLine("    return network;");
                sb.AppendLine("  })());");
                sb.AppendLine("});\n");
            }

            if (!string.IsNullOrWhiteSpace(OfflinePage))
            {
                sb.AppendLine("// Offline fallback for navigation requests");
                sb.AppendLine("self.addEventListener('fetch', event => {");
                sb.AppendLine("  if (event.request.mode === 'navigate') {");
                sb.AppendLine("    event.respondWith((async () => {");
                sb.AppendLine("      try { return await fetch(event.request); } catch (err) { return caches.match('" + OfflinePage.Replace("'", "\\'") + "'); }");
                sb.AppendLine("    })());");
                sb.AppendLine("  }");
                sb.AppendLine("});\n");
            }

            PreviewScript = sb.ToString();
        }

        private async Task DownloadScript()
        {
            try
            {
                GenerateScript();
                var content = PreviewScript ?? string.Empty;
                await JsRuntime.Utils().DownloadFile("service-worker.js", "application/javascript", content, Cts.Token);
            }
            catch (Exception ex)
            {
                await ProcessException(ex);
            }
        }
    }
}