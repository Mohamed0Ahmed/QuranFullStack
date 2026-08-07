namespace QuranDashboard.Application.Abstractions.Access;

public sealed record PreviewLogtoSubjectRelinkCommand(
    int UserId,
    string NewSub,
    string EvidenceToken);

public sealed record ConfirmLogtoSubjectRelinkCommand(
    int UserId,
    uint ExpectedVersion,
    string OldSub,
    string NewSub,
    string Reason,
    bool Confirmed,
    string EvidenceToken);

public sealed record LogtoSubjectRelinkPreview(
    int UserId,
    string OldSub,
    string NewSub,
    uint Version,
    bool IsOwner);

public interface ILogtoSubjectRelinkService
{
    Task<AccessOperationResult<LogtoSubjectRelinkPreview>> PreviewAsync(
        PreviewLogtoSubjectRelinkCommand command,
        CancellationToken cancellationToken);

    Task<AccessOperationResult<AccessUserDetail>> ConfirmAsync(
        string actorSub,
        ConfirmLogtoSubjectRelinkCommand command,
        CancellationToken cancellationToken);
}
