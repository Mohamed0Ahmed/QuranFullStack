namespace QuranDashboard.Api.RateLimiting;

public static class RateLimitRequestClassifier
{
    private const string HealthPathBase = "/api/health";

    public static bool IsHealthRequest(PathString path) =>
        path.StartsWithSegments(HealthPathBase, StringComparison.OrdinalIgnoreCase);
}
