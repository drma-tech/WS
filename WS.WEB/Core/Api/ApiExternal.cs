namespace WS.WEB.Core.Api;

public abstract class ApiExternal(IHttpClientFactory factory) : ApiCore(factory, null, [], ApiType.Anonymous)
{
    protected new async Task<T?> GetAsync<T>(string uri, bool setNewVersion, RenderControlState<T>? actions, CancellationToken cancellationToken) where T : class
    {
        return await base.GetAsync<T>($"public/external?url=" + uri.ConvertFromStringToBase64(), setNewVersion, actions, cancellationToken);
    }
}
