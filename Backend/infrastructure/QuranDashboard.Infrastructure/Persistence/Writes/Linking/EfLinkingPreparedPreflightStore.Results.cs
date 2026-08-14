using System.Text.Json;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingPreparedPreflightStore
{
    private static readonly JsonSerializerOptions PreparedJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<LinkingPreparedResultSummary> PersistPreparedResultAsync(
        LinkingPreparedPreflightLease lease,
        LinkingOperationRequest request,
        LinkingOperationIntent intent,
        LinkingConfirmedDoorState state,
        LinkingOperationClassification classification,
        Func<LinkingPreparedPreflightStage, int, int, int?, CancellationToken, Task<bool>> publishProgress,
        CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Prepared results require the active revision read transaction.");
        }

        await DrainPreparedResultRowsAsync(lease.PreflightId, publishProgress, cancellationToken);
        var sourceRows = await db.LinkingPreparedSources
            .Where(source => source.PreflightId == lease.PreflightId)
            .OrderBy(source => source.OrderValue)
            .ToListAsync(cancellationToken);
        var classificationsByOrder = classification.Sources.ToDictionary(source => source.Source.OrderValue);
        var intentsByOrder = intent.Sources.ToDictionary(source => source.OrderValue);
        var requestSourcesByOrder = request.Sources.ToDictionary(source => source.OrderValue);
        var removedByOrder = intent.Sources.ToDictionary(
            source => source.OrderValue,
            source =>
            {
                var contribution = state.Contributions.FirstOrDefault(candidate =>
                    string.Equals(candidate.SourceIdentity, source.SourceIdentity, StringComparison.Ordinal));
                var desiredAyahIds = source.Units
                    .SelectMany(unit => unit.Ayahs)
                    .Select(ayah => ayah.AyahId)
                    .ToHashSet();
                return contribution?.Units
                    .SelectMany(unit => unit.Ayahs)
                    .Count(ayah => !desiredAyahIds.Contains(ayah.AyahId)) ?? 0;
            });
        var totalRemoved = removedByOrder.Values.Sum();
        var processedSources = 0;
        var processedAyahs = 0;
        var totalAyahs = intent.Sources.Sum(source => source.Units.Sum(unit => unit.Ayahs.Count))
            + totalRemoved;

        foreach (var sourceRow in sourceRows)
        {
            var sourceClassification = classificationsByOrder[sourceRow.OrderValue];
            var sourceIntent = intentsByOrder[sourceRow.OrderValue];
            var sourceRequest = requestSourcesByOrder[sourceRow.OrderValue];
            var oldContribution = state.Contributions.FirstOrDefault(contribution =>
                string.Equals(
                    contribution.SourceIdentity,
                    sourceIntent.SourceIdentity,
                    StringComparison.Ordinal));
            var desiredAyahIds = sourceIntent.Units
                .SelectMany(unit => unit.Ayahs)
                .Select(ayah => ayah.AyahId)
                .ToHashSet();
            var removed = removedByOrder[sourceRow.OrderValue];
            ApplySourceSummary(sourceRow, sourceClassification, removed);

            var classificationByAyahId = sourceClassification.Ayahs.ToDictionary(ayah => ayah.AyahId);
            var requestByAyahId = sourceRequest.Units
                .SelectMany(unit => unit.Ayahs)
                .ToDictionary(ayah => ayah.AyahId);
            processedAyahs = await PersistRequestedAyahsAsync(
                lease,
                sourceRow,
                sourceIntent,
                classificationByAyahId,
                requestByAyahId,
                processedSources,
                processedAyahs,
                totalAyahs,
                publishProgress,
                cancellationToken);

            if (oldContribution is not null)
            {
                processedAyahs = await PersistRemovedAyahsAsync(
                    lease,
                    sourceRow,
                    oldContribution,
                    desiredAyahIds,
                    processedSources,
                    processedAyahs,
                    totalAyahs,
                    publishProgress,
                    cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            processedSources++;
            if (!await publishProgress(
                LinkingPreparedPreflightStage.Persisting,
                processedSources,
                processedAyahs,
                totalAyahs,
                cancellationToken))
            {
                throw new OperationCanceledException(cancellationToken);
            }

            db.Entry(sourceRow).State = EntityState.Detached;
        }

        foreach (var batch in LinkingPreflightToken.AffectedContributionsOf(state, classification)
                     .Chunk(policy.PersistenceBatchSize))
        {
            var rows = batch.Select(contribution => new LinkingPreparedAffectedContribution
            {
                PreflightId = lease.PreflightId,
                ContributionId = contribution.Id,
                ExpectedContributionVersion = contribution.Version,
            }).ToList();
            db.LinkingPreparedAffectedContributions.AddRange(rows);
            await db.SaveChangesAsync(cancellationToken);
            Detach(rows);
            await RequireProgressAsync(
                processedSources,
                processedAyahs,
                totalAyahs,
                publishProgress,
                cancellationToken);
        }

        var totals = classification.Totals with { Removed = classification.Totals.Removed + totalRemoved };
        return new LinkingPreparedResultSummary(
            new LinkingPreflightCountsDto(
                totals.Requested,
                totals.New,
                totals.Overlapping,
                totals.Unchanged,
                totals.Updated,
                totals.Removed,
                totals.Invalid),
            classification.IsNoOp && totalRemoved == 0,
            classification.IsBlocked);
    }

    public async Task<bool> FinalizeReadyAsync(
        LinkingPreparedPreflightLease lease,
        LinkingConfirmedDoorState state,
        LinkingPreparedResultSummary summary,
        string intentHash,
        string preflightToken,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await TakeAdvisoryLockAsync(
            ProcessingLockNamespace,
            ProcessingLockKey(lease.PreflightId),
            cancellationToken);
        var revision = await LockRevisionAsync(transaction, cancellationToken);
        var preflight = await LockLeaseForFinalizationAsync(lease, cancellationToken);
        if (preflight is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        var now = await DatabaseNowAsync(cancellationToken);
        if (preflight.CancellationRequestedAtUtc is not null)
        {
            ApplyTerminalState(
                preflight,
                LinkingPreparedPreflightStatus.Cancelled,
                LinkingPreparedPreflightFailureCode.PreflightCancelled,
                now);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        if (revision != lease.LinkingDataRevision)
        {
            ApplyTerminalState(
                preflight,
                LinkingPreparedPreflightStatus.Stale,
                LinkingPreparedPreflightFailureCode.LinkingDataStale,
                now);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        if (!await PreparedInputsRemainCurrentAsync(lease.PreflightId, state, cancellationToken))
        {
            ApplyTerminalState(
                preflight,
                LinkingPreparedPreflightStatus.Stale,
                LinkingPreparedPreflightFailureCode.PreflightStale,
                now);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        var published = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE linking_prepared_preflights
            SET status = {LinkingPreparedPreflightLifecycleTokens.ToToken(LinkingPreparedPreflightStatus.Ready)},
                stage = {LinkingPreparedPreflightLifecycleTokens.ToToken(LinkingPreparedPreflightStage.Persisting)},
                expected_door_version = {state.DoorVersion},
                intent_hash = {intentHash},
                preflight_token = {preflightToken},
                ready_at_utc = CURRENT_TIMESTAMP,
                expires_at_utc = CURRENT_TIMESTAMP + {policy.ReadyPreflightLifetime},
                is_no_op = {summary.IsNoOp},
                is_blocked = {summary.IsBlocked},
                requested_count = {summary.Counts.Requested},
                new_count = {summary.Counts.New},
                overlapping_count = {summary.Counts.Overlapping},
                unchanged_count = {summary.Counts.Unchanged},
                updated_count = {summary.Counts.Updated},
                removed_count = {summary.Counts.Removed},
                invalid_count = {summary.Counts.Invalid},
                lease_owner = NULL,
                lease_expires_at_utc = NULL,
                updated_at_utc = CURRENT_TIMESTAMP
            WHERE id = {lease.PreflightId}
              AND status = 'preparing'
              AND lease_owner = {lease.LeaseOwner}
              AND attempt_count = {lease.AttemptCount}
              AND lease_expires_at_utc > CURRENT_TIMESTAMP
              AND cancellation_requested_at_utc IS NULL
            """,
            cancellationToken);
        if (published != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task CompleteFailureAsync(
        LinkingPreparedPreflightLease lease,
        LinkingPreparedPreflightFailureCode failureCode,
        bool retryable,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await TakeAdvisoryLockAsync(
            ProcessingLockNamespace,
            ProcessingLockKey(lease.PreflightId),
            cancellationToken);
        var preflight = await LockLeaseForFinalizationAsync(lease, cancellationToken);
        if (preflight is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var now = await DatabaseNowAsync(cancellationToken);
        if (preflight.CancellationRequestedAtUtc is not null)
        {
            preflight.Status = LinkingPreparedPreflightStatus.Cancelled;
            preflight.FailureCode = LinkingPreparedPreflightFailureCode.PreflightCancelled;
            preflight.CompletedAtUtc = now;
        }
        else if (failureCode is LinkingPreparedPreflightFailureCode.LinkingDataStale
            or LinkingPreparedPreflightFailureCode.PreflightStale)
        {
            preflight.Status = LinkingPreparedPreflightStatus.Stale;
            preflight.FailureCode = failureCode;
            preflight.CompletedAtUtc = now;
        }
        else if (retryable && preflight.AttemptCount < policy.MaximumAutomaticAttempts)
        {
            preflight.Status = LinkingPreparedPreflightStatus.Queued;
            preflight.FailureCode = null;
        }
        else
        {
            preflight.Status = LinkingPreparedPreflightStatus.Failed;
            preflight.FailureCode = failureCode;
            preflight.CompletedAtUtc = now;
        }

        preflight.LeaseOwner = null;
        preflight.LeaseExpiresAtUtc = null;
        preflight.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<LinkingPreparedPreflight?> LockLeaseForFinalizationAsync(
        LinkingPreparedPreflightLease lease,
        CancellationToken cancellationToken) =>
        (await db.LinkingPreparedPreflights.FromSqlInterpolated(
                $"""
                SELECT preflight.*, preflight.xmin
                FROM linking_prepared_preflights preflight
                WHERE id = {lease.PreflightId}
                  AND status = 'preparing'
                  AND lease_owner = {lease.LeaseOwner}
                  AND attempt_count = {lease.AttemptCount}
                  AND lease_expires_at_utc > CURRENT_TIMESTAMP
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken))
        .SingleOrDefault();

    private static void ApplySourceSummary(
        LinkingPreparedSource target,
        LinkingSourceClassification source,
        int removed)
    {
        target.Classification = LinkingPreflightTokens.ToToken(source.Classification);
        target.ExistingContributionId = source.ExistingContributionId;
        target.ExpectedContributionVersion = source.ExistingContributionVersion;
        target.TotalAyahCount = source.Source.ResolvedAyahCount;
        target.RequestedCount = source.Counts.Requested;
        target.NewCount = source.Counts.New;
        target.OverlappingCount = source.Counts.Overlapping;
        target.UnchangedCount = source.Counts.Unchanged;
        target.UpdatedCount = source.Counts.Updated;
        target.RemovedCount = source.Counts.Removed + removed;
        target.InvalidCount = source.Counts.Invalid;
    }

    private static IReadOnlyList<LinkingPreparedAyahWord> WordsOf(PersistedAyah persisted)
    {
        var matches = persisted.SourceMatchedWordIds.ToHashSet();
        var requested = persisted.RequestedWordIds.ToHashSet();
        return matches.Union(requested).Order().Select((wordId, index) => new LinkingPreparedAyahWord
        {
            PreparedAyahId = persisted.Ayah.Id,
            QuranWordId = wordId,
            IsSourceMatch = matches.Contains(wordId),
            IsRequested = requested.Contains(wordId),
            OrderValue = index + 1,
        }).ToList();
    }

    private static IReadOnlyList<LinkingPreparedAyahDescription> DescriptionsOf(PersistedAyah persisted) =>
        persisted.Descriptions.Select((body, index) => new LinkingPreparedAyahDescription
        {
            PreparedAyahId = persisted.Ayah.Id,
            OrderValue = index + 1,
            Body = body,
        }).ToList();

    private static string EncodeImpact(LinkingAyahClassification ayah) =>
        JsonSerializer.Serialize(
            new ImpactDocument(
                1,
                ayah.OverlappingSources,
                ayah.WordChanges,
                ayah.DoorWordImpact,
                ayah.DescriptionChanges),
            PreparedJsonOptions);

    private static string EncodeEmptyImpact() =>
        JsonSerializer.Serialize(
            new ImpactDocument(
                1,
                [],
                new LinkingWordChanges([], [], []),
                new LinkingDoorWordImpact([], [], []),
                new LinkingDescriptionChanges([], [], [], [])),
            PreparedJsonOptions);

    private sealed record PersistedAyah(
        LinkingPreparedAyah Ayah,
        IReadOnlyList<int> SourceMatchedWordIds,
        IReadOnlyList<int> RequestedWordIds,
        IReadOnlyList<string> Descriptions);

    private sealed record ImpactDocument(
        int SchemaVersion,
        IReadOnlyList<LinkingOverlappingSource> OverlappingSources,
        LinkingWordChanges WordChanges,
        LinkingDoorWordImpact DoorWordImpact,
        LinkingDescriptionChanges DescriptionChanges);
}
