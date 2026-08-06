using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Application.Access.Commands.ReactivateAccessUser;

public sealed class ReactivateAccessUserHandler(
    ICurrentUser currentUser,
    IAccessUserMutationService mutationService)
{
    public Task<AccessOperationResult<AccessUserDetail>> HandleAsync(
        ReactivateAccessUserCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId < 1 || !AccessAdministrationValidation.TryGetReason(command.Reason, out var reason))
        {
            return Task.FromResult(AccessOperationResult<AccessUserDetail>.Failed(AccessOperationFailure.InvalidRequest));
        }

        return mutationService.ReactivateAsync(
            currentUser.Identity.Sub,
            command with { Reason = reason },
            cancellationToken);
    }
}
