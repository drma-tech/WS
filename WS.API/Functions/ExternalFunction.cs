using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

namespace WS.API.Functions;

public class ExternalFunction(IHttpClientFactory factory)
{
    [Function("ExternalGet")]
    public async Task<HttpResponseData> ExternalGet(
        [HttpTrigger(AuthorizationLevel.Anonymous, Method.Get, Route = "public/external")] HttpRequestData req, CancellationToken cancellationToken)
    {
        var url = req.GetQueryParameters()["url"]?.ConvertFromBase64ToString() ?? throw new UnhandledException("url null");
        var client = factory.CreateClient("general");

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await req.CreateResponse(stream, TtlCache.OneDay, cancellationToken);
    }

    [Function("ExternalIndexNow")]
    public async Task ExternalIndexNow(
        [HttpTrigger(AuthorizationLevel.Anonymous, Method.Post, Route = "public/external/indexnow")] HttpRequestData req, CancellationToken cancellationToken)
    {
        var url = req.GetQueryParameters()["url"]?.ConvertFromBase64ToString() ?? throw new UnhandledException("url null");
        var client = factory.CreateClient("general");

        var body = await req.GetPublicBody<IndexNowModel>(cancellationToken);
        var payload = JsonSerializer.Serialize(body);

        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

        await client.PostAsync(url, content, cancellationToken);
    }
}