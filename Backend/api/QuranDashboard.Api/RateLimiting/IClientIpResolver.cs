namespace QuranDashboard.Api.RateLimiting;

public interface IClientIpResolver
{
    string Resolve(HttpContext context);
}
