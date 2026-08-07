using Microsoft.Azure.Functions.Worker.Http;
using System.Globalization;

namespace WS.API.Core.Auth;

public static class AuthUsersHelper
{
    public static string? GetUserIP(this HttpRequestData req, bool includePort)
    {
        if (req.Headers.TryGetValues("X-Forwarded-For", out var values))
        {
            if (includePort)
                return values.FirstOrDefault()?.Split(',')[0];

            return values.FirstOrDefault()?.Split(',')[0].Split(':')[0];
        }

        if (string.Equals(Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
        {
            return "127.0.0.1";
        }

        return null;
    }

    public static CultureInfo? GetUserCulture(this HttpRequestData req)
    {
        var language = "en";

        if (req.Headers.TryGetValues("Referer", out var referers))
        {
            var referer = referers.FirstOrDefault();

            if (Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            {
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length > 0 && ConfigurationsStatic.SupportedLanguages.Contains(segments[0], StringComparer.OrdinalIgnoreCase))
                {
                    language = segments[0];
                }
            }
        }

        return CultureInfo.GetCultureInfo(language);
    }
}
