using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingPreparedPreflightStore
{
    private const int ProcessingLockNamespace = 193648319;
    private const int ClaimLockNamespace = 193648320;
    private const int ClaimLockKey = 1;

    public async Task<LinkingPreparedPreflightLease?> ClaimAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            $"SELECT pg_advisory_xact_lock({ClaimLockNamespace}, {ClaimLockKey})",
            cancellationToken);
        var activeLeases = await db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM linking_prepared_preflights
                WHERE status = 'preparing' AND lease_expires_at_utc > CURRENT_TIMESTAMP
                """)
            .SingleAsync(cancellationToken);
        if (activeLeases >= policy.PreflightProcessorConcurrency)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var candidates = await db.LinkingPreparedPreflights
            .FromSqlRaw(
                """
                SELECT preflight.*, preflight.xmin
                FROM linking_prepared_preflights preflight
                WHERE (
                    status = 'queued'
                    OR (status = 'preparing' AND lease_expires_at_utc < CURRENT_TIMESTAMP))
                  AND attempt_count < {0}
                  AND cancellation_requested_at_utc IS NULL
                ORDER BY created_at_utc, id
                FOR UPDATE SKIP LOCKED
                LIMIT {1}
                """,
                policy.MaximumAutomaticAttempts,
                policy.PreflightProcessorConcurrency + 1)
            .ToListAsync(cancellationToken);
        LinkingPreparedPreflight? preflight = null;
        foreach (var candidate in candidates)
        {
            var acquired = await db.Database.SqlQueryRaw<bool>(
                    "SELECT pg_try_advisory_xact_lock({0}, {1}) AS \"Value\"",
                    ProcessingLockNamespace,
                    ProcessingLockKey(candidate.Id))
                .SingleAsync(cancellationToken);
            if (acquired)
            {
                preflight = candidate;
                break;
            }
        }

        if (preflight is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var now = await DatabaseNowAsync(cancellationToken);
        preflight.Status = LinkingPreparedPreflightStatus.Preparing;
        preflight.Stage = LinkingPreparedPreflightStage.Resolving;
        preflight.LeaseOwner = Guid.NewGuid();
        preflight.LeaseExpiresAtUtc = now.Add(policy.WorkerLease);
        preflight.AttemptCount++;
        preflight.StartedAtUtc ??= now;
        preflight.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var lease = new LinkingPreparedPreflightLease(
            preflight.Id,
            preflight.LeaseOwner.Value,
            preflight.AttemptCount,
            preflight.LinkingDataRevision);
        db.ChangeTracker.Clear();
        return lease;
    }

    public async Task<IAsyncDisposable?> TryAcquireProcessingFenceAsync(
        LinkingPreparedPreflightLease lease,
        CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var acquired = await db.Database.SqlQueryRaw<bool>(
                    "SELECT pg_try_advisory_lock({0}, {1}) AS \"Value\"",
                    ProcessingLockNamespace,
                    ProcessingLockKey(lease.PreflightId))
                .SingleAsync(cancellationToken);
            if (!acquired)
            {
                await db.Database.CloseConnectionAsync();
                return null;
            }

            var active = await ProbeLeaseAsync(lease, cancellationToken);
            if (active)
            {
                return new ProcessingFence(db, ProcessingLockKey(lease.PreflightId));
            }

            await new ProcessingFence(db, ProcessingLockKey(lease.PreflightId)).DisposeAsync();
            return null;
        }
        catch
        {
            await db.Database.CloseConnectionAsync();
            throw;
        }
    }

    public async Task<LinkingPreparedPreflightWork?> LoadWorkAsync(
        LinkingPreparedPreflightLease lease,
        CancellationToken cancellationToken)
    {
        var rows = await db.LinkingPreparedPreflights
            .FromSqlInterpolated(
                $"""
                SELECT preflight.*, preflight.xmin
                FROM linking_prepared_preflights preflight
                WHERE id = {lease.PreflightId}
                  AND status = 'preparing'
                  AND lease_owner = {lease.LeaseOwner}
                  AND attempt_count = {lease.AttemptCount}
                  AND lease_expires_at_utc > CURRENT_TIMESTAMP
                  AND cancellation_requested_at_utc IS NULL
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var preflight = rows.SingleOrDefault();
        if (preflight is null)
        {
            return null;
        }

        var sources = await db.LinkingPreparedSources
            .AsNoTracking()
            .Where(source => source.PreflightId == lease.PreflightId)
            .OrderBy(source => source.OrderValue)
            .ToListAsync(cancellationToken);
        return new LinkingPreparedPreflightWork(
            preflight.Id,
            preflight.ActorUserId,
            preflight.DoorId,
            preflight.LinkingDataRevision,
            preflight.RequestHash,
            [.. sources.Select(source => new LinkingPreparedSourceWork(
                source.Id,
                source.OrderValue,
                new LinkingPreparedInlineSource(
                    LinkingPreparedSnapshotCodec.DecodeDescriptor(source.DescriptorDocumentJson),
                    LinkingPreparedSnapshotCodec.DecodeConfiguration(
                        source.ConfigurationDocumentJson,
                        source.Label))))]);
    }

    public async Task<bool> PublishProgressAsync(
        LinkingPreparedPreflightLease lease,
        LinkingPreparedPreflightStage stage,
        int processedSources,
        int processedAyahs,
        int? totalAyahs,
        CancellationToken cancellationToken)
    {
        return await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE linking_prepared_preflights
            SET stage = {LinkingPreparedPreflightLifecycleTokens.ToToken(stage)},
                processed_sources = {processedSources},
                processed_ayahs = {processedAyahs},
                total_ayahs = {totalAyahs},
                lease_expires_at_utc = CURRENT_TIMESTAMP + {policy.WorkerLease},
                updated_at_utc = CURRENT_TIMESTAMP
            WHERE id = {lease.PreflightId}
              AND status = 'preparing'
              AND lease_owner = {lease.LeaseOwner}
              AND attempt_count = {lease.AttemptCount}
              AND lease_expires_at_utc > CURRENT_TIMESTAMP
              AND cancellation_requested_at_utc IS NULL
            """,
            cancellationToken) == 1;
    }

    public async Task<bool> RenewLeaseAsync(
        LinkingPreparedPreflightLease lease,
        CancellationToken cancellationToken)
    {
        return await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE linking_prepared_preflights
            SET lease_expires_at_utc = CURRENT_TIMESTAMP + {policy.WorkerLease},
                updated_at_utc = CURRENT_TIMESTAMP
            WHERE id = {lease.PreflightId}
              AND status = 'preparing'
              AND lease_owner = {lease.LeaseOwner}
              AND attempt_count = {lease.AttemptCount}
              AND lease_expires_at_utc > CURRENT_TIMESTAMP
              AND cancellation_requested_at_utc IS NULL
            """,
            cancellationToken) == 1;
    }

    public async Task<bool> ProbeLeaseAsync(
        LinkingPreparedPreflightLease lease,
        CancellationToken cancellationToken) =>
        await db.Database.SqlQueryRaw<bool>(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM linking_prepared_preflights
                    WHERE id = {0}
                      AND status = 'preparing'
                      AND lease_owner = {1}
                      AND attempt_count = {2}
                      AND lease_expires_at_utc > CURRENT_TIMESTAMP
                      AND cancellation_requested_at_utc IS NULL
                ) AS "Value"
                """,
                lease.PreflightId,
                lease.LeaseOwner,
                lease.AttemptCount)
            .SingleAsync(cancellationToken);

    private static int ProcessingLockKey(Guid preflightId) =>
        BitConverter.ToInt32(preflightId.ToByteArray(), 0);

    private async Task<DateTimeOffset> DatabaseNowAsync(CancellationToken cancellationToken) =>
        await db.Database.SqlQueryRaw<DateTimeOffset>(
                "SELECT CURRENT_TIMESTAMP AS \"Value\"")
            .SingleAsync(cancellationToken);

    private sealed class ProcessingFence(QuranDashboardDbContext dbContext, int processingKey)
        : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                _ = await dbContext.Database.SqlQueryRaw<bool>(
                        "SELECT pg_advisory_unlock({0}, {1}) AS \"Value\"",
                        ProcessingLockNamespace,
                        processingKey)
                    .SingleAsync();
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }
}
