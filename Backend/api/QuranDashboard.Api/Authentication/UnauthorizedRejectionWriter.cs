namespace QuranDashboard.Api.Authentication;

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
