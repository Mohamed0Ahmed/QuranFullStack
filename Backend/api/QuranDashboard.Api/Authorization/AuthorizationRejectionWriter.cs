namespace QuranDashboard.Api.Authorization;

public sealed class AuthorizationRejectionWriter
{
    public ValueTask WriteChallengeAsync(HttpContext httpContext, CancellationToken cancellationToken) =>
        WriteAsync(
            httpContext,
            new AuthorizationRejection(StatusCodes.Status401Unauthorized, ApiMessages.Unauthorized),
            cancellationToken);

    public ValueTask WriteForbidAsync(
        HttpContext httpContext,
        AuthorizationFailureReason? failureReason,
        CancellationToken cancellationToken)
        => WriteAsync(httpContext, ResolveForbid(failureReason), cancellationToken);

    private static AuthorizationRejection ResolveForbid(AuthorizationFailureReason? failureReason) =>
        failureReason switch
        {
            AuthorizationFailureReason.Unprovisioned =>
                new(StatusCodes.Status403Forbidden, ApiMessages.AccessUnprovisioned),
            AuthorizationFailureReason.Inactive =>
                new(StatusCodes.Status403Forbidden, ApiMessages.AccessInactive),
            AuthorizationFailureReason.OwnerRequired =>
                new(StatusCodes.Status403Forbidden, ApiMessages.AccessOwnerRequired),
            AuthorizationFailureReason.InfrastructureUnavailable =>
                new(StatusCodes.Status503ServiceUnavailable, ApiMessages.AuthorizationUnavailable),
            _ => new(StatusCodes.Status403Forbidden, ApiMessages.AccessPermissionDenied),
        };

    private static async ValueTask WriteAsync(
        HttpContext httpContext,
        AuthorizationRejection rejection,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        httpContext.Response.StatusCode = rejection.StatusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(rejection.Message),
            cancellationToken);
    }

    private sealed record AuthorizationRejection(int StatusCode, string Message);
}
