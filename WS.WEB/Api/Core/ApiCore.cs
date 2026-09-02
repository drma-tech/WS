using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization.Metadata;

namespace WS.WEB.Api.Core;

/// <summary>
///
/// </summary>
/// <param name="factory"></param>
/// <param name="key">If data is modified by the user themselves, this key activates version control</param>
/// <param name="extraKeys">keys of other APIs that can be modified by this API</param>
/// <param name="type"></param>
public abstract class ApiCore(string? key, string[] extraKeys)
{
    protected static IDictionary<string, int> CacheVersion { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

    public static void ResetCacheVersion()
    {
        CacheVersion = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public void SetNewVersion()
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

    protected async Task<string?> GetStringAsync(HttpClient http, string uri, CancellationToken cancellationToken)
    {
        try
        {
            await AppStateStatic.ProcessingStarted.PublishAsync();

            if (key.NotEmpty())
                return await http.GetStringAsync(uri.ConfigureParameters(GetVersion()), cancellationToken);

            return await http.GetStringAsync(uri, cancellationToken);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<bool> GetBoolAsync(HttpClient http, string uri, CancellationToken cancellationToken)
    {
        try
        {
            await AppStateStatic.ProcessingStarted.PublishAsync();

            if (key.NotEmpty())
                return await http.GetJsonFromApi<bool>(uri.ConfigureParameters(GetVersion()), cancellationToken);

            return await http.GetJsonFromApi<bool>(uri, cancellationToken);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<byte[]> GetBytesAsync(HttpClient http, string uri, RenderControlState<byte[]>[] states, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var state in states)
            {
                await state.StartLoading(null);
            }
            await AppStateStatic.ProcessingStarted.PublishAsync();

            if (key.NotEmpty()) uri = uri.ConfigureParameters(GetVersion());
            var result = await http.GetByteArrayAsync(uri, cancellationToken);

            foreach (var state in states)
            {
                await state.FinishLoading(result);
            }

            return result;
        }
        catch (NotificationException ex)
        {
            foreach (var state in states)
            {
                await state.ShowWarning(ex.Message);
            }
            throw;
        }
        catch (Exception ex)
        {
            foreach (var state in states)
            {
                await state.ShowError(ex.Message);
            }
            throw;
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<T?> GetAsync<T>(HttpClient http, string uri, bool setNewVersion, RenderControlState<T?>[] states, CancellationToken cancellationToken) where T : class
    {
        try
        {
            foreach (var state in states)
            {
                await state.StartLoading(null);
            }
            await AppStateStatic.ProcessingStarted.PublishAsync();

            if (setNewVersion) SetNewVersion();

            T? result = default;

            if (key.NotEmpty())
                result = await http.GetJsonFromApi<T>(uri.ConfigureParameters(GetVersion()), cancellationToken);
            else
                result = await http.GetJsonFromApi<T>(uri, cancellationToken);

            foreach (var state in states)
            {
                await state.FinishLoading(result);
            }

            return result;
        }
        catch (NotificationException ex)
        {
            foreach (var state in states)
            {
                await state.ShowWarning(ex.Message);
            }
            throw;
        }
        catch (Exception ex)
        {
            foreach (var state in states)
            {
                await state.ShowError(ex.Message);
            }
            throw;
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task<IEnumerable<T>> GetListAsync<T>(HttpClient http, string uri, RenderControlState<IEnumerable<T>>[] states, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var state in states)
            {
                await state.StartLoading(null);
            }
            await AppStateStatic.ProcessingStarted.PublishAsync();

            IEnumerable<T> result;

            if (key.NotEmpty())
                result = await http.GetJsonFromApi<IEnumerable<T>>(uri.ConfigureParameters(GetVersion()), cancellationToken) ?? [];
            else
                result = await http.GetJsonFromApi<IEnumerable<T>>(uri, cancellationToken) ?? [];

            foreach (var state in states)
            {
                await state.FinishLoading(result);
            }
            return result ?? [];
        }
        catch (NotificationException ex)
        {
            foreach (var state in states)
            {
                await state.ShowWarning(ex.Message);
            }
            throw;
        }
        catch (Exception ex)
        {
            foreach (var state in states)
            {
                await state.ShowError(ex.Message);
            }
            throw;
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task PostAsync(HttpClient http, string uri, CancellationToken cancellationToken)
    {
        try
        {
            await AppStateStatic.ProcessingStarted.PublishAsync();

            SetNewVersion();

            var response = await http.PostAsync(uri, content: null, cancellationToken);

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

    protected async Task<TOut?> PostAsync<TIn, TOut>(HttpClient http, string uri, TIn? obj, JsonTypeInfo<TIn?> requestTypeInfo, JsonTypeInfo<TOut?>? responseTypeInfo,
        RenderControlState<TOut?>[] states, CancellationToken cancellationToken)
        where TIn : class
        where TOut : class
    {
        try
        {
            foreach (var state in states)
            {
                await state.StartProcessing(null);
            }
            await AppStateStatic.ProcessingStarted.PublishAsync();

            SetNewVersion();

            var response = await http.PostAsJsonAsync(uri, obj, requestTypeInfo, cancellationToken);

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
                foreach (var state in states)
                {
                    await state.FinishProcessing(result);
                }
                return result;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            foreach (var state in states)
            {
                await state.ShowError(content);
            }

            throw new NotificationException(content);
        }
        finally
        {
            await AppStateStatic.ProcessingFinished.PublishAsync();
        }
    }

    protected async Task DeleteAsync(HttpClient http, string uri, CancellationToken cancellationToken)
    {
        try
        {
            await AppStateStatic.ProcessingStarted.PublishAsync();

            SetNewVersion();

            var response = await http.DeleteAsync(uri, cancellationToken);

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