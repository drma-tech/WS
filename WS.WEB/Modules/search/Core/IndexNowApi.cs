using WS.Shared.Models;

namespace WS.WEB.Modules.Search.Core
{
    public class IndexNowApi(IHttpClientFactory factory) : ApiExternal(factory)
    {
        public async Task<HttpResponseMessage?> SendUrls(string api, IndexNowModel payload, CancellationToken cancellationToken)
        {
            return await base.PostAsync<IndexNowModel, HttpResponseMessage>($"public/external/indexnow?url=" + api.ConvertFromStringToBase64(), payload, 
                JavascriptContext.Default.IndexNowModel, null, cancellationToken);
        }
    }
}