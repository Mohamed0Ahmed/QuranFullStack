namespace QuranDashboard.Api.RateLimiting;

/// <summary>
/// Resolves the raw client IP used to partition the rate limiter. Returns the IP only —
/// policy-key namespacing (<c>general:</c>/<c>health:</c>) is the partitioner's concern.
/// </summary>
public interface IClientIpResolver
{
    string Resolve(HttpContext context);
}
