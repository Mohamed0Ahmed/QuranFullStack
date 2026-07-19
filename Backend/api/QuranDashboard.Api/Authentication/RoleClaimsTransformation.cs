using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

/// <summary>
/// Loads the authenticated caller's application role into a <see cref="ClaimTypes.Role"/> claim so
/// <c>[Authorize(Policy = …)]</c>/<c>RequireRole(…)</c> and <see cref="ClaimsPrincipal.IsInRole"/> work.
/// The role is resolved by the Logto <c>sub</c> via <see cref="IUserRoleResolver"/> (short-TTL cached).
/// This runs during authentication/authorization and MAY be invoked multiple times per request, so it is
/// idempotent: the role lives on a dedicated identity tagged <see cref="RoleClaimsAuthenticationType"/>, and
/// idempotency keys off that marked identity — never off the mere presence of a <see cref="ClaimTypes.Role"/>
/// claim, which (with <c>MapInboundClaims = false</c>) a caller could smuggle in the token and thereby
/// short-circuit the database role load. It never throws for an unauthenticated principal or a missing
/// <c>sub</c> — it simply returns the principal unchanged.
/// </summary>
public sealed class RoleClaimsTransformation(IUserRoleResolver roleResolver) : IClaimsTransformation
{
    /// <summary>
    /// Authentication type stamped on the identity this transformation adds, so a repeat invocation
    /// recognizes its own prior work regardless of any token-borne role claim.
    /// </summary>
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

        // Raw claim types are preserved (MapInboundClaims = false), so the identity key is the literal "sub".
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

        // Attach the role on a separate, marked identity so IsInRole/RequireRole (ClaimTypes.Role) see it
        // and a later invocation recognizes this transformation's work via RoleClaimsAuthenticationType.
        var roleIdentity = new ClaimsIdentity(RoleClaimsAuthenticationType);
        roleIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
        principal.AddIdentity(roleIdentity);
        return principal;
    }
}
