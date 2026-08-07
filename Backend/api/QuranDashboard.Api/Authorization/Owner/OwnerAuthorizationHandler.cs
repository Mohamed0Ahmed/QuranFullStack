using Microsoft.AspNetCore.Authorization;

namespace QuranDashboard.Api.Authorization.Owner;

public sealed class OwnerAuthorizationHandler(
    AuthorizationStateAccessEvaluator stateEvaluator,
    AuthorizationFailureState failureState) : AuthorizationHandler<OwnerOnlyRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnerOnlyRequirement requirement)
    {
        if (!stateEvaluator.CurrentEndpointMetadataIsValid())
        {
            failureState.Record(AuthorizationFailureReason.OwnerRequired);
            return;
        }

        var state = await stateEvaluator.ResolveActiveStateAsync(context.User);
        if (state is null)
        {
            return;
        }

        if (state.IsOwner)
        {
            context.Succeed(requirement);
            return;
        }

        failureState.Record(AuthorizationFailureReason.OwnerRequired);
    }
}
