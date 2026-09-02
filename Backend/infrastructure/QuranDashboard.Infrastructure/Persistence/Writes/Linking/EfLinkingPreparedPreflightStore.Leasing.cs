using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingPreparedPreflightStore
{
    public async Task<LinkingPreparedPreflightLease?> ClaimAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await lockProtocol.AcquirePreparedWorkerClaimAsync(cancellationToken);
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
            var acquired = await lockProtocol.TryAcquirePreparedProcessingForWorkerClaimAsync(
                candidate.Id,
                cancellationToken);
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
        IAsyncDisposable? processingLock = null;
        try
        {
            processingLock = await lockProtocol.TryAcquirePreparedProcessingSessionAsync(
                lease.PreflightId,
                cancellationToken);
            if (processingLock is null)
            {
                await db.Database.CloseConnectionAsync();
                return null;
            }

            var active = await ProbeLeaseAsync(lease, cancellationToken);
            if (active)
            {
                return new ProcessingFence(db, processingLock);
            }

            await new ProcessingFence(db, processingLock).DisposeAsync();
            return null;
        }
        catch
        {
            try
            {
                if (processingLock is not null)
                {
                    await processingLock.DisposeAsync();
                }
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }

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
            [.. sources.Select(source =>
            {
                var descriptor = LinkingPreparedSnapshotCodec.DecodeDescriptor(
                    source.DescriptorDocumentJson);
                var configuration = LinkingPreparedSnapshotCodec.DecodeConfiguration(
                    source.ConfigurationDocumentJson,
                    descriptor.Kind);
                if (descriptor.Kind != source.SourceKind
                    || configuration.ContributionMode != source.ContributionMode)
                {
                    throw new InvalidDataException("The stored prepared linking source is incoherent.");
                }

                return new LinkingPreparedSourceWork(
                    source.Id,
                    source.OrderValue,
                    new LinkingPreparedInlineSource(descriptor, configuration));
            })]);
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

    private async Task<DateTimeOffset> DatabaseNowAsync(CancellationToken cancellationToken) =>
        await db.Database.SqlQueryRaw<DateTimeOffset>(
                "SELECT CURRENT_TIMESTAMP AS \"Value\"")
            .SingleAsync(cancellationToken);

    private sealed class ProcessingFence(
        QuranDashboardDbContext dbContext,
        IAsyncDisposable processingLock)
        : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await processingLock.DisposeAsync();
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }
}
