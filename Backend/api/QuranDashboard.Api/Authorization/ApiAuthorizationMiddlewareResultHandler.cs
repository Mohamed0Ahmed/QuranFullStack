using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace QuranDashboard.Api.Authorization;

public sealed class ApiAuthorizationMiddlewareResultHandler(
    AuthorizationRejectionWriter rejectionWriter) : IAuthorizationMiddlewareResultHandler
{
    public Task HandleAsync(
        RequestDelegate next,
        HttpContext httpContext,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            return next(httpContext);
        }

        var failureState = httpContext.RequestServices.GetRequiredService<AuthorizationFailureState>();
        return authorizeResult.Challenged
            ? rejectionWriter.WriteChallengeAsync(httpContext, httpContext.RequestAborted).AsTask()
            : rejectionWriter.WriteForbidAsync(
                httpContext,
                failureState.Reason,
                httpContext.RequestAborted).AsTask();
    }
}
