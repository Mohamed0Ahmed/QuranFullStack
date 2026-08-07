using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

public static class InteractiveIdentityEvidenceAuthentication
{
    public const string Scheme = "LogtoIdTokenEvidence";
}

public sealed class JwtInteractiveIdentityEvidenceValidator(
    IServiceScopeFactory serviceScopeFactory) : IInteractiveIdentityEvidenceValidator
{
    public async Task<AuthenticatedInteractiveIdentity?> ValidateAsync(
        string evidenceToken,
        string expectedSubject,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(evidenceToken)
            || string.IsNullOrWhiteSpace(expectedSubject))
        {
            return null;
        }

        var principal = await AuthenticateAsync(evidenceToken, cancellationToken);
        return principal is null
            ? null
            : CreateValidatedIdentity(principal, expectedSubject);
    }

    private async Task<ClaimsPrincipal?> AuthenticateAsync(
        string evidenceToken,
        CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            RequestAborted = cancellationToken,
        };
        context.Request.Headers.Authorization = $"Bearer {evidenceToken}";
        var authenticationService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var result = await authenticationService.AuthenticateAsync(
            context,
            InteractiveIdentityEvidenceAuthentication.Scheme);

        return result.Succeeded ? result.Principal : null;
    }

    private static AuthenticatedInteractiveIdentity? CreateValidatedIdentity(
        ClaimsPrincipal principal,
        string expectedSubject)
    {
        var sub = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value;
        var emailVerified = bool.TryParse(principal.FindFirst("email_verified")?.Value, out var verified)
            && verified;
        if (string.IsNullOrWhiteSpace(sub)
            || !string.Equals(sub, expectedSubject, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(email)
            || !emailVerified)
        {
            return null;
        }

        return new AuthenticatedInteractiveIdentity(sub, email, true);
    }
}
