using System.Data;
using System.Text.Json;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Reads.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingPreparedPreflightStore
{
    public async Task<LinkingPreparedDetailPageDto?> GetDetailPageAsync(
        int actorUserId,
        Guid preflightId,
        long? preparedSourceId,
        string filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);
        var revision = await LockRevisionAsync(transaction, cancellationToken);
        var preflight = await db.LinkingPreparedPreflights
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == preflightId
                    && candidate.ActorUserId == actorUserId
                    && candidate.CleanupStartedAtUtc == null,
                cancellationToken);
        if (preflight is null)
        {
            return null;
        }

        EnsureDetailsAvailable(
            preflight,
            revision,
            await DatabaseNowAsync(cancellationToken));
        if (preparedSourceId is { } sourceId
            && !await db.LinkingPreparedSources.AsNoTracking().AnyAsync(
                source => source.Id == sourceId && source.PreflightId == preflightId,
                cancellationToken))
        {
            return null;
        }

        var candidateRows = db.LinkingPreparedAyahs.AsNoTracking()
            .Where(ayah => ayah.PreflightId == preflightId);
        if (preparedSourceId is { } selectedSourceId)
        {
            candidateRows = candidateRows.Where(ayah => ayah.SourceId == selectedSourceId);
        }

        if (string.Equals(
                filter,
                LinkingPreparedDetailFilters.ExistingFilter,
                StringComparison.Ordinal))
        {
            var overlap = LinkingPreflightTokens.ToToken(LinkingPreflightClassification.OverlapOtherSource);
            var unchanged = LinkingPreflightTokens.ToToken(LinkingPreflightClassification.Unchanged);
            candidateRows = candidateRows.Where(ayah =>
                ayah.Classification == overlap || ayah.Classification == unchanged);
        }
        else if (!string.Equals(
                     filter,
                     LinkingPreparedDetailFilters.AllFilter,
                     StringComparison.Ordinal))
        {
            candidateRows = candidateRows.Where(ayah => ayah.Classification == filter);
        }

        var candidates = candidateRows
            .Select(ayah => new { ayah.QuranOrder, ayah.AyahId })
            .Distinct();
        var totalItems = await candidates.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (totalItems + pageSize - 1) / pageSize;
        if (totalPages > 0 && page > totalPages)
        {
            throw new LinkingPageOutOfRangeException(page);
        }

        var pageKeys = await candidates
            .OrderBy(item => item.QuranOrder)
            .ThenBy(item => item.AyahId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var ayahIds = pageKeys.Select(item => item.AyahId).ToList();
        var overlaysQuery = db.LinkingPreparedAyahs.AsNoTracking()
            .Where(ayah => ayah.PreflightId == preflightId && ayahIds.Contains(ayah.AyahId));
        if (preparedSourceId is { } overlaySourceId)
        {
            overlaysQuery = overlaysQuery.Where(ayah => ayah.SourceId == overlaySourceId);
        }

        var overlays = await overlaysQuery
            .OrderBy(ayah => ayah.QuranOrder)
            .ThenBy(ayah => ayah.SourceOrder)
            .ToListAsync(cancellationToken);
        var overlayIds = overlays.Select(overlay => overlay.Id).ToList();
        var words = await db.LinkingPreparedAyahWords.AsNoTracking()
            .Where(word => overlayIds.Contains(word.PreparedAyahId))
            .OrderBy(word => word.PreparedAyahId)
            .ThenBy(word => word.OrderValue)
            .ToListAsync(cancellationToken);
        var descriptions = await db.LinkingPreparedAyahDescriptions.AsNoTracking()
            .Where(description => overlayIds.Contains(description.PreparedAyahId))
            .OrderBy(description => description.PreparedAyahId)
            .ThenBy(description => description.OrderValue)
            .ToListAsync(cancellationToken);
        var hydrated = await LinkingAyahHydration.ProjectAsync(
            db,
            await LinkingAyahHydration.LoadByIdsAsync(db, ayahIds, cancellationToken),
            new Dictionary<int, IReadOnlyList<int>>(),
            true,
            cancellationToken);
        var wordsByOverlay = words
            .GroupBy(word => word.PreparedAyahId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<LinkingPreparedAyahWord>)[.. group]);
        var descriptionsByOverlay = descriptions
            .GroupBy(description => description.PreparedAyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<LinkingPreparedAyahDescription>)[.. group]);
        var overlaysByAyahId = overlays
            .GroupBy(overlay => overlay.AyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<LinkingPreparedAyahOverlayDto>)group
                    .Select(overlay => ProjectOverlay(
                        overlay,
                        wordsByOverlay.GetValueOrDefault(overlay.Id, []),
                        descriptionsByOverlay.GetValueOrDefault(overlay.Id, [])))
                    .ToList());
        var orderByAyahId = pageKeys
            .Select((key, index) => new { key.AyahId, Index = index })
            .ToDictionary(item => item.AyahId, item => item.Index);
        var items = hydrated
            .OrderBy(ayah => orderByAyahId[ayah.AyahId])
            .Select(ayah => new LinkingPreparedDetailItemDto(
                ayah,
                overlaysByAyahId.GetValueOrDefault(ayah.AyahId, [])))
            .ToList();
        await transaction.CommitAsync(cancellationToken);
        return new LinkingPreparedDetailPageDto(
            preflightId,
            revision,
            preparedSourceId is null ? "merged" : "source",
            preparedSourceId,
            filter,
            page,
            pageSize,
            totalItems,
            totalPages,
            items);
    }

    private static void EnsureDetailsAvailable(
        LinkingPreparedPreflight preflight,
        long currentRevision,
        DateTimeOffset now)
    {
        if (preflight.LinkingDataRevision != currentRevision)
        {
            throw new LinkingDataStaleException(preflight.LinkingDataRevision, currentRevision);
        }

        if (preflight.Status == LinkingPreparedPreflightStatus.Ready
            && preflight.ExpiresAtUtc <= now)
        {
            throw new LinkingPreparedPreflightLifecycleException(
                LinkingPreparedPreflightFailureCode.PreflightExpired,
                true);
        }

        if (preflight.Status is LinkingPreparedPreflightStatus.Ready
            or LinkingPreparedPreflightStatus.Confirmed)
        {
            return;
        }

        var failureCode = preflight.Status is LinkingPreparedPreflightStatus.Queued
            or LinkingPreparedPreflightStatus.Preparing
            ? LinkingPreparedPreflightFailureCode.PreflightNotReady
            : preflight.FailureCode ?? LinkingPreparedPreflightFailureCode.PreparationFailed;
        throw new LinkingPreparedPreflightLifecycleException(
            failureCode,
            preflight.Status == LinkingPreparedPreflightStatus.Expired);
    }

    private static LinkingPreparedAyahOverlayDto ProjectOverlay(
        LinkingPreparedAyah overlay,
        IReadOnlyList<LinkingPreparedAyahWord> words,
        IReadOnlyList<LinkingPreparedAyahDescription> descriptions)
    {
        var impact = JsonSerializer.Deserialize<ImpactDocument>(
            overlay.ClassificationImpactJson,
            PreparedJsonOptions)
            ?? throw new InvalidOperationException("The prepared linking impact document is empty.");
        return new LinkingPreparedAyahOverlayDto(
            overlay.SourceId,
            overlay.SourceOrder,
            overlay.UnitId,
            overlay.IsRequested,
            overlay.UnitOrder,
            overlay.AyahOrder,
            overlay.IsGrouped,
            overlay.Classification,
            overlay.InvalidReason,
            [.. words.Where(word => word.IsSourceMatch).Select(word => word.QuranWordId)],
            [.. words.Where(word => word.IsRequested).Select(word => word.QuranWordId)],
            [.. descriptions.Select(description => description.Body)],
            [.. impact.OverlappingSources.Select(source => new LinkingOverlappingSourceDto(
                source.SourceIdentity,
                source.Label,
                source.SourceKind))],
            new LinkingWordChangesDto(
                impact.WordChanges.Added,
                impact.WordChanges.Removed,
                impact.WordChanges.Unchanged),
            new LinkingDoorWordImpactDto(
                impact.DoorWordImpact.Added,
                impact.DoorWordImpact.Existing,
                impact.DoorWordImpact.Removed),
            new LinkingDescriptionChangesDto(
                impact.DescriptionChanges.Added,
                impact.DescriptionChanges.Removed,
                impact.DescriptionChanges.Changed,
                impact.DescriptionChanges.Unchanged));
    }
}
