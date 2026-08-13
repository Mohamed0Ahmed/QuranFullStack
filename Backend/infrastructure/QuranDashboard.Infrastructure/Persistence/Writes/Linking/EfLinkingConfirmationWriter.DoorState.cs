using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task ApplyDoorStateAsync(
        int actorUserId,
        int doorId,
        LockedConfirmationState loaded,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var unitIds = await db.LinkingUnits
            .Where(unit => unit.DoorId == doorId)
            .Select(unit => unit.Id)
            .ToListAsync(cancellationToken);
        var unitAyahs = await db.LinkingUnitAyahs
            .Where(ayah => unitIds.Contains(ayah.UnitId))
            .Select(ayah => new UnitAyahRow(ayah.Id, ayah.AyahId))
            .ToListAsync(cancellationToken);
        var unitAyahIds = unitAyahs.Select(ayah => ayah.Id).ToList();
        var unitWords = await db.LinkingUnitAyahWords
            .Where(word => unitAyahIds.Contains(word.UnitAyahId))
            .Select(word => new UnitWordRow(word.UnitAyahId, word.QuranWordId))
            .ToListAsync(cancellationToken);
        var desiredWords = unitAyahs
            .GroupJoin(
                unitWords,
                ayah => ayah.Id,
                word => word.UnitAyahId,
                (ayah, words) => new { ayah.AyahId, WordIds = words.Select(word => word.QuranWordId) })
            .GroupBy(entry => entry.AyahId)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(entry => entry.WordIds).ToHashSet());
        var doorAyahsByAyahId = loaded.DoorAyahs.ToDictionary(ayah => ayah.AyahId);

        foreach (var ayahId in desiredWords.Keys.Where(ayahId => !doorAyahsByAyahId.ContainsKey(ayahId)))
        {
            var doorAyah = new LinkingDoorAyah
            {
                DoorId = doorId,
                AyahId = ayahId,
                CreatedAtUtc = now,
                CreatedBy = actorUserId,
            };

            db.LinkingDoorAyahs.Add(doorAyah);
            doorAyahsByAyahId.Add(ayahId, doorAyah);
        }

        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        var existingWords = loaded.DoorWords
            .ToDictionary(word => (word.DoorAyahId, word.QuranWordId));

        foreach (var word in loaded.DoorWords)
        {
            var doorAyah = loaded.DoorAyahs.First(ayah => ayah.Id == word.DoorAyahId);

            if (!desiredWords.GetValueOrDefault(doorAyah.AyahId, []).Contains(word.QuranWordId))
            {
                db.LinkingDoorAyahWords.Remove(word);
            }
        }

        foreach (var (ayahId, wordIds) in desiredWords)
        {
            var doorAyah = doorAyahsByAyahId[ayahId];

            db.LinkingDoorAyahWords.AddRange(wordIds
                .Where(wordId => !existingWords.ContainsKey((doorAyah.Id, wordId)))
                .Select(wordId => new LinkingDoorAyahWord
                {
                    DoorAyahId = doorAyah.Id,
                    QuranWordId = wordId,
                    AyahId = ayahId,
                    CreatedAtUtc = now,
                    CreatedBy = actorUserId,
                }));
        }

        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        db.LinkingDoorAyahs.RemoveRange(
            loaded.DoorAyahs.Where(ayah => !desiredWords.ContainsKey(ayah.AyahId)));

        loaded.Door.UpdatedAtUtc = now;
        loaded.Door.UpdatedBy = actorUserId;
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
    }

    private sealed record UnitAyahRow(long Id, int AyahId);

    private sealed record UnitWordRow(long UnitAyahId, int QuranWordId);
}
