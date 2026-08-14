using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationJobStore
{
    public async Task RunMaintenanceAsync(CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var cutoff = (await DatabaseNowAsync(cancellationToken)).Subtract(policy.TerminalRetention);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var candidates = await db.LinkingConfirmationJobs.FromSqlInterpolated(
                $"""
                SELECT job.*, job.xmin
                FROM linking_confirmation_jobs job
                WHERE status IN ('succeeded', 'stale', 'failed', 'cancelled')
                  AND completed_at_utc < {cutoff}
                  AND (cleanup_started_at_utc IS NULL
                       OR cleanup_lease_expires_at_utc < CURRENT_TIMESTAMP)
                ORDER BY completed_at_utc, id
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .ToListAsync(cancellationToken);
        var job = candidates.SingleOrDefault();
        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var now = await DatabaseNowAsync(cancellationToken);
        job.CleanupOwner = Guid.NewGuid();
        job.CleanupAttemptCount++;
        job.CleanupStartedAtUtc ??= now;
        job.CleanupLeaseExpiresAtUtc = now.Add(policy.WorkerLease);
        job.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        db.LinkingConfirmationJobs.Remove(job);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
