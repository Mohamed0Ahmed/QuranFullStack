using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Application.Access.Commands.AcceptAccessUser;

public sealed class AcceptAccessUserHandler(
    ICurrentUser currentUser,
    IAccessUserMutationService mutationService)
{
    public Task<AccessOperationResult<AccessUserDetail>> HandleAsync(
        AcceptAccessUserCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId < 1 || !AccessAdministrationValidation.TryGetOptionalReason(command.Reason, out var reason))
        {
            return Task.FromResult(AccessOperationResult<AccessUserDetail>.Failed(AccessOperationFailure.InvalidRequest));
        }

        return mutationService.AcceptAsync(
            currentUser.Identity.Sub,
            command with { Reason = reason },
            cancellationToken);
    }
}
