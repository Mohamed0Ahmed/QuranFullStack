using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingPreparedPreflightStore
{
    private async Task<int> PersistRequestedAyahsAsync(
        LinkingPreparedPreflightLease lease,
        LinkingPreparedSource sourceRow,
        LinkingOperationSourceIntent sourceIntent,
        IReadOnlyDictionary<int, LinkingAyahClassification> classifications,
        IReadOnlyDictionary<int, LinkingOperationAyahRequest> requests,
        int processedSources,
        int processedAyahs,
        int totalAyahs,
        Func<LinkingPreparedPreflightStage, int, int, int?, CancellationToken, Task<bool>> publishProgress,
        CancellationToken cancellationToken)
    {
        var indexedUnits = sourceIntent.Units
            .Select((unit, index) => new IndexedIntentUnit(index + 1, unit))
            .ToList();
        foreach (var unitBatch in indexedUnits.Chunk(policy.PersistenceBatchSize))
        {
            var rows = unitBatch.Select(item => new PreparedUnitRow(
                item,
                new LinkingPreparedUnit
                {
                    PreflightId = lease.PreflightId,
                    SourceId = sourceRow.Id,
                    OrderValue = item.OrderValue,
                    UnitIdentity = item.Unit.Identity,
                    UnitIdentityHash = LinkingSourceIdentity.HashOf(item.Unit.Identity),
                    IsGrouped = item.Unit.IsGrouped,
                })).ToList();
            db.LinkingPreparedUnits.AddRange(rows.Select(item => item.Row));
            await db.SaveChangesAsync(cancellationToken);

            var ayahs = rows.SelectMany(item =>
                item.Unit.Unit.Ayahs.Select((ayah, index) => new PersistedAyah(
                    new LinkingPreparedAyah
                    {
                        PreflightId = lease.PreflightId,
                        SourceId = sourceRow.Id,
                        UnitId = item.Row.Id,
                        IsRequested = true,
                        SourceOrder = sourceRow.OrderValue,
                        UnitOrder = item.Unit.OrderValue,
                        AyahOrder = index + 1,
                        QuranOrder = ayah.AyahId,
                        IsGrouped = item.Unit.Unit.IsGrouped,
                        AyahId = ayah.AyahId,
                        Classification = LinkingPreflightTokens.ToToken(
                            classifications[ayah.AyahId].Classification),
                        InvalidReason = LinkingPreflightTokens.ToToken(
                            classifications[ayah.AyahId].InvalidReason),
                        ClassificationImpactJson = EncodeImpact(classifications[ayah.AyahId]),
                    },
                    ayah.SourceMatchedWordIds,
                    requests[ayah.AyahId].SelectedWordIds,
                    ayah.Descriptions))).ToList();
            foreach (var ayahBatch in ayahs.Chunk(policy.PersistenceBatchSize))
            {
                processedAyahs = await PersistAyahBatchAsync(
                    ayahBatch,
                    processedSources,
                    processedAyahs,
                    totalAyahs,
                    publishProgress,
                    cancellationToken);
            }

            Detach(rows.Select(item => item.Row));
        }

        return processedAyahs;
    }

    private async Task<int> PersistRemovedAyahsAsync(
        LinkingPreparedPreflightLease lease,
        LinkingPreparedSource sourceRow,
        LinkingConfirmedContribution oldContribution,
        IReadOnlySet<int> desiredAyahIds,
        int processedSources,
        int processedAyahs,
        int totalAyahs,
        Func<LinkingPreparedPreflightStage, int, int, int?, CancellationToken, Task<bool>> publishProgress,
        CancellationToken cancellationToken)
    {
        var removed = oldContribution.Units
            .OrderBy(unit => unit.OrderValue)
            .SelectMany(unit => unit.Ayahs
                .Where(ayah => !desiredAyahIds.Contains(ayah.AyahId))
                .OrderBy(ayah => ayah.OrderValue)
                .Select(ayah => new PersistedAyah(
                    new LinkingPreparedAyah
                    {
                        PreflightId = lease.PreflightId,
                        SourceId = sourceRow.Id,
                        IsRequested = false,
                        SourceOrder = sourceRow.OrderValue,
                        UnitOrder = unit.OrderValue,
                        AyahOrder = ayah.OrderValue,
                        QuranOrder = ayah.AyahId,
                        IsGrouped = unit.IsGrouped,
                        AyahId = ayah.AyahId,
                        Classification = LinkingPreflightTokens.ToToken(
                            LinkingPreflightClassification.Remove),
                        ClassificationImpactJson = EncodeEmptyImpact(),
                    },
                    [],
                    [],
                    [])));
        foreach (var batch in removed.Chunk(policy.PersistenceBatchSize))
        {
            processedAyahs = await PersistAyahBatchAsync(
                batch,
                processedSources,
                processedAyahs,
                totalAyahs,
                publishProgress,
                cancellationToken);
        }

        return processedAyahs;
    }

    private async Task<int> PersistAyahBatchAsync(
        IReadOnlyList<PersistedAyah> batch,
        int processedSources,
        int processedAyahs,
        int totalAyahs,
        Func<LinkingPreparedPreflightStage, int, int, int?, CancellationToken, Task<bool>> publishProgress,
        CancellationToken cancellationToken)
    {
        var ayahs = batch.Select(item => item.Ayah).ToList();
        db.LinkingPreparedAyahs.AddRange(ayahs);
        await db.SaveChangesAsync(cancellationToken);
        var words = batch.SelectMany(WordsOf).ToList();
        foreach (var wordBatch in words.Chunk(policy.PersistenceBatchSize))
        {
            db.LinkingPreparedAyahWords.AddRange(wordBatch);
            await db.SaveChangesAsync(cancellationToken);
            Detach(wordBatch);
        }

        var descriptions = batch.SelectMany(DescriptionsOf).ToList();
        foreach (var descriptionBatch in descriptions.Chunk(policy.PersistenceBatchSize))
        {
            db.LinkingPreparedAyahDescriptions.AddRange(descriptionBatch);
            await db.SaveChangesAsync(cancellationToken);
            Detach(descriptionBatch);
        }

        processedAyahs += batch.Count;
        Detach(ayahs);
        await RequireProgressAsync(
            processedSources,
            processedAyahs,
            totalAyahs,
            publishProgress,
            cancellationToken);
        return processedAyahs;
    }

    private static async Task RequireProgressAsync(
        int processedSources,
        int processedAyahs,
        int? totalAyahs,
        Func<LinkingPreparedPreflightStage, int, int, int?, CancellationToken, Task<bool>> publishProgress,
        CancellationToken cancellationToken)
    {
        if (!await publishProgress(
            LinkingPreparedPreflightStage.Persisting,
            processedSources,
            processedAyahs,
            totalAyahs,
            cancellationToken))
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private async Task DrainPreparedResultRowsAsync(
        Guid preflightId,
        Func<LinkingPreparedPreflightStage, int, int, int?, CancellationToken, Task<bool>> publishProgress,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var deleted = await DeletePreparedDescriptionBatchAsync(preflightId, cancellationToken)
                || await DeletePreparedWordBatchAsync(preflightId, cancellationToken)
                || await DeletePreparedAyahBatchAsync(preflightId, cancellationToken)
                || await DeletePreparedUnitBatchAsync(preflightId, cancellationToken)
                || await DeletePreparedAffectedContributionBatchAsync(preflightId, cancellationToken);
            if (!deleted)
            {
                return;
            }

            await RequireProgressAsync(0, 0, null, publishProgress, cancellationToken);
        }
    }

    private async Task<bool> DeletePreparedDescriptionBatchAsync(
        Guid preflightId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from description in db.LinkingPreparedAyahDescriptions
            join ayah in db.LinkingPreparedAyahs on description.PreparedAyahId equals ayah.Id
            where ayah.PreflightId == preflightId
            orderby description.Id
            select description)
            .Take(policy.PersistenceBatchSize)
            .ToListAsync(cancellationToken);
        return await DeleteTrackedBatchAsync(rows, cancellationToken);
    }

    private async Task<bool> DeletePreparedWordBatchAsync(
        Guid preflightId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from word in db.LinkingPreparedAyahWords
            join ayah in db.LinkingPreparedAyahs on word.PreparedAyahId equals ayah.Id
            where ayah.PreflightId == preflightId
            orderby word.PreparedAyahId, word.QuranWordId
            select word)
            .Take(policy.PersistenceBatchSize)
            .ToListAsync(cancellationToken);
        return await DeleteTrackedBatchAsync(rows, cancellationToken);
    }

    private async Task<bool> DeletePreparedAyahBatchAsync(
        Guid preflightId,
        CancellationToken cancellationToken)
    {
        var rows = await db.LinkingPreparedAyahs
            .Where(ayah => ayah.PreflightId == preflightId)
            .OrderBy(ayah => ayah.Id)
            .Take(policy.PersistenceBatchSize)
            .ToListAsync(cancellationToken);
        return await DeleteTrackedBatchAsync(rows, cancellationToken);
    }

    private async Task<bool> DeletePreparedUnitBatchAsync(
        Guid preflightId,
        CancellationToken cancellationToken)
    {
        var rows = await db.LinkingPreparedUnits
            .Where(unit => unit.PreflightId == preflightId)
            .OrderBy(unit => unit.Id)
            .Take(policy.PersistenceBatchSize)
            .ToListAsync(cancellationToken);
        return await DeleteTrackedBatchAsync(rows, cancellationToken);
    }

    private async Task<bool> DeletePreparedAffectedContributionBatchAsync(
        Guid preflightId,
        CancellationToken cancellationToken)
    {
        var rows = await db.LinkingPreparedAffectedContributions
            .Where(contribution => contribution.PreflightId == preflightId)
            .OrderBy(contribution => contribution.ContributionId)
            .Take(policy.PersistenceBatchSize)
            .ToListAsync(cancellationToken);
        return await DeleteTrackedBatchAsync(rows, cancellationToken);
    }

    private async Task<bool> DeleteTrackedBatchAsync<TEntity>(
        IReadOnlyList<TEntity> rows,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (rows.Count == 0)
        {
            return false;
        }

        db.RemoveRange(rows);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
        return true;
    }

    private async Task<bool> PreparedInputsRemainCurrentAsync(
        Guid preflightId,
        LinkingConfirmedDoorState state,
        CancellationToken cancellationToken)
    {
        var doorVersion = await db.AbwabDoors
            .AsNoTracking()
            .Where(door => door.Id == state.DoorId)
            .Select(door => (uint?)door.Version)
            .SingleOrDefaultAsync(cancellationToken);
        if (doorVersion != state.DoorVersion)
        {
            return false;
        }

        var expected = await db.LinkingPreparedAffectedContributions
            .AsNoTracking()
            .Where(contribution => contribution.PreflightId == preflightId)
            .ToDictionaryAsync(
                contribution => contribution.ContributionId,
                contribution => contribution.ExpectedContributionVersion,
                cancellationToken);
        var actual = await db.LinkingSourceContributions
            .AsNoTracking()
            .Where(contribution => expected.Keys.Contains(contribution.Id))
            .ToDictionaryAsync(
                contribution => contribution.Id,
                contribution => contribution.Version,
                cancellationToken);
        return expected.Count == actual.Count
            && expected.All(pair => actual.GetValueOrDefault(pair.Key) == pair.Value);
    }

    private static void ApplyTerminalState(
        LinkingPreparedPreflight preflight,
        LinkingPreparedPreflightStatus status,
        LinkingPreparedPreflightFailureCode failureCode,
        DateTimeOffset now)
    {
        preflight.Status = status;
        preflight.FailureCode = failureCode;
        preflight.CompletedAtUtc = now;
        preflight.LeaseOwner = null;
        preflight.LeaseExpiresAtUtc = null;
        preflight.UpdatedAtUtc = now;
    }

    private void Detach<TEntity>(IEnumerable<TEntity> rows)
        where TEntity : class
    {
        foreach (var row in rows)
        {
            db.Entry(row).State = EntityState.Detached;
        }
    }

    private sealed record IndexedIntentUnit(int OrderValue, LinkingOperationUnitIntent Unit);

    private sealed record PreparedUnitRow(IndexedIntentUnit Unit, LinkingPreparedUnit Row);
}
