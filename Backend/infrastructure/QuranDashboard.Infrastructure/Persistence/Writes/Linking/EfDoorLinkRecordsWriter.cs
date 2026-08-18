using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Application.Abstractions.Linking.DoorLinks;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfDoorLinkRecordsWriter(
    QuranDashboardDbContext db,
    AbwabDoorInclusionSyncLock syncLock,
    IAbwabDoorInclusionSynchronizer inclusionSynchronizer) : IDoorLinkRecordsWriter
{
    public async Task<DoorLinkMutationWriteResult> ReplaceWordsAsync(
        int doorId,
        long unitId,
        uint expectedDoorVersion,
        IReadOnlyList<DoorLinkSelectedWord> selectedWords,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var door = await LockDoorAsync(doorId, cancellationToken);
        var invalidDoor = ValidateDoor(door, expectedDoorVersion);
        if (invalidDoor is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return invalidDoor;
        }

        var unitState = await LockLiveUnitAsync(doorId, unitId, cancellationToken);
        if (unitState is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DoorLinkMutationWriteResult.UnitNotFound();
        }

        if (!await SelectedWordsAreValidAsync(unitState.Ayahs, selectedWords, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DoorLinkMutationWriteResult.InvalidWords();
        }

        var existingWords = await LoadSelectedWordsAsync(unitId, cancellationToken);
        var normalizedWords = selectedWords.OrderBy(word => word.AyahId).ThenBy(word => word.QuranWordId).ToList();
        if (existingWords.SequenceEqual(normalizedWords))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DoorLinkMutationWriteResult.Success(
                new DoorLinkMutationDto(0, door!.Version),
                true);
        }

        var affectedContributionIds = await LoadMappedContributionIdsAsync(
            doorId,
            unitId,
            cancellationToken);
        await ReplaceUnitWordsAsync(unitState.Ayahs, normalizedWords, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        await TouchLiveContributionsAsync(
            doorId,
            affectedContributionIds,
            actorUserId,
            now,
            cancellationToken);
        await RebuildDoorAyahsAsync(
            doorId,
            unitState.Ayahs.Select(ayah => ayah.AyahId).ToList(),
            actorUserId,
            false,
            cancellationToken);
        await inclusionSynchronizer.SynchronizeAsync(
            doorId,
            AbwabDoorInclusionMutationSet.Create([], [unitId], [], []),
            actorUserId,
            cancellationToken);
        await BumpDoorAsync(door!, actorUserId, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DoorLinkMutationWriteResult.Success(
            new DoorLinkMutationDto(1, door!.Version),
            false);
    }

    private async Task<bool> SelectedWordsAreValidAsync(
        IReadOnlyList<LinkingUnitAyah> unitAyahs,
        IReadOnlyList<DoorLinkSelectedWord> selectedWords,
        CancellationToken cancellationToken)
    {
        var ayahIds = unitAyahs.Select(ayah => ayah.AyahId).ToHashSet();
        if (selectedWords.Any(word => !ayahIds.Contains(word.AyahId)))
        {
            return false;
        }

        var wordIds = selectedWords.Select(word => word.QuranWordId).ToList();
        var canonicalWords = await db.QuranWords.AsNoTracking()
            .Where(word => wordIds.Contains(word.Id))
            .Select(word => new DoorLinkCanonicalWord(word.Id, word.AyahId, word.IsAyahMarker))
            .ToListAsync(cancellationToken);
        var canonicalById = canonicalWords.ToDictionary(word => word.Id);

        return selectedWords.All(selected =>
            canonicalById.TryGetValue(selected.QuranWordId, out var canonical)
            && !canonical.IsAyahMarker
            && canonical.AyahId == selected.AyahId);
    }

    private async Task<IReadOnlyList<DoorLinkSelectedWord>> LoadSelectedWordsAsync(
        long unitId,
        CancellationToken cancellationToken) =>
        await (
                from unitAyah in db.LinkingUnitAyahs.AsNoTracking()
                join word in db.LinkingUnitAyahWords.AsNoTracking()
                    on unitAyah.Id equals word.UnitAyahId
                where unitAyah.UnitId == unitId
                orderby word.AyahId, word.QuranWordId
                select new DoorLinkSelectedWord(word.AyahId, word.QuranWordId))
            .ToListAsync(cancellationToken);

    private async Task ReplaceUnitWordsAsync(
        IReadOnlyList<LinkingUnitAyah> unitAyahs,
        IReadOnlyList<DoorLinkSelectedWord> selectedWords,
        CancellationToken cancellationToken)
    {
        var unitAyahIds = unitAyahs.Select(ayah => ayah.Id).ToList();
        var existing = await db.LinkingUnitAyahWords
            .Where(word => unitAyahIds.Contains(word.UnitAyahId))
            .ToListAsync(cancellationToken);
        db.LinkingUnitAyahWords.RemoveRange(existing);

        var unitAyahIdByAyahId = unitAyahs.ToDictionary(ayah => ayah.AyahId, ayah => ayah.Id);
        db.LinkingUnitAyahWords.AddRange(selectedWords.Select(word => new LinkingUnitAyahWord
        {
            UnitAyahId = unitAyahIdByAyahId[word.AyahId],
            AyahId = word.AyahId,
            QuranWordId = word.QuranWordId,
        }));
        await SaveChangesAsync(cancellationToken);
    }

    private async Task TouchLiveContributionsAsync(
        int doorId,
        IReadOnlyList<long> contributionIds,
        int actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (contributionIds.Count == 0)
        {
            return;
        }

        await db.LinkingSourceContributions
            .Where(contribution => contributionIds.Contains(contribution.Id)
                && contribution.DoorId == doorId
                && contribution.DeletedAtUtc == null)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(contribution => contribution.UpdatedAtUtc, now)
                    .SetProperty(contribution => contribution.UpdatedBy, actorUserId),
                cancellationToken);
    }

    private sealed record DoorLinkCanonicalWord(int Id, int AyahId, bool IsAyahMarker);
}
