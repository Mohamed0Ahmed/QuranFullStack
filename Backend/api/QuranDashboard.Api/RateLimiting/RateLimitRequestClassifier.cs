namespace QuranDashboard.Api.RateLimiting;

// Single source of the health-path rule, shared by the partitioner and the rejection writer (DRY).
public static class RateLimitRequestClassifier
{
    private const string HealthPathBase = "/api/health";

    public static bool IsHealthRequest(PathString path) =>
        path.StartsWithSegments(HealthPathBase, StringComparison.OrdinalIgnoreCase);
}
