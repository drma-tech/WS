using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using WS.API.Core.Auth;
using System.Diagnostics;
using System.Net;

namespace WS.API.Core;

internal sealed class ApiMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var req = await context.GetHttpRequestDataAsync();
        var sw = Stopwatch.StartNew();

        try
        {
            if (req is null)
            {
                await next(context);
                return;
            }

            var originalUrl = req.Headers.TryGetValues("X-MS-Original-Url", out var urls) ? urls.FirstOrDefault() : null;

            if (originalUrl?.Contains("www.", StringComparison.OrdinalIgnoreCase) == true)
            {
                var culture = req.GetUserCulture();
                var msg = Shared.Translations.Validation.Validations.ResourceManager.GetString(nameof(Shared.Translations.Validation.Validations.DomainDeactivated), culture);

                await context.SetHttpResponseStatusCode(HttpStatusCode.Gone, msg!);
                return;
            }

            if (req.Url.AbsolutePath.Contains("webhook", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            if (req.Url.AbsolutePath.Contains("job", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            var version = req.Headers.TryGetValues("X-App-Version", out var versions) ? versions.FirstOrDefault() : null;

            if (HttpRequestDataExtensions.IsOutdated(version))
            {
                var culture = req.GetUserCulture();
                var msg = Shared.Translations.Validation.Validations.ResourceManager.GetString(nameof(Shared.Translations.Validation.Validations.OutdatedVersion), culture);

                await context.SetHttpResponseStatusCode(HttpStatusCode.UpgradeRequired, string.Format(culture, msg!, version ?? "error"));
                return;
            }

            await next(context);
        }
        catch (NotificationException ex)
        {
            await context.SetHttpResponseStatusCode(HttpStatusCode.BadRequest, ex.Message);
        }
        catch (TaskCanceledException)
        {
            // ignored
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (ObjectDisposedException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            req?.LogError(ex);

            if (string.Equals(ex.Message, "Not Found", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.Equals(ex.Message, "Bad Gateway", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.Equals(ex.Message, "Too Many Requests", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await context.SetHttpResponseStatusCode(HttpStatusCode.InternalServerError, "This request could not be processed.");
        }
        finally
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds > 7000)
            {
                req?.LogWarning($"Executed in {sw.Elapsed}");
            }
        }
    }
}
