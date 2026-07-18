namespace QuranDashboard.Api.RateLimiting;

/// <summary>
/// Single source of the health-path rule, shared by the partitioner (to select the health
/// limiter) and the rejection writer (to pick the <c>Retry-After</c> fallback) so the rule is
/// defined once (DRY).
/// </summary>
public static class RateLimitRequestClassifier
{
    private const string HealthPathBase = "/api/health";

    public static bool IsHealthRequest(PathString path) =>
        path.StartsWithSegments(HealthPathBase, StringComparison.OrdinalIgnoreCase);
}
