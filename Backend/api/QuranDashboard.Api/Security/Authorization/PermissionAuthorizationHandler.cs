using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Security;

namespace QuranDashboard.Api.Security.Authorization;

public sealed class PermissionAuthorizationHandler(
    ICurrentUser currentUser,
    EffectivePermissionResolver resolver) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var roleName = context.User.FindFirst(ClaimTypes.Role)?.Value;
        var effective = await resolver.ResolveAsync(currentUser.Sub, roleName, CancellationToken.None);
        if (effective.Contains(requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }
    }
}
