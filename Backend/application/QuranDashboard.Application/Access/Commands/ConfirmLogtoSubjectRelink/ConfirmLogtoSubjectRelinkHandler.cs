using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Application.Access.Commands.ConfirmLogtoSubjectRelink;

public sealed class ConfirmLogtoSubjectRelinkHandler(
    ICurrentUser currentUser,
    ILogtoSubjectRelinkService relinkService)
{
    public Task<AccessOperationResult<AccessUserDetail>> HandleAsync(
        ConfirmLogtoSubjectRelinkCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId < 1
            || !command.Confirmed
            || string.IsNullOrWhiteSpace(command.OldSub)
            || string.IsNullOrWhiteSpace(command.NewSub)
            || string.IsNullOrWhiteSpace(command.EvidenceToken)
            || !AccessAdministrationValidation.TryGetReason(command.Reason, out var reason))
        {
            return Task.FromResult(AccessOperationResult<AccessUserDetail>.Failed(
                command.Confirmed ? AccessOperationFailure.InvalidRequest : AccessOperationFailure.ConfirmationRequired));
        }

        return relinkService.ConfirmAsync(
            currentUser.Identity.Sub,
            command with
            {
                OldSub = command.OldSub.Trim(),
                NewSub = command.NewSub.Trim(),
                EvidenceToken = command.EvidenceToken.Trim(),
                Reason = reason,
            },
            cancellationToken);
    }
}
