namespace QuranDashboard.Api.Authentication;

/// <summary>
/// Writes the <c>401 Unauthorized</c> response for requests that fail JwtBearer authentication,
/// mirroring <see cref="RateLimiting.RateLimitRejectionWriter"/>: the shared
/// <see cref="ApiResponse{T}"/> failure envelope instead of the framework's default empty body.
/// </summary>
public sealed class UnauthorizedRejectionWriter
{
    public async ValueTask WriteAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(ApiMessages.Unauthorized),
            cancellationToken);
    }
}
