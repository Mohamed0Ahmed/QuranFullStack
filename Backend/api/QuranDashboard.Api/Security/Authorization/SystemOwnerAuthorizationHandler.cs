using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Security.Authorization;

// Authoritative System Owner check: succeeds only when the caller's subject is an active enabled owner in
// the membership table. This is the backend authority behind the (non-authoritative) frontend hiding.
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
