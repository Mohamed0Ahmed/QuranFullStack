using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;

public interface ILinkingPreparedPreflightStore
{
    Task<LinkingPreparedPreflightReceipt> EnqueueAsync(
        int actorUserId,
        CreateLinkingPreparedPreflightRequest request,
        CancellationToken cancellationToken);

    Task<LinkingPreparedPreflightStatusDto?> GetStatusAsync(
        int actorUserId,
        Guid preflightId,
        CancellationToken cancellationToken);

    Task<LinkingPreparedPreflightStatusDto?> CancelAsync(
        int actorUserId,
        Guid preflightId,
        CancellationToken cancellationToken);

    Task<LinkingPreparedDetailPageDto?> GetDetailPageAsync(
        int actorUserId,
        Guid preflightId,
        long? preparedSourceId,
        string filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<LinkingPreparedPreflightLease?> ClaimAsync(CancellationToken cancellationToken);

    Task<IAsyncDisposable?> TryAcquireProcessingFenceAsync(
        LinkingPreparedPreflightLease lease,
        CancellationToken cancellationToken);

    Task<LinkingPreparedPreflightWork?> LoadWorkAsync(
        LinkingPreparedPreflightLease lease,
        CancellationToken cancellationToken);

    Task<bool> PublishProgressAsync(
        LinkingPreparedPreflightLease lease,
        LinkingPreparedPreflightStage stage,
        int processedSources,
        int processedAyahs,
        int? totalAyahs,
        CancellationToken cancellationToken);

    Task<bool> RenewLeaseAsync(
        LinkingPreparedPreflightLease lease,
        CancellationToken cancellationToken);

    Task<bool> ProbeLeaseAsync(
        LinkingPreparedPreflightLease lease,
        CancellationToken cancellationToken);

    Task<LinkingPreparedResultSummary> PersistPreparedResultAsync(
        LinkingPreparedPreflightLease lease,
        LinkingOperationRequest request,
        LinkingOperationIntent intent,
        LinkingConfirmedDoorState state,
        LinkingOperationClassification classification,
        Func<LinkingPreparedPreflightStage, int, int, int?, CancellationToken, Task<bool>> publishProgress,
        CancellationToken cancellationToken);

    Task<bool> FinalizeReadyAsync(
        LinkingPreparedPreflightLease lease,
        LinkingConfirmedDoorState state,
        LinkingPreparedResultSummary summary,
        string intentHash,
        string preflightToken,
        CancellationToken cancellationToken);

    Task CompleteFailureAsync(
        LinkingPreparedPreflightLease lease,
        LinkingPreparedPreflightFailureCode failureCode,
        bool retryable,
        CancellationToken cancellationToken);

    Task RunMaintenanceAsync(CancellationToken cancellationToken);
}

public sealed record LinkingPreparedPreflightLease(
    Guid PreflightId,
    Guid LeaseOwner,
    int AttemptCount,
    long LinkingDataRevision);

public sealed record LinkingPreparedPreflightWork(
    Guid PreflightId,
    int ActorUserId,
    int DoorId,
    long LinkingDataRevision,
    string RequestHash,
    IReadOnlyList<LinkingPreparedSourceWork> Sources);

public sealed record LinkingPreparedSourceWork(
    long PreparedSourceId,
    int OrderValue,
    LinkingPreparedInlineSource Source);
