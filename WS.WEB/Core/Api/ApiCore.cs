using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization.Metadata;

namespace WS.WEB.Core.Api;

public enum ApiType
{
    Local,
    Anonymous,
    Authenticated,
}

public abstract class ApiCore(IHttpClientFactory factory, string? key, ApiType type)
{
    protected HttpClient LocalHttp => factory.CreateClient("Local");
    protected HttpClient AnonymousHttp => factory.CreateClient("Anonymous");
    protected HttpClient AuthenticatedHttp => factory.CreateClient("Authenticated");

    private HttpClient GetHttp(ApiType type) => type switch
    {
        ApiType.Local => LocalHttp,
        ApiType.Anonymous => AnonymousHttp,
        ApiType.Authenticated => AuthenticatedHttp,
        _ => throw new NotImplementedException()
    };

    protected static Dictionary<string, int> CacheVersion { get; set; } = [];

    public static void ResetCacheVersion()
    {
        CacheVersion = [];
    }

    public static void SetNewVersion(string? key)
    {
        if (key.NotEmpty()) CacheVersion[key] = RandomNumberGenerator.GetInt32(1, 999999);
    }

    private Dictionary<string, string> GetVersion()
    {
        if (!CacheVersion.TryGetValue(key!, out _)) CacheVersion[key!] = RandomNumberGenerator.GetInt32(1, 999999);

        return new Dictionary<string, string> { { "v", CacheVersion[key!].ToString() } };
    }

    protected async Task<string?> GetStringAsync(string uri, CancellationToken cancellationToken)
    {
        try
        {
            await AppStateStatic.ProcessingStarted.PublishAsync();

            if (key.NotEmpty())
                return await GetHttp(type).GetStringAsync(uri.ConfigureParameters(GetVersion()), cancellationToken);
            return await GetHttp(type).GetStringAsync(uri, cancellationToken);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<byte[]> GetBytesAsync(string uri, ComponentActions<byte[]>? actions, CancellationToken cancellationToken)
    {
        try
        {
            if (actions != null) await actions.StartLoading(null);
            await AppStateStatic.ProcessingStarted.PublishAsync();

            byte[] result = [];

            if (type == ApiType.Authenticated && !AppStateStatic.IsAuthenticated)
            {
                //return default if user is not authenticated and api requires authentication
            }
            else
            {
                if (key.NotEmpty()) uri = uri.ConfigureParameters(GetVersion());
                result = await GetHttp(type).GetByteArrayAsync(uri, cancellationToken);
            }

            if (actions != null) await actions.FinishLoading(result);

            return result;
        }
        catch (NotificationException ex)
        {
            if (actions != null) await actions.ShowWarning(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            if (actions != null) await actions.ShowError(ex.Message);
            throw;
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<T?> GetAsync<T>(string uri, bool setNewVersion, ComponentActions<T?>? actions, CancellationToken cancellationToken)
    {
        try
        {
            if (actions != null) await actions.StartLoading(null);
            await AppStateStatic.ProcessingStarted.PublishAsync();

            if (setNewVersion) SetNewVersion(key);

            T? result = default;

            if (type == ApiType.Authenticated && !AppStateStatic.IsAuthenticated)
            {
                //return default if user is not authenticated and api requires authentication
            }
            else
            {
                if (key.NotEmpty())
                    result = await GetHttp(type).GetJsonFromApi<T>(uri.ConfigureParameters(GetVersion()), cancellationToken);
                else
                    result = await GetHttp(type).GetJsonFromApi<T>(uri, cancellationToken);
            }

            if (actions != null) await actions.FinishLoading(result);

            return result;
        }
        catch (NotificationException ex)
        {
            if (actions != null) await actions.ShowWarning(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            if (actions != null) await actions.ShowError(ex.Message);
            throw;
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<HashSet<T>> GetListAsync<T>(string uri, ComponentActions<HashSet<T>>? actions, CancellationToken cancellationToken)
    {
        try
        {
            if (actions != null) await actions.StartLoading(null);
            await AppStateStatic.ProcessingStarted.PublishAsync();

            HashSet<T>? result = default;

            if (type == ApiType.Authenticated && !AppStateStatic.IsAuthenticated)
            {
                //return default if user is not authenticated and api requires authentication
            }
            else
            {
                if (key.NotEmpty())
                    result = await GetHttp(type).GetJsonFromApi<HashSet<T>>(uri.ConfigureParameters(GetVersion()), cancellationToken);
                else
                    result = await GetHttp(type).GetJsonFromApi<HashSet<T>>(uri, cancellationToken);
            }

            if (actions != null) await actions.FinishLoading(result);
            return result ?? [];
        }
        catch (NotificationException ex)
        {
            if (actions != null) await actions.ShowWarning(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            if (actions != null) await actions.ShowError(ex.Message);
            throw;
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<O?> PostAsync<I, O>(string uri, I? obj, JsonTypeInfo<I?> requestTypeInfo, JsonTypeInfo<O?>? responseTypeInfo, CancellationToken cancellationToken)
    {
        try
        {
            await AppStateStatic.ProcessingStarted.PublishAsync();

            SetNewVersion(key);

            var response = await GetHttp(type).PostAsJsonAsync(uri, obj, requestTypeInfo, cancellationToken);

            if (typeof(O) == typeof(HttpResponseMessage))
            {
                return (O)(object)response;
            }

            if (response.StatusCode == HttpStatusCode.NoContent) return default;

            if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync(responseTypeInfo, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NotificationException(content);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<O?> PutAsync<I, O>(string uri, I? obj, JsonTypeInfo<I?> requestTypeInfo, JsonTypeInfo<O?> responseTypeInfo, CancellationToken cancellationToken)
    {
        try
        {
            await AppStateStatic.ProcessingStarted.PublishAsync();

            SetNewVersion(key);

            var response = await GetHttp(type).PutAsJsonAsync(uri, obj, requestTypeInfo, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NoContent) return default;

            if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync(responseTypeInfo, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NotificationException(content);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<T?> DeleteAsync<T>(string uri, JsonTypeInfo<T?> typeInfo, CancellationToken cancellationToken)
    {
        try
        {
            await AppStateStatic.ProcessingStarted.PublishAsync();

            SetNewVersion(key);

            var response = await GetHttp(type).DeleteAsync(uri, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NoContent) return default;

            if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new NotificationException(content);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }
}