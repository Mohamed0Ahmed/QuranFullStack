using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingPreparedPreflightStore
{
    public async Task RunMaintenanceAsync(CancellationToken cancellationToken)
    {
        await ExpireReadyPreflightsAsync(cancellationToken);
        await CancelAbandonedLeasesAsync(cancellationToken);
        await FailAbandonedPreflightsAsync(cancellationToken);

        var now = await DatabaseNowAsync(cancellationToken);
        var cleanup = await ClaimCleanupAsync(now, cancellationToken);
        if (cleanup is not null)
        {
            await DrainCleanupAsync(cleanup, cancellationToken);
        }
    }

    private async Task ExpireReadyPreflightsAsync(CancellationToken cancellationToken) =>
        _ = await db.Database.ExecuteSqlRawAsync(
            """
            WITH candidates AS (
                SELECT id
                FROM linking_prepared_preflights
                WHERE status = 'ready'
                  AND confirmation_accepted_at_utc IS NULL
                  AND expires_at_utc < CURRENT_TIMESTAMP
                ORDER BY expires_at_utc, id
                FOR UPDATE SKIP LOCKED
                LIMIT 500
            )
            UPDATE linking_prepared_preflights AS preflight
            SET status = 'expired',
                failure_code = 'PREFLIGHT_EXPIRED',
                completed_at_utc = CURRENT_TIMESTAMP,
                updated_at_utc = CURRENT_TIMESTAMP
            FROM candidates
            WHERE preflight.id = candidates.id
              AND preflight.status = 'ready'
              AND preflight.confirmation_accepted_at_utc IS NULL
              AND preflight.expires_at_utc < CURRENT_TIMESTAMP
            """,
            cancellationToken);

    private async Task CancelAbandonedLeasesAsync(CancellationToken cancellationToken) =>
        _ = await db.Database.ExecuteSqlRawAsync(
            """
            WITH candidates AS (
                SELECT id
                FROM linking_prepared_preflights
                WHERE status = 'preparing'
                  AND cancellation_requested_at_utc IS NOT NULL
                  AND lease_expires_at_utc < CURRENT_TIMESTAMP
                ORDER BY lease_expires_at_utc, id
                FOR UPDATE SKIP LOCKED
                LIMIT 500
            )
            UPDATE linking_prepared_preflights AS preflight
            SET status = 'cancelled',
                failure_code = 'PREFLIGHT_CANCELLED',
                completed_at_utc = CURRENT_TIMESTAMP,
                lease_owner = NULL,
                lease_expires_at_utc = NULL,
                updated_at_utc = CURRENT_TIMESTAMP
            FROM candidates
            WHERE preflight.id = candidates.id
              AND preflight.status = 'preparing'
              AND preflight.cancellation_requested_at_utc IS NOT NULL
              AND preflight.lease_expires_at_utc < CURRENT_TIMESTAMP
            """,
            cancellationToken);

    private async Task FailAbandonedPreflightsAsync(CancellationToken cancellationToken) =>
        _ = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            WITH candidates AS (
                SELECT id
                FROM linking_prepared_preflights
                WHERE (status = 'queued'
                        AND created_at_utc < CURRENT_TIMESTAMP - {policy.AbandonedPreflightLifetime})
                   OR (status = 'preparing'
                        AND lease_expires_at_utc < CURRENT_TIMESTAMP
                        AND started_at_utc < CURRENT_TIMESTAMP - {policy.AbandonedPreflightLifetime})
                ORDER BY created_at_utc, id
                FOR UPDATE SKIP LOCKED
                LIMIT 500
            )
            UPDATE linking_prepared_preflights AS preflight
            SET status = 'failed',
                failure_code = 'PREPARATION_ABANDONED',
                completed_at_utc = CURRENT_TIMESTAMP,
                lease_owner = NULL,
                lease_expires_at_utc = NULL,
                updated_at_utc = CURRENT_TIMESTAMP
            FROM candidates
            WHERE preflight.id = candidates.id
              AND ((preflight.status = 'queued'
                    AND preflight.created_at_utc < CURRENT_TIMESTAMP - {policy.AbandonedPreflightLifetime})
                OR (preflight.status = 'preparing'
                    AND preflight.lease_expires_at_utc < CURRENT_TIMESTAMP
                    AND preflight.started_at_utc < CURRENT_TIMESTAMP - {policy.AbandonedPreflightLifetime}))
            """,
            cancellationToken);

    private async Task<CleanupLease?> ClaimCleanupAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var cutoff = now.Subtract(policy.TerminalRetention);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var candidates = await db.LinkingPreparedPreflights
            .FromSqlInterpolated(
                $"""
                SELECT preflight.*, preflight.xmin
                FROM linking_prepared_preflights preflight
                WHERE status IN ('stale', 'failed', 'cancelled', 'expired', 'confirmed')
                  AND completed_at_utc < {cutoff}
                  AND (cleanup_started_at_utc IS NULL OR cleanup_lease_expires_at_utc < CURRENT_TIMESTAMP)
                ORDER BY completed_at_utc, id
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .ToListAsync(cancellationToken);
        var preflight = candidates.SingleOrDefault();
        if (preflight is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        preflight.CleanupOwner = Guid.NewGuid();
        preflight.CleanupAttemptCount++;
        var claimedAt = await DatabaseNowAsync(cancellationToken);
        preflight.CleanupStartedAtUtc ??= claimedAt;
        preflight.CleanupLeaseExpiresAtUtc = claimedAt.Add(policy.WorkerLease);
        preflight.UpdatedAtUtc = claimedAt;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CleanupLease(
            preflight.Id,
            preflight.CleanupOwner.Value,
            preflight.CleanupAttemptCount);
    }

    private async Task DrainCleanupAsync(CleanupLease lease, CancellationToken cancellationToken)
    {
        while (true)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var preflight = await LockCleanupLeaseAsync(lease, cancellationToken);
            if (preflight is null)
            {
                return;
            }

            var deleted = await DeletePreparedDescriptionBatchAsync(
                    lease.PreflightId,
                    cancellationToken)
                || await DeletePreparedWordBatchAsync(lease.PreflightId, cancellationToken)
                || await DeletePreparedAyahBatchAsync(lease.PreflightId, cancellationToken)
                || await DeletePreparedUnitBatchAsync(lease.PreflightId, cancellationToken)
                || await DeletePreparedAffectedContributionBatchAsync(
                    lease.PreflightId,
                    cancellationToken)
                || await DeletePreparedSourceBatchAsync(lease.PreflightId, cancellationToken);
            if (deleted)
            {
                if (!await RenewCleanupLeaseAsync(lease, cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return;
                }

                await transaction.CommitAsync(cancellationToken);
                continue;
            }

            if (db.Entry(preflight).State == EntityState.Detached)
            {
                db.Attach(preflight);
            }
            db.LinkingPreparedPreflights.Remove(preflight);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }
    }

    private async Task<LinkingPreparedPreflight?> LockCleanupLeaseAsync(
        CleanupLease lease,
        CancellationToken cancellationToken) =>
        (await db.LinkingPreparedPreflights.FromSqlInterpolated(
                $"""
                SELECT preflight.*, preflight.xmin
                FROM linking_prepared_preflights preflight
                WHERE id = {lease.PreflightId}
                  AND cleanup_owner = {lease.Owner}
                  AND cleanup_attempt_count = {lease.AttemptCount}
                  AND cleanup_lease_expires_at_utc > CURRENT_TIMESTAMP
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken))
        .SingleOrDefault();

    private async Task<bool> RenewCleanupLeaseAsync(
        CleanupLease lease,
        CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE linking_prepared_preflights
            SET cleanup_lease_expires_at_utc = CURRENT_TIMESTAMP + {policy.WorkerLease},
                updated_at_utc = CURRENT_TIMESTAMP
            WHERE id = {lease.PreflightId}
              AND cleanup_owner = {lease.Owner}
              AND cleanup_attempt_count = {lease.AttemptCount}
              AND cleanup_lease_expires_at_utc > CURRENT_TIMESTAMP
            """,
            cancellationToken) == 1;

    private async Task<bool> DeletePreparedSourceBatchAsync(
        Guid preflightId,
        CancellationToken cancellationToken)
    {
        var rows = await db.LinkingPreparedSources
            .Where(source => source.PreflightId == preflightId)
            .OrderBy(source => source.Id)
            .Take(policy.PersistenceBatchSize)
            .ToListAsync(cancellationToken);
        return await DeleteTrackedBatchAsync(rows, cancellationToken);
    }

    private sealed record CleanupLease(Guid PreflightId, Guid Owner, int AttemptCount);
}
