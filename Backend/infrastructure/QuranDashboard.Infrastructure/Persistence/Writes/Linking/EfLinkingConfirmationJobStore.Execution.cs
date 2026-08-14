using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
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

    public async Task<LinkingPreparedConfirmationExecution?> LoadExecutionAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken)
    {
        var jobExists = await db.LinkingConfirmationJobs.AsNoTracking().AnyAsync(
            job => job.Id == lease.JobId
                && job.LeaseOwner == lease.LeaseOwner
                && job.AttemptCount == lease.AttemptCount
                && job.LeaseExpiresAtUtc > DateTimeOffset.UtcNow
                && (job.Status == LinkingConfirmationJobStatus.Running
                    || job.Status == LinkingConfirmationJobStatus.Finalizing),
            cancellationToken);
        if (!jobExists)
        {
            return null;
        }

        var preflight = await db.LinkingPreparedPreflights.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Id == lease.PreflightId
                && candidate.ActorUserId == lease.ActorUserId
                && candidate.DoorId == lease.DoorId,
            cancellationToken);
        if (preflight is null
            || preflight.Status != LinkingPreparedPreflightStatus.Ready
            || preflight.ConfirmationAcceptedAtUtc is null
            || preflight.IsBlocked != false
            || string.IsNullOrWhiteSpace(preflight.PreflightToken))
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.PreflightStale,
                LinkingPreparedPreflightFailureCode.PreflightStale);
        }

        var expectedHash = LinkingConfirmationRequestHasher.ComputePrepared(
            preflight.Id,
            preflight.PreflightToken,
            preflight.LinkingDataRevision);
        if (!string.Equals(expectedHash, lease.RequestHash, StringComparison.Ordinal))
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.IdempotencyConflict,
                LinkingPreparedPreflightFailureCode.IdempotencyConflict);
        }

        var sources = await db.LinkingPreparedSources.AsNoTracking()
            .Where(source => source.PreflightId == preflight.Id)
            .OrderBy(source => source.OrderValue)
            .ToListAsync(cancellationToken);
        var units = await db.LinkingPreparedUnits.AsNoTracking()
            .Where(unit => unit.PreflightId == preflight.Id)
            .OrderBy(unit => unit.SourceId)
            .ThenBy(unit => unit.OrderValue)
            .ToListAsync(cancellationToken);
        var ayahs = await db.LinkingPreparedAyahs.AsNoTracking()
            .Where(ayah => ayah.PreflightId == preflight.Id && ayah.IsRequested)
            .OrderBy(ayah => ayah.SourceOrder)
            .ThenBy(ayah => ayah.UnitOrder)
            .ThenBy(ayah => ayah.AyahOrder)
            .ToListAsync(cancellationToken);
        var ayahRowIds = ayahs.Select(ayah => ayah.Id).ToList();
        var words = ayahRowIds.Count == 0
            ? []
            : await db.LinkingPreparedAyahWords.AsNoTracking()
                .Where(word => ayahRowIds.Contains(word.PreparedAyahId))
                .OrderBy(word => word.PreparedAyahId)
                .ThenBy(word => word.OrderValue)
                .ToListAsync(cancellationToken);
        var descriptions = ayahRowIds.Count == 0
            ? []
            : await db.LinkingPreparedAyahDescriptions.AsNoTracking()
                .Where(description => ayahRowIds.Contains(description.PreparedAyahId))
                .OrderBy(description => description.PreparedAyahId)
                .ThenBy(description => description.OrderValue)
                .ToListAsync(cancellationToken);
        var quranAyahIds = ayahs.Select(ayah => ayah.AyahId).Distinct().ToList();
        var quranAyahs = await db.QuranAyahs.AsNoTracking()
            .Where(ayah => quranAyahIds.Contains(ayah.Id))
            .Select(ayah => new PreparedQuranAyah(
                ayah.Id,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber))
            .ToDictionaryAsync(ayah => ayah.Id, cancellationToken);

        var wordsByAyah = words.GroupBy(word => word.PreparedAyahId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var descriptionsByAyah = descriptions.GroupBy(description => description.PreparedAyahId)
            .ToDictionary(group => group.Key, group => group.Select(row => row.Body).ToList());
        var ayahsByUnit = ayahs.Where(ayah => ayah.UnitId is not null)
            .GroupBy(ayah => ayah.UnitId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());
        var unitsBySource = units.GroupBy(unit => unit.SourceId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var requestSources = new List<LinkingOperationSourceRequest>(sources.Count);
        var intentSources = new List<LinkingOperationSourceIntent>(sources.Count);
        foreach (var source in sources)
        {
            var descriptor = LinkingPreparedSnapshotCodec.DecodeDescriptor(source.DescriptorDocumentJson);
            var requestUnits = new List<LinkingOperationUnitRequest>();
            var intentUnits = new List<LinkingOperationUnitIntent>();
            foreach (var unit in unitsBySource.GetValueOrDefault(source.Id, []))
            {
                var unitAyahs = ayahsByUnit.GetValueOrDefault(unit.Id, []);
                requestUnits.Add(new LinkingOperationUnitRequest(
                    [.. unitAyahs.Select(ayah => new LinkingOperationAyahRequest(
                        ayah.AyahId,
                        wordsByAyah.GetValueOrDefault(ayah.Id, [])
                            .Where(word => word.IsRequested)
                            .Select(word => word.QuranWordId)
                            .Distinct()
                            .Order()
                            .ToList(),
                        descriptionsByAyah.GetValueOrDefault(ayah.Id, [])))]));
                intentUnits.Add(new LinkingOperationUnitIntent(
                    unit.UnitIdentity,
                    unit.IsGrouped,
                    [.. unitAyahs.Select(ayah =>
                    {
                        var quranAyah = quranAyahs[ayah.AyahId];
                        var ayahWords = wordsByAyah.GetValueOrDefault(ayah.Id, []);
                        return new LinkingOperationAyahIntent(
                            ayah.AyahId,
                            quranAyah.VerseKey,
                            quranAyah.SurahNumber,
                            quranAyah.AyahNumber,
                            ayahWords.Select(word => word.QuranWordId).Distinct().Order().ToList(),
                            descriptionsByAyah.GetValueOrDefault(ayah.Id, []),
                            ParseInvalidReason(ayah.InvalidReason),
                            ayahWords.Where(word => word.IsSourceMatch)
                                .Select(word => word.QuranWordId)
                                .Distinct()
                                .Order()
                                .ToList());
                    })]));
            }

            requestSources.Add(new LinkingOperationSourceRequest(
                descriptor,
                source.ContributionMode,
                source.AutomaticWordMatchesEnabled,
                source.OrderValue,
                source.ExistingContributionId,
                source.ExpectedContributionVersion,
                requestUnits));
            intentSources.Add(new LinkingOperationSourceIntent(
                source.ContributionIdentity,
                source.SourceKind,
                source.Label,
                source.ContributionMode,
                source.AutomaticWordMatchesEnabled,
                source.OrderValue,
                source.TotalAyahCount ?? 0,
                preflight.ReadyAtUtc ?? preflight.UpdatedAtUtc,
                intentUnits,
                intentUnits.SelectMany(unit => unit.Ayahs)
                    .Select(ayah => ayah.InvalidReason)
                    .FirstOrDefault(reason => reason is not null)));
        }

        return new LinkingPreparedConfirmationExecution(
            new LinkingOperationRequest(
                preflight.DoorId,
                preflight.LinkingDataRevision,
                preflight.PreflightToken,
                lease.IdempotencyKey,
                requestSources),
            new LinkingOperationIntent(preflight.DoorId, false, intentSources),
            ayahs.Count);
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

    private static LinkingPreflightInvalidReason? ParseInvalidReason(string? token) => token switch
    {
        null => null,
        "DOOR_ARCHIVED" => LinkingPreflightInvalidReason.DoorArchived,
        "AYAH_OUTSIDE_SOURCE" => LinkingPreflightInvalidReason.AyahOutsideSource,
        "WORD_IS_AYAH_MARKER" => LinkingPreflightInvalidReason.WordIsAyahMarker,
        "WORD_OUTSIDE_AYAH" => LinkingPreflightInvalidReason.WordOutsideAyah,
        _ => throw new InvalidDataException($"Unknown prepared linking invalid reason '{token}'."),
    };

    private sealed record PreparedQuranAyah(
        int Id,
        string VerseKey,
        int SurahNumber,
        int AyahNumber);
}
