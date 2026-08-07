using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Application.Access.Commands.PreviewLogtoSubjectRelink;

public sealed class PreviewLogtoSubjectRelinkHandler(ILogtoSubjectRelinkService relinkService)
{
    public Task<AccessOperationResult<LogtoSubjectRelinkPreview>> HandleAsync(
        PreviewLogtoSubjectRelinkCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId < 1
            || string.IsNullOrWhiteSpace(command.NewSub)
            || string.IsNullOrWhiteSpace(command.EvidenceToken))
        {
            return Task.FromResult(AccessOperationResult<LogtoSubjectRelinkPreview>.Failed(AccessOperationFailure.InvalidRequest));
        }

        return relinkService.PreviewAsync(
            command with { NewSub = command.NewSub.Trim(), EvidenceToken = command.EvidenceToken.Trim() },
            cancellationToken);
    }
}
