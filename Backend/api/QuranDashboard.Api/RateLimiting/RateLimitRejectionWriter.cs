using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace QuranDashboard.Api.RateLimiting;

public sealed class RateLimitRejectionWriter(IOptions<RateLimitingOptions> options)
{
    public async ValueTask WriteAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var retryAfterSeconds = ResolveRetryAfterSeconds(context);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(ApiMessages.TooManyRequests),
            cancellationToken);
    }

    private int ResolveRetryAfterSeconds(OnRejectedContext context)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            return Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        }

        var limits = options.Value;
        return RateLimitRequestClassifier.IsHealthRequest(context.HttpContext.Request.Path)
            ? limits.HealthWindowSeconds
            : limits.ReplenishmentPeriodSeconds;
    }
}
