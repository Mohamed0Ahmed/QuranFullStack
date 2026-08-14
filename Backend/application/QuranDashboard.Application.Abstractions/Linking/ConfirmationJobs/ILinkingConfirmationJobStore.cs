using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;

public interface ILinkingConfirmationJobStore
{
    Task<LinkingConfirmationJobReceipt> EnqueueAsync(
        int actorUserId,
        CreateLinkingConfirmationJobRequest request,
        CancellationToken cancellationToken);

    Task<LinkingConfirmationJobStatusDto?> GetStatusAsync(
        int actorUserId,
        Guid jobId,
        CancellationToken cancellationToken);

    Task<LinkingConfirmationJobStatusDto?> CancelAsync(
        int actorUserId,
        Guid jobId,
        CancellationToken cancellationToken);

    Task<LinkingDurableConfirmationOutcomeDto?> GetDurableOutcomeAsync(
        int actorUserId,
        Guid idempotencyKey,
        CancellationToken cancellationToken);

    Task<LinkingConfirmationJobLease?> ClaimAsync(CancellationToken cancellationToken);

    Task<LinkingPreparedConfirmationExecution?> LoadExecutionAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken);

    Task<bool> PublishProgressAsync(
        LinkingConfirmationJobLease lease,
        LinkingConfirmationJobStage stage,
        int processedItems,
        int totalItems,
        CancellationToken cancellationToken);

    Task<bool> RenewLeaseAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken);

    Task<bool> EnterFinalizingAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken);

    Task CompleteFailureAsync(
        LinkingConfirmationJobLease lease,
        LinkingConfirmationJobStatus status,
        LinkingConfirmationJobFailureCode failureCode,
        bool retryable,
        CancellationToken cancellationToken);

    Task RunMaintenanceAsync(CancellationToken cancellationToken);
}

public interface ILinkingConfirmationJobProcessor
{
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken);
}
