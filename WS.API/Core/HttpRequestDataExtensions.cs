using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Web;
using WS.API.Core.Auth;
using WS.API.Core.Models;

namespace WS.API.Core;

public static class HttpRequestDataExtensions
{
    public static async Task<T> GetPublicBody<T>(this HttpRequestData req, CancellationToken cancellationToken)
        where T : class, new()
    {
        req.Body.Position = 0; //in case of a previous read
        var model = await JsonSerializer.DeserializeAsync<T>(req.Body, cancellationToken: cancellationToken);
        model ??= new T();

        return model;
    }

    public static async Task<HttpResponseData> CreateResponse<T>(this HttpRequestData req, T? doc, TtlCache maxAge, CancellationToken cancellationToken) where T : class
    {
        var response = req.CreateResponse();

        if (doc != null)
        {
            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(doc, cancellationToken);
        }
        else
        {
            response.StatusCode = HttpStatusCode.NoContent;
        }

        response.Headers.Add("Cache-Control", $"public, max-age={(int)maxAge}");

        return response;
    }

    public static async Task<HttpResponseData> CreateResponse(this HttpRequestData req, Stream? stream, TtlCache maxAge, CancellationToken cancellationToken)
    {
        var response = req.CreateResponse();

        if (stream != null)
        {
            response.StatusCode = HttpStatusCode.OK;
            response.Headers.Add("Content-Type", "application/json");
            await stream.CopyToAsync(response.Body, cancellationToken);
        }
        else
        {
            response.StatusCode = HttpStatusCode.NoContent;
        }

        response.Headers.Add("Cache-Control", $"public, max-age={(int)maxAge}");

        return response;
    }

    public static async Task<HttpResponseData> CreateResponse(this HttpRequestData req, HttpStatusCode status, string msg)
    {
        var response = req.CreateResponse(status);
        await response.WriteStringAsync(msg);
        return response;
    }

    public static async Task<HttpResponseData> CreateResponse<T>(this HttpRequestData req, HttpStatusCode status, T value)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(value);
        return response;
    }

    public static StringDictionary GetQueryParameters(this HttpRequestData req)
    {
        var valueCollection = HttpUtility.ParseQueryString(req.Url.Query);

        var dictionary = new StringDictionary();
        foreach (var key in valueCollection.AllKeys)
            if (key != null)
                dictionary.Add(key.ToLowerInvariant(), valueCollection[key]);

        return dictionary;
    }

    public static void LogError(this HttpRequestData req, Exception? ex)
    {
        var logger = req.FunctionContext.GetLogger(req.FunctionContext.FunctionDefinition.Name);

        var valueCollection = HttpUtility.ParseQueryString(req.Url.Query);
        var version = req.Headers.TryGetValues("X-App-Version", out var values) ? values.FirstOrDefault() : null;

        req.Body.Position = 0; //in case of a previous read

        var log = new LogModel
        {
            Params = string.Join("|", valueCollection.AllKeys.Select(key => $"{key}={req.GetQueryParameters()[key!]}")),
            AppVersion = version,
            Ip = req.GetUserIP(false),
        };

        logger.LogError(ex, "params:{Custom_Params}, version:{Custom_AppVersion}, ip:{Custom_Ip}", log.Params, log.AppVersion, log.Ip);
    }

    public static void LogWarning(this HttpRequestData req, string? message)
    {
        var logger = req.FunctionContext.GetLogger(req.FunctionContext.FunctionDefinition.Name);

        var valueCollection = HttpUtility.ParseQueryString(req.Url.Query);
        var version = req.Headers.TryGetValues("X-App-Version", out var values) ? values.FirstOrDefault() : null;

        var log = new LogModel
        {
            Message = message,
            Params = string.Join("|", valueCollection.AllKeys.Select(key => $"{key}={req.GetQueryParameters()[key!]}")),
            AppVersion = version,
            Ip = req.GetUserIP(false),
        };

        logger.LogWarning("message:{Custom_Message}, params:{Custom_Params}, version:{Custom_AppVersion}, ip:{Custom_Ip}", log.Message, log.Params, log.AppVersion, log.Ip);
    }

    /// <summary>
    /// Ideally, wait two weeks before forcing a version (this gives most users time to update naturally).
    /// </summary>
    private static readonly DateOnly MinimumSupportedVersion = new(2026, 06, 30);

    public static bool IsOutdated(string? version)
    {
        if (version.Empty())
        {
            return true;
        }

        if (version == "loading")
        {
            return false; //todo: force load always the version
        }

        if (!DateOnly.TryParseExact(version, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var clientVersion))
        {
            return true;
        }

        if (clientVersion < MinimumSupportedVersion)
        {
            return true;
        }

        return false;
    }
}

public struct Method
{
    public const string Get = "GET";

    public const string Post = "POST";

    public const string Put = "PUT";

    public const string Delete = "DELETE";
}