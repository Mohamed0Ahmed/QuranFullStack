using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Security.Authorization;

public sealed class SystemOwnerAuthorizationHandler(
    ICurrentUser currentUser,
    ISystemOwnerStore owners) : AuthorizationHandler<SystemOwnerRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SystemOwnerRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (await owners.IsActiveSystemOwnerAsync(currentUser.Sub, CancellationToken.None))
        {
            context.Succeed(requirement);
        }
    }
}
