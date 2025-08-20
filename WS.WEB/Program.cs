using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Polly;
using Polly.Extensions.Http;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using WS.WEB;
using WS.WEB.Core.Helper;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

if (builder.RootComponents.Empty())
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

ConfigureServices(builder.Services, builder.HostEnvironment.BaseAddress);

var app = builder.Build();

await app.RunAsync();

static void ConfigureServices(IServiceCollection collection, string baseAddress)
{
    collection.AddMudServices();

    collection.AddPWAUpdater();

    collection.AddHttpClient("RetryHttpClient", c => { c.BaseAddress = new Uri(baseAddress); })
        .AddPolicyHandler(request =>
            request.Method == HttpMethod.Get
                ? GetRetryPolicy()
                : Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>());

    collection.AddOptions();
    collection.AddAuthorizationCore();
}

//https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // 408,5xx
        .WaitAndRetryAsync([TimeSpan.FromSeconds(2)]);
}