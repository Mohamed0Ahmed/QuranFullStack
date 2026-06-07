using Microsoft.AspNetCore.Diagnostics;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;

namespace QuranDashboard.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);

        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var response = ApiResponse<object>.Fail(ApiMessages.UnexpectedError);

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
