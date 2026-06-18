using WS.Shared.Models;

namespace WS.WEB.Modules.Search.Core
{
    public class IndexNowApi(IHttpClientFactory factory) : ApiExternal(factory)
    {
        public async Task SendUrls(string api, IndexNowModel payload, CancellationToken cancellationToken)
        {
            await base.PostAsync<IndexNowModel, IndexNowModel>($"public/external/indexnow?url=" + api.ConvertFromStringToBase64(), payload, JavascriptContext.Default.IndexNowModel, JavascriptContext.Default.IndexNowModel, cancellationToken);
        }
    }
}