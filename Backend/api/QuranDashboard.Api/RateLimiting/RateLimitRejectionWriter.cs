using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace QuranDashboard.Api.RateLimiting;

/// <summary>
/// Writes the <c>429 Too Many Requests</c> response for rejected requests, mirroring
/// <see cref="Middleware.GlobalExceptionHandler"/>: the shared <see cref="ApiResponse{T}"/>
/// envelope plus a <c>Retry-After</c> header. Options-aware — reads
/// <see cref="RateLimitingOptions"/> for the <c>Retry-After</c> fallback when the lease
/// exposes no <see cref="MetadataName.RetryAfter"/> metadata.
/// </summary>
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

        // Both built-in limiters normally supply RetryAfter metadata; fall back to the relevant
        // window/period only if it is ever missing. The health-path rule is shared with the partitioner.
        var limits = options.Value;
        return RateLimitRequestClassifier.IsHealthRequest(context.HttpContext.Request.Path)
            ? limits.HealthWindowSeconds
            : limits.ReplenishmentPeriodSeconds;
    }
}
