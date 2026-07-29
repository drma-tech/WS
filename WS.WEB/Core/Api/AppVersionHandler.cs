namespace WS.WEB.Core.Api
{
    public sealed class AppVersionHandler() : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Remove("X-App-Version");

            request.Headers.Add("X-App-Version", AppStateStatic.Version);

            var platform = AppStateStatic.GetSavedPlatform();

            if (request.RequestUri?.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) == true && (platform == null || platform != Platform.webapp))
            {
                throw new UnhandledException("You are using an older version of the app. Please update it via your app store (Microsoft Store, Google Play, Apple App Store, etc.).");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
