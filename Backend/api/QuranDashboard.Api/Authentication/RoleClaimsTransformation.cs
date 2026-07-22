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

        // Key idempotency off our marked identity, never a ClaimTypes.Role claim — a token could forge that (privilege escalation).
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
