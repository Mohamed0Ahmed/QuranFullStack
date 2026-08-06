using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

public sealed class JwtInteractiveIdentityEvidenceValidator(
    IServiceScopeFactory serviceScopeFactory) : IInteractiveIdentityEvidenceValidator
{
    public async Task<AuthenticatedInteractiveIdentity?> ValidateAsync(
        string evidenceToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(evidenceToken))
        {
            return null;
        }

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            RequestAborted = cancellationToken,
        };
        context.Request.Headers.Authorization = $"Bearer {evidenceToken}";
        var authenticationService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var result = await authenticationService.AuthenticateAsync(context, JwtBearerDefaults.AuthenticationScheme);
        var principal = result.Principal;
        if (!result.Succeeded || principal is null)
        {
            return null;
        }

        var sub = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(sub))
        {
            return null;
        }

        var email = principal.FindFirst("email")?.Value;
        var emailVerified = bool.TryParse(principal.FindFirst("email_verified")?.Value, out var verified)
            && verified;
        return new AuthenticatedInteractiveIdentity(sub, email, emailVerified);
    }
}
