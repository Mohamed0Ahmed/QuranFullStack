using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

public sealed class RoleClaimsTransformation(IUserRoleResolver roleResolver) : IClaimsTransformation
{
    public const string RoleClaimsAuthenticationType = "QuranDashboardRoleClaims";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        // Idempotency keys off our own marked identity, not off any ClaimTypes.Role claim: with
        // MapInboundClaims = false a token could carry such a claim, and it must neither short-circuit the
        // database role load below nor be mistaken for this transformation's output.
        if (principal.Identities.Any(identity => identity.AuthenticationType == RoleClaimsAuthenticationType))
        {
            return principal;
        }

        var sub = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub))
        {
            return principal;
        }

        var roleName = await roleResolver.GetActiveRoleNameAsync(sub, CancellationToken.None);
        if (string.IsNullOrEmpty(roleName))
        {
            return principal;
        }

        var roleIdentity = new ClaimsIdentity(RoleClaimsAuthenticationType);
        roleIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
        principal.AddIdentity(roleIdentity);
        return principal;
    }
}
