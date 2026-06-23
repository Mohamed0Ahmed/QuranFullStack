using Microsoft.AspNetCore.Diagnostics;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using System.Diagnostics;

namespace QuranDashboard.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        var requestId = httpContext.TraceIdentifier;
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value ?? string.Empty;

        logger.LogError(
            exception,
            "Unhandled exception while processing request {traceId} {requestId} {method} {path}",
            traceId,
            requestId,
            method,
            path);

        var response = ApiResponse<object>.Fail(ApiMessages.UnexpectedError);

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
