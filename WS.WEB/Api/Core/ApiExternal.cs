using System.Text.Json.Serialization.Metadata;

namespace WS.WEB.Api.Core;

public abstract class ApiExternal(IHttpClientFactory factory) : ApiCore(key: null, extraKeys: [])
{
    protected HttpClient AnonymousHttp => factory.CreateClient("Anonymous");

    protected async Task<string?> GetStringAsync(string endpoint, CancellationToken cancellationToken)
    {
        return await GetStringAsync(AnonymousHttp, endpoint, cancellationToken);
    }

    protected async Task<T?> GetAsync<T>(string uri, bool setNewVersion, RenderControlState<T?>[] states, CancellationToken cancellationToken) where T : class
    {
        return await GetAsync(AnonymousHttp, $"public/external?url=" + uri.ConvertFromStringToBase64(), setNewVersion, states, cancellationToken);
    }

    protected async Task<TOut?> PostAsync<TIn, TOut>(string endpoint, TIn? obj, JsonTypeInfo<TIn?> requestTypeInfo, JsonTypeInfo<TOut?>? responseTypeInfo, RenderControlState<TOut?>[] states, CancellationToken cancellationToken)
        where TIn : class
        where TOut : class
    {
        return await PostAsync(AnonymousHttp, endpoint, obj, requestTypeInfo, responseTypeInfo, states, cancellationToken);
    }
}