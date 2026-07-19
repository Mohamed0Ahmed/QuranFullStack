using System.Net;

namespace QuranDashboard.Api.RateLimiting;

/// <summary>
/// Resolves the client IP from the configured single-valued header (default <c>X-Real-IP</c>),
/// falling back to the transport <see cref="ConnectionInfo.RemoteIpAddress"/>, then to the
/// <c>"unknown"</c> sentinel. The header is single-valued (Railway <c>X-Real-IP</c>): there is
/// no comma split and no leftmost-of-list logic.
/// </summary>
public sealed class ClientIpResolver(IOptions<RateLimitingOptions> options) : IClientIpResolver
{
    public const string UnknownClient = "unknown";

    private readonly RateLimitingOptions _options = options.Value;

    public string Resolve(HttpContext context)
    {
        var headerName = _options.ClientIpHeaderName;

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
