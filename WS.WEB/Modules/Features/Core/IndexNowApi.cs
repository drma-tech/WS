using WS.Shared.Models;
using WS.WEB.Api.Core;

namespace WS.WEB.Modules.Features.Core
{
    public class IndexNowApi(IHttpClientFactory factory) : ApiExternal(factory)
    {
        public async Task<HttpResponseMessage?> SendUrls(string api, IndexNowModel payload, CancellationToken cancellationToken)
        {
            return await base.PostAsync<IndexNowModel, HttpResponseMessage>($"public/external/indexnow?url=" + api.ConvertFromStringToBase64(), payload,
                JavascriptContext.Default.IndexNowModel, responseTypeInfo: null, states: [], cancellationToken);
        }
    }
}