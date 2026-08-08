using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Application.Access.Commands.ReplaceUserPermissions;

public sealed class ReplaceUserPermissionsHandler(
    ICurrentUser currentUser,
    IAccessUserMutationService mutationService)
{
    public Task<AccessOperationResult<AccessUserPermissions>> HandleAsync(
        ReplaceUserPermissionsCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId < 1
            || command.PermissionCodes is null
            || !AccessAdministrationValidation.TryGetOptionalReason(command.Reason, out var reason))
        {
            return Task.FromResult(AccessOperationResult<AccessUserPermissions>.Failed(AccessOperationFailure.InvalidRequest));
        }

        return mutationService.ReplacePermissionsAsync(
            currentUser.Identity.Sub,
            command with { Reason = reason },
            cancellationToken);
    }
}
