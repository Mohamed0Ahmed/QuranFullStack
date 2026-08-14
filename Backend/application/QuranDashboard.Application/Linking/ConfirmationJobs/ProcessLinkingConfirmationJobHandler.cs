using System.Runtime.ExceptionServices;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking.ConfirmationJobs;

public sealed class ProcessLinkingConfirmationJobHandler(
    ILinkingConfirmationJobStore store,
    ILinkingConfirmationWriter writer,
    LinkingConfirmationJobLeaseService leaseService,
    ILogger<ProcessLinkingConfirmationJobHandler> logger) : ILinkingConfirmationJobProcessor
{
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var lease = await store.ClaimAsync(cancellationToken);
        if (lease is null)
        {
            return false;
        }

        try
        {
            await ProcessClaimedAsync(lease, cancellationToken);
        }
        catch (LinkingConfirmationJobLeaseLostException)
        {
        }
        catch (LinkingDataStaleException)
        {
            await store.CompleteFailureAsync(
                lease,
                LinkingConfirmationJobStatus.Stale,
                LinkingConfirmationJobFailureCode.LinkingDataStale,
                false,
                cancellationToken);
        }
        catch (LinkingStaleVersionException)
        {
            await store.CompleteFailureAsync(
                lease,
                LinkingConfirmationJobStatus.Stale,
                LinkingConfirmationJobFailureCode.PreflightStale,
                false,
                cancellationToken);
        }
        catch (LinkingIdempotencyConflictException)
        {
            await store.CompleteFailureAsync(
                lease,
                LinkingConfirmationJobStatus.Failed,
                LinkingConfirmationJobFailureCode.IdempotencyConflict,
                false,
                cancellationToken);
        }
        catch (LinkingConfirmationTerminalException exception)
        {
            await store.CompleteFailureAsync(
                lease,
                exception.Status,
                exception.FailureCode,
                false,
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await store.CompleteFailureAsync(
                lease,
                LinkingConfirmationJobStatus.Cancelled,
                LinkingConfirmationJobFailureCode.ConfirmationCancelled,
                false,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Linking confirmation job {JobId} attempt {AttemptCount} failed.",
                lease.JobId,
                lease.AttemptCount);
            await store.CompleteFailureAsync(
                lease,
                LinkingConfirmationJobStatus.Failed,
                LinkingConfirmationJobFailureCode.ConfirmationFailed,
                true,
                cancellationToken);
        }

        return true;
    }

    private async Task ProcessClaimedAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken)
    {
        using var workCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var heartbeat = leaseService.RunHeartbeatAsync(
            lease,
            workCancellation.Cancel,
            workCancellation.Token);
        Exception? processingFailure = null;

        try
        {
            var execution = await store.LoadExecutionAsync(lease, workCancellation.Token)
                ?? throw new LinkingConfirmationJobLeaseLostException();
            if (lease.Status != LinkingConfirmationJobStatus.Finalizing)
            {
                if (!await leaseService.PublishProgressAsync(
                        lease,
                        LinkingConfirmationJobStage.ApplyingUnitDiff,
                        execution.TotalItems,
                        execution.TotalItems,
                        workCancellation.Token))
                {
                    throw new LinkingConfirmationJobLeaseLostException();
                }

                if (!await store.EnterFinalizingAsync(lease, workCancellation.Token))
                {
                    throw new LinkingConfirmationJobLeaseLostException();
                }
            }

            var write = await writer.ConfirmPreparedAsync(
                lease with { Status = LinkingConfirmationJobStatus.Finalizing },
                execution.Request,
                execution.Intent,
                LinkingOperationClassifier.Classify,
                workCancellation.Token);
            if (write is LinkingConfirmationWriteResult.DoorNotFound)
            {
                throw new LinkingConfirmationTerminalException(
                    LinkingConfirmationJobStatus.Failed,
                    LinkingConfirmationJobFailureCode.DoorNotFound);
            }

            if (write is LinkingConfirmationWriteResult.InvalidClassification)
            {
                throw new LinkingConfirmationTerminalException(
                    LinkingConfirmationJobStatus.Stale,
                    LinkingConfirmationJobFailureCode.PreflightBlocked);
            }
        }
        catch (Exception exception)
        {
            processingFailure = exception;
        }

        workCancellation.Cancel();
        Exception? heartbeatFailure = null;
        try
        {
            await heartbeat;
        }
        catch (OperationCanceledException) when (workCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            heartbeatFailure = exception;
        }

        var failure = heartbeatFailure ?? processingFailure;
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private sealed class LinkingConfirmationTerminalException(
        LinkingConfirmationJobStatus status,
        LinkingConfirmationJobFailureCode failureCode) : Exception
    {
        public LinkingConfirmationJobStatus Status { get; } = status;
        public LinkingConfirmationJobFailureCode FailureCode { get; } = failureCode;
    }
}
