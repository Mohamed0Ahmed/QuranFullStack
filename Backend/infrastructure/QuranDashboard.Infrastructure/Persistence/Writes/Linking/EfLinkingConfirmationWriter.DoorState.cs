using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task ApplyDoorStateAsync(
        int actorUserId,
        int doorId,
        IReadOnlySet<int> affectedAyahIds,
        LockedConfirmationState loaded,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var desiredWordsByAyahId = await LoadDesiredDoorWordsAsync(
            doorId,
            affectedAyahIds,
            cancellationToken);
        var doorAyahsByAyahId = loaded.DoorAyahs
            .Where(ayah => affectedAyahIds.Contains(ayah.AyahId))
            .ToDictionary(ayah => ayah.AyahId);

        foreach (var batch in BatchesOf(desiredWordsByAyahId.Keys
                     .Where(ayahId => !doorAyahsByAyahId.ContainsKey(ayahId))))
        {
            var entities = batch.Select(ayahId => new LinkingDoorAyah
            {
                DoorId = doorId,
                AyahId = ayahId,
                CreatedAtUtc = now,
                CreatedBy = actorUserId,
            }).ToList();

            db.LinkingDoorAyahs.AddRange(entities);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);

            foreach (var entity in entities)
            {
                doorAyahsByAyahId.Add(entity.AyahId, entity);
            }

            DetachRange(entities);
        }

        var ayahIdByDoorAyahId = doorAyahsByAyahId.Values.ToDictionary(ayah => ayah.Id, ayah => ayah.AyahId);
        var existingWords = loaded.DoorWords
            .Where(word => ayahIdByDoorAyahId.ContainsKey(word.DoorAyahId))
            .ToDictionary(word => (word.DoorAyahId, word.QuranWordId));
        var removedWords = existingWords.Values.Where(word =>
        {
            var ayahId = ayahIdByDoorAyahId[word.DoorAyahId];
            return desiredWordsByAyahId.ContainsKey(ayahId)
                && !desiredWordsByAyahId[ayahId].Contains(word.QuranWordId);
        });

        foreach (var batch in BatchesOf(removedWords))
        {
            db.LinkingDoorAyahWords.AttachRange(batch);
            db.LinkingDoorAyahWords.RemoveRange(batch);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        }

        var addedWords = desiredWordsByAyahId.SelectMany(entry =>
        {
            var doorAyah = doorAyahsByAyahId[entry.Key];
            return entry.Value
                .Where(wordId => !existingWords.ContainsKey((doorAyah.Id, wordId)))
                .Select(wordId => new LinkingDoorAyahWord
                {
                    DoorAyahId = doorAyah.Id,
                    QuranWordId = wordId,
                    AyahId = entry.Key,
                    CreatedAtUtc = now,
                    CreatedBy = actorUserId,
                });
        });

        foreach (var batch in BatchesOf(addedWords))
        {
            db.LinkingDoorAyahWords.AddRange(batch);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
            DetachRange(batch);
        }

        var removedDoorAyahs = loaded.DoorAyahs
            .Where(ayah => affectedAyahIds.Contains(ayah.AyahId))
            .Where(ayah => !desiredWordsByAyahId.ContainsKey(ayah.AyahId));

        foreach (var batch in BatchesOf(removedDoorAyahs))
        {
            db.LinkingDoorAyahs.AttachRange(batch);
            db.LinkingDoorAyahs.RemoveRange(batch);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        }

        loaded.Door.UpdatedAtUtc = now;
        loaded.Door.UpdatedBy = actorUserId;
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
    }

    private async Task<IReadOnlyDictionary<int, HashSet<int>>> LoadDesiredDoorWordsAsync(
        int doorId,
        IReadOnlySet<int> affectedAyahIds,
        CancellationToken cancellationToken)
    {
        var desiredWordsByAyahId = new Dictionary<int, HashSet<int>>();

        foreach (var batch in BatchesOf(affectedAyahIds))
        {
            var ayahIds = await (
                    from unitAyah in db.LinkingUnitAyahs
                    join unit in db.LinkingUnits on unitAyah.UnitId equals unit.Id
                    join link in db.LinkingSourceContributionUnits on unit.Id equals link.UnitId
                    join contribution in db.LinkingSourceContributions
                        on link.SourceContributionId equals contribution.Id
                    where unit.DoorId == doorId
                          && contribution.DoorId == doorId
                          && contribution.DeletedAtUtc == null
                          && batch.Contains(unitAyah.AyahId)
                    select unitAyah.AyahId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var ayahId in ayahIds)
            {
                desiredWordsByAyahId.TryAdd(ayahId, []);
            }

            if (ayahIds.Count == 0)
            {
                continue;
            }

            var words = await (
                    from word in db.LinkingUnitAyahWords
                    join unitAyah in db.LinkingUnitAyahs on word.UnitAyahId equals unitAyah.Id
                    join unit in db.LinkingUnits on unitAyah.UnitId equals unit.Id
                    join link in db.LinkingSourceContributionUnits on unit.Id equals link.UnitId
                    join contribution in db.LinkingSourceContributions
                        on link.SourceContributionId equals contribution.Id
                    where unit.DoorId == doorId
                          && contribution.DoorId == doorId
                          && contribution.DeletedAtUtc == null
                          && batch.Contains(unitAyah.AyahId)
                    select new UnitWordRow(unitAyah.AyahId, word.QuranWordId))
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var word in words)
            {
                desiredWordsByAyahId[word.AyahId].Add(word.QuranWordId);
            }
        }

        return desiredWordsByAyahId;
    }

    private sealed record UnitWordRow(int AyahId, int QuranWordId);
}
