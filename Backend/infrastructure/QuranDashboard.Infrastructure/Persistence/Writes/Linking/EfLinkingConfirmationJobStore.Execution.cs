using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationJobStore
{
    public async Task<LinkingConfirmationJobLease?> ClaimAsync(CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await TakeAdvisoryLockAsync(ClaimLockNamespace, 1, cancellationToken);
        var activeCount = await db.LinkingConfirmationJobs.CountAsync(
            job => (job.Status == LinkingConfirmationJobStatus.Running
                    || job.Status == LinkingConfirmationJobStatus.Finalizing)
                && job.LeaseExpiresAtUtc > DateTimeOffset.UtcNow,
            cancellationToken);
        if (activeCount >= policy.ConfirmationProcessorConcurrency)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var candidates = await db.LinkingConfirmationJobs.FromSqlInterpolated(
                $"""
                SELECT job.*, job.xmin
                FROM linking_confirmation_jobs job
                WHERE (
                    job.status = 'queued'
                    OR (job.status IN ('running', 'finalizing')
                        AND job.lease_expires_at_utc < CURRENT_TIMESTAMP))
                  AND job.attempt_count < {policy.MaximumAutomaticAttempts}
                  AND job.cancellation_requested_at_utc IS NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM linking_confirmation_jobs active
                      WHERE active.door_id = job.door_id
                        AND active.id <> job.id
                        AND active.status IN ('running', 'finalizing')
                        AND active.lease_expires_at_utc > CURRENT_TIMESTAMP)
                ORDER BY job.queued_at_utc, job.id
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .ToListAsync(cancellationToken);
        var job = candidates.SingleOrDefault();
        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var now = await DatabaseNowAsync(cancellationToken);
        if (job.Status != LinkingConfirmationJobStatus.Finalizing)
        {
            job.Status = LinkingConfirmationJobStatus.Running;
            job.Stage = LinkingConfirmationJobStage.LoadingPrepared;
        }

        job.AttemptCount++;
        job.LeaseOwner = Guid.NewGuid();
        job.LeaseExpiresAtUtc = now.Add(policy.WorkerLease);
        job.StartedAtUtc ??= now;
        job.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new LinkingConfirmationJobLease(
            job.Id,
            job.PreflightId,
            job.ActorUserId,
            job.DoorId,
            job.IdempotencyKey,
            job.RequestHash,
            job.LeaseOwner.Value,
            job.AttemptCount,
            job.Status);
    }

    public async Task<bool> PublishProgressAsync(
        LinkingConfirmationJobLease lease,
        LinkingConfirmationJobStage stage,
        int processedItems,
        int totalItems,
        CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE linking_confirmation_jobs
            SET stage = {LinkingConfirmationJobLifecycleTokens.ToToken(stage)},
                processed_items = {processedItems},
                total_items = {totalItems},
                updated_at_utc = CURRENT_TIMESTAMP
            WHERE id = {lease.JobId}
              AND status = 'running'
              AND lease_owner = {lease.LeaseOwner}
              AND attempt_count = {lease.AttemptCount}
              AND lease_expires_at_utc > CURRENT_TIMESTAMP
              AND cancellation_requested_at_utc IS NULL
            """,
            cancellationToken) == 1;

    public async Task<bool> RenewLeaseAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE linking_confirmation_jobs
            SET lease_expires_at_utc = CURRENT_TIMESTAMP + {policy.WorkerLease},
                updated_at_utc = CURRENT_TIMESTAMP
            WHERE id = {lease.JobId}
              AND status IN ('running', 'finalizing')
              AND lease_owner = {lease.LeaseOwner}
              AND attempt_count = {lease.AttemptCount}
              AND lease_expires_at_utc > CURRENT_TIMESTAMP
            """,
            cancellationToken) == 1;

    public async Task<bool> EnterFinalizingAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await TakeAdvisoryLockAsync(JobLockNamespace, LockKey(lease.JobId), cancellationToken);
        var changed = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE linking_confirmation_jobs
            SET status = 'finalizing',
                stage = 'committing',
                updated_at_utc = CURRENT_TIMESTAMP
            WHERE id = {lease.JobId}
              AND status = 'running'
              AND lease_owner = {lease.LeaseOwner}
              AND attempt_count = {lease.AttemptCount}
              AND lease_expires_at_utc > CURRENT_TIMESTAMP
              AND cancellation_requested_at_utc IS NULL
            """,
            cancellationToken);
        if (changed != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task CompleteFailureAsync(
        LinkingConfirmationJobLease lease,
        LinkingConfirmationJobStatus status,
        LinkingConfirmationJobFailureCode failureCode,
        bool retryable,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await TakeAdvisoryLockAsync(JobLockNamespace, LockKey(lease.JobId), cancellationToken);
        var jobs = await db.LinkingConfirmationJobs.FromSqlInterpolated(
                $"""
                SELECT job.*, job.xmin
                FROM linking_confirmation_jobs job
                WHERE id = {lease.JobId}
                  AND status IN ('running', 'finalizing')
                  AND lease_owner = {lease.LeaseOwner}
                  AND attempt_count = {lease.AttemptCount}
                  AND lease_expires_at_utc > CURRENT_TIMESTAMP
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);
        var job = jobs.SingleOrDefault();
        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var now = await DatabaseNowAsync(cancellationToken);
        if (retryable
            && job.AttemptCount < policy.MaximumAutomaticAttempts
            && job.CancellationRequestedAtUtc is null)
        {
            if (job.Status == LinkingConfirmationJobStatus.Running)
            {
                job.Status = LinkingConfirmationJobStatus.Queued;
                job.Stage = LinkingConfirmationJobStage.LoadingPrepared;
            }

            job.LeaseOwner = null;
            job.LeaseExpiresAtUtc = now;
            job.UpdatedAtUtc = now;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var preflight = await LockPreflightAsync(job.PreflightId, cancellationToken)
            ?? throw new InvalidOperationException("A retained confirmation job lost its pinned preflight.");
        var preflightStatus = status switch
        {
            LinkingConfirmationJobStatus.Stale => LinkingPreparedPreflightStatus.Stale,
            LinkingConfirmationJobStatus.Cancelled => LinkingPreparedPreflightStatus.Cancelled,
            _ => LinkingPreparedPreflightStatus.Failed,
        };
        var preflightFailure = failureCode switch
        {
            LinkingConfirmationJobFailureCode.LinkingDataStale =>
                LinkingPreparedPreflightFailureCode.LinkingDataStale,
            LinkingConfirmationJobFailureCode.PreflightBlocked =>
                LinkingPreparedPreflightFailureCode.PreflightBlocked,
            LinkingConfirmationJobFailureCode.PreflightStale =>
                LinkingPreparedPreflightFailureCode.PreflightStale,
            LinkingConfirmationJobFailureCode.ConfirmationCancelled =>
                LinkingPreparedPreflightFailureCode.ConfirmationCancelled,
            _ => LinkingPreparedPreflightFailureCode.ConfirmationFailed,
        };
        ApplyTerminal(
            job,
            preflight,
            status,
            failureCode,
            preflightStatus,
            preflightFailure,
            now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

}
