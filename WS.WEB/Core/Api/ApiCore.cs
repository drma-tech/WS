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

/// <summary>
///
/// </summary>
/// <param name="factory"></param>
/// <param name="key">If data is modified by the user themselves, this key activates version control</param>
/// <param name="extraKeys">keys of other APIs that can be modified by this API</param>
/// <param name="type"></param>
public abstract class ApiCore(IHttpClientFactory factory, string? key, string[] extraKeys, ApiType type)
{
    protected HttpClient LocalHttp => factory.CreateClient("Local");
    protected HttpClient AnonymousHttp => factory.CreateClient("Anonymous");
    protected HttpClient AuthenticatedHttp => factory.CreateClient("Authenticated");

    private HttpClient GetHttp(ApiType type) => type switch
    {
        ApiType.Local => LocalHttp,
        ApiType.Anonymous => AnonymousHttp,
        ApiType.Authenticated => AuthenticatedHttp,
        _ => throw new NotSupportedException(),
    };

    protected static IDictionary<string, int> CacheVersion { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

    public static void ResetCacheVersion()
    {
        CacheVersion = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public static void SetNewVersion(string? key, string[] extraKeys)
    {
        if (key.NotEmpty()) CacheVersion[key] = RandomNumberGenerator.GetInt32(1, 999999);

        foreach (var item in extraKeys)
        {
            CacheVersion[item] = RandomNumberGenerator.GetInt32(1, 999999);
        }
    }

    private Dictionary<string, string> GetVersion()
    {
        if (!CacheVersion.ContainsKey(key!)) CacheVersion[key!] = RandomNumberGenerator.GetInt32(1, 999999);

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "v", CacheVersion[key!].ToString(System.Globalization.CultureInfo.InvariantCulture) } };
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

    protected async Task<bool> GetBoolAsync(string uri, CancellationToken cancellationToken)
    {
        try
        {
            await AppStateStatic.ProcessingStarted.PublishAsync();

            if (key.NotEmpty())
                return await GetHttp(type).GetJsonFromApi<bool>(uri.ConfigureParameters(GetVersion()), cancellationToken);

            return await GetHttp(type).GetJsonFromApi<bool>(uri, cancellationToken);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<byte[]> GetBytesAsync(string uri, RenderControlState<byte[]>? state, CancellationToken cancellationToken)
    {
        try
        {
            if (state != null) await state.StartLoading(null);
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

            if (state != null) await state.FinishLoading(result);

            return result;
        }
        catch (NotificationException ex)
        {
            if (state != null) await state.ShowWarning(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            if (state != null) await state.ShowError(ex.Message);
            throw;
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<T?> GetAsync<T>(string uri, bool setNewVersion, RenderControlState<T>? state, CancellationToken cancellationToken) where T : class
    {
        try
        {
            if (state != null) await state.StartLoading(null);
            await AppStateStatic.ProcessingStarted.PublishAsync();

            if (setNewVersion) SetNewVersion(key, extraKeys);

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

            if (state != null) await state.FinishLoading(result);

            return result;
        }
        catch (NotificationException ex)
        {
            if (state != null) await state.ShowWarning(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            if (state != null) await state.ShowError(ex.Message);
            throw;
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<IEnumerable<T>> GetListAsync<T>(string uri, RenderControlState<IEnumerable<T>>? state, CancellationToken cancellationToken)
    {
        try
        {
            if (state != null) await state.StartLoading(null);
            await AppStateStatic.ProcessingStarted.PublishAsync();

            IEnumerable<T>? result = default;

            if (type == ApiType.Authenticated && !AppStateStatic.IsAuthenticated)
            {
                //return default if user is not authenticated and api requires authentication
            }
            else
            {
                if (key.NotEmpty())
                    result = await GetHttp(type).GetJsonFromApi<IEnumerable<T>>(uri.ConfigureParameters(GetVersion()), cancellationToken);
                else
                    result = await GetHttp(type).GetJsonFromApi<IEnumerable<T>>(uri, cancellationToken);
            }

            if (state != null) await state.FinishLoading(result);
            return result ?? [];
        }
        catch (NotificationException ex)
        {
            if (state != null) await state.ShowWarning(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            if (state != null) await state.ShowError(ex.Message);
            throw;
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task PostAsync(string uri, CancellationToken cancellationToken)
    {
        try
        {
            await AppStateStatic.ProcessingStarted.PublishAsync();

            SetNewVersion(key, extraKeys);

            var response = await GetHttp(type).PostAsync(uri, content: null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new NotificationException(content);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<TOut> PostAsync<TIn, TOut>(string uri, TIn? obj, JsonTypeInfo<TIn?> requestTypeInfo, JsonTypeInfo<TOut?>? responseTypeInfo, RenderControlState<TOut>? state, CancellationToken cancellationToken)
        where TIn : class
        where TOut : class
    {
        try
        {
            if (state != null) await state.StartProcessing(null);
            await AppStateStatic.ProcessingStarted.PublishAsync();

            SetNewVersion(key, extraKeys);

            var response = await GetHttp(type).PostAsJsonAsync(uri, obj, requestTypeInfo, cancellationToken);

            if (typeof(TOut) == typeof(HttpResponseMessage))
            {
                return (TOut)(object)response;
            }

            if (responseTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(responseTypeInfo), "Response type info must be provided for non-HttpResponseMessage types.");
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(responseTypeInfo, cancellationToken) ?? throw new NotificationException("Failed to read response content.");
                if (state != null) await state.FinishProcessing(result);
                return result;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (state != null) await state.ShowError(content);

            throw new NotificationException(content);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<TOut> PutAsync<TIn, TOut>(string uri, TIn? obj, JsonTypeInfo<TIn?> requestTypeInfo, JsonTypeInfo<TOut?> responseTypeInfo, RenderControlState<TOut>? state, CancellationToken cancellationToken)
        where TIn : class
        where TOut : class
    {
        try
        {
            if (state != null) await state.StartProcessing(null);
            await AppStateStatic.ProcessingStarted.PublishAsync();

            SetNewVersion(key, extraKeys);

            var response = await GetHttp(type).PutAsJsonAsync(uri, obj, requestTypeInfo, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync(responseTypeInfo, cancellationToken) ?? throw new NotificationException("Failed to read response content.");
                if (state != null) await state.FinishProcessing(result);
                return result;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (state != null) await state.ShowError(content);

            throw new NotificationException(content);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task DeleteAsync(string uri, CancellationToken cancellationToken)
    {
        try
        {
            await AppStateStatic.ProcessingStarted.PublishAsync();

            SetNewVersion(key, extraKeys);

            var response = await GetHttp(type).DeleteAsync(uri, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NoContent) return;

            if (response.IsSuccessStatusCode) return;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new NotificationException(content);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }
}
