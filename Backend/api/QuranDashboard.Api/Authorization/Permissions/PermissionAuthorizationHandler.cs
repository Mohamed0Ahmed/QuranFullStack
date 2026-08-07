using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.Api.Authorization.Permissions;

public sealed class PermissionAuthorizationHandler(
    AuthorizationStateAccessEvaluator stateEvaluator,
    AuthorizationFailureState failureState) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!stateEvaluator.CurrentEndpointMetadataIsValid())
        {
            failureState.Record(AuthorizationFailureReason.MissingPermission);
            return;
        }

        if (!AbwabPermissionCatalogue.All.Any(permission =>
                string.Equals(permission.Code, requirement.PermissionCode, StringComparison.Ordinal)))
        {
            failureState.Record(AuthorizationFailureReason.MissingPermission);
            return;
        }

        var state = await stateEvaluator.ResolveActiveStateAsync(context.User);
        if (state is null)
        {
            return;
        }

        if (state.IsOwner || state.PermissionCodes.Contains(requirement.PermissionCode))
        {
            context.Succeed(requirement);
            return;
        }

        failureState.Record(AuthorizationFailureReason.MissingPermission);
    }
}
