using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;

namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingConfirmationWriter
{
    Task<LinkingConfirmationResultDto?> FindLegacyReplayAsync(
        int actorUserId,
        int doorId,
        Guid idempotencyKey,
        LinkingConfirmationRequestContract requestContract,
        CancellationToken cancellationToken);

    Task<LinkingConfirmationWriteResult> ConfirmAsync(
        int actorUserId,
        LinkingOperationRequest request,
        LinkingOperationIntent intent,
        LinkingConfirmationRequestContract requestContract,
        Func<LinkingOperationIntent, LinkingConfirmedDoorState, LinkingOperationClassification> classify,
        CancellationToken cancellationToken);

    Task<LinkingConfirmationWriteResult> ConfirmPreparedAsync(
        LinkingConfirmationJobLease lease,
        LinkingOperationRequest request,
        LinkingOperationIntent intent,
        Func<LinkingOperationIntent, LinkingConfirmedDoorState, LinkingOperationClassification> classify,
        CancellationToken cancellationToken);
}

public sealed record LinkingConfirmationRequestContract(
    string Kind,
    int SchemaVersion,
    string RequestHash,
    long LinkingDataRevision,
    Guid? PreparedPreflightReferenceId = null,
    Guid? ConfirmationJobReferenceId = null,
    Guid? PreparedPreflightId = null);

public abstract record LinkingConfirmationWriteResult
{
    private LinkingConfirmationWriteResult() { }

    public sealed record Success(
        LinkingConfirmationResultDto Result,
        bool IsReplay) : LinkingConfirmationWriteResult;

    public sealed record DoorNotFound(int DoorId) : LinkingConfirmationWriteResult;

    public sealed record InvalidClassification(
        LinkingOperationClassification Classification) : LinkingConfirmationWriteResult;
}
