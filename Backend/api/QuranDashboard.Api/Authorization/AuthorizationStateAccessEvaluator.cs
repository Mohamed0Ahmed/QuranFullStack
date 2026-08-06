using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Routing;
using QuranDashboard.Api.Authorization.Validation;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Api.Authorization;

public sealed class AuthorizationStateAccessEvaluator(
    IAuthorizationStateResolver resolver,
    ICurrentUser currentUser,
    AuthorizationFailureState failureState,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthorizationStateAccessEvaluator> logger)
{
    public bool CurrentEndpointMetadataIsValid()
    {
        var endpoint = httpContextAccessor.HttpContext?.GetEndpoint();
        return endpoint is not RouteEndpoint routeEndpoint
            || UnsafeEndpointMetadataValidator.ValidateEndpoint(routeEndpoint).IsValid;
    }

    public async Task<AuthorizationState?> ResolveActiveStateAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        string sub;
        try
        {
            sub = currentUser.Identity.Sub;
        }
        catch (InvalidOperationException)
        {
            failureState.Record(AuthorizationFailureReason.Unprovisioned);
            return null;
        }

        if (string.IsNullOrWhiteSpace(sub))
        {
            failureState.Record(AuthorizationFailureReason.Unprovisioned);
            return null;
        }

        AuthorizationState? state;
        try
        {
            state = await resolver.ResolveAsync(
                sub,
                httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None);
        }
        catch (OperationCanceledException) when (httpContextAccessor.HttpContext?.RequestAborted.IsCancellationRequested == true)
        {
            throw;
        }
        catch (Exception exception)
        {
            var httpContext = httpContextAccessor.HttpContext;
            logger.LogError(
                exception,
                "Authorization state resolution failed for request {traceId} {requestId} {method} {path}",
                Activity.Current?.TraceId.ToString(),
                httpContext?.TraceIdentifier,
                httpContext?.Request.Method,
                httpContext?.Request.Path.Value);
            failureState.Record(AuthorizationFailureReason.InfrastructureUnavailable);
            return null;
        }

        if (state is null)
        {
            failureState.Record(AuthorizationFailureReason.Unprovisioned);
            return null;
        }

        if (state.Status != UserStatus.Active)
        {
            failureState.Record(AuthorizationFailureReason.Inactive);
            return null;
        }

        return state;
    }
}
