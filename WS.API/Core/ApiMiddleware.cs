using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.Diagnostics;
using System.Net;

namespace WS.API.Core;

internal sealed class ApiMiddleware() : IFunctionsWorkerMiddleware
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

            var version = req.Headers.TryGetValues("X-App-Version", out var values) ? values.FirstOrDefault() : null;

            if (HttpRequestDataExtensions.IsOutdated(version))
            {
                await context.SetHttpResponseStatusCode(
                    HttpStatusCode.UpgradeRequired,
                    $"An outdated version has been detected ({version ?? "error"}). Please update to the latest version to continue using the platform. If you cannot update, try clearing your browser or app cache and reopen it."
                );
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
            await context.SetHttpResponseStatusCode(HttpStatusCode.InternalServerError, "Invocation failed!");
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