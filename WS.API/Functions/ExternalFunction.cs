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
    public async Task<HttpResponseMessage> ExternalIndexNow(
        [HttpTrigger(AuthorizationLevel.Anonymous, Method.Post, Route = "public/external/indexnow")] HttpRequestData req, CancellationToken cancellationToken)
    {
        var url = req.GetQueryParameters()["url"]?.ConvertFromBase64ToString() ?? throw new UnhandledException("url null");
        var client = factory.CreateClient("general");

        var body = await req.GetBody<IndexNowModel>(cancellationToken);
        var payload = JsonSerializer.Serialize(body);

        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

        return await client.PostAsync(url, content, cancellationToken);
    }

    [Function("RunSitemap")]
    public async Task RunSitemap(
       [HttpTrigger(AuthorizationLevel.Anonymous, Method.Post, Route = "public/sitemap")] HttpRequestData req, CancellationToken cancellationToken)
    {
        var http = factory.CreateClient();
        //var helper = new SitemapHelper(http, "https://streamingdiscovery.com/en", includeAlternates: false, noIndex: true, ignoreRel: ["nofollow"], ignoreTarget: [], maxDepth: 8);
        var helper = new NewSitemapGenerator(http, "https://streamingdiscovery.com/en");

        var result = await helper.GenerateAsync(true, 8);
    }
}