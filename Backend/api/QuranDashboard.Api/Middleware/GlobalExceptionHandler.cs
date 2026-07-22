using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;
using QuranDashboard.Api.Abwab;
using QuranDashboard.Application.Abstractions.Security;

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

        if (exception is UserProvisioningEmailConflictException)
        {
            logger.LogWarning(
                "Provisioning email conflict while processing request {traceId} {requestId} {method} {path}",
                traceId,
                requestId,
                method,
                path);

            var conflictResponse = ApiResponse<object>.Fail(ApiMessages.EmailAlreadyRegistered);

            httpContext.Response.Clear();
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            httpContext.Response.ContentType = "application/json";

            await httpContext.Response.WriteAsJsonAsync(conflictResponse, cancellationToken);
            return true;
        }

        if (AbwabConflictResponses.TryMap(exception, out var abwabStatusCode, out var abwabResponse))
        {
            logger.LogWarning(
                "Abwab conflict while processing request {traceId} {requestId} {method} {path}",
                traceId,
                requestId,
                method,
                path);

            httpContext.Response.Clear();
            httpContext.Response.StatusCode = abwabStatusCode;
            httpContext.Response.ContentType = "application/json";

            await httpContext.Response.WriteAsJsonAsync(abwabResponse, cancellationToken);
            return true;
        }

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
