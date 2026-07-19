using System.Net;

namespace QuranDashboard.Api.RateLimiting;

public sealed class ClientIpResolver(IOptions<RateLimitingOptions> options) : IClientIpResolver
{
    public const string UnknownClient = "unknown";

    private readonly RateLimitingOptions _options = options.Value;

    public string Resolve(HttpContext context)
    {
        var headerName = _options.ClientIpHeaderName;

        // Header is single-valued (Railway X-Real-IP): no comma split / leftmost-of-list logic.
        if (!string.IsNullOrWhiteSpace(headerName)
            && context.Request.Headers.TryGetValue(headerName, out var headerValues))
        {
            var candidate = headerValues.ToString().Trim();
            if (candidate.Length > 0 && IPAddress.TryParse(candidate, out var parsed))
            {
                return parsed.ToString();
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient;
    }
}
