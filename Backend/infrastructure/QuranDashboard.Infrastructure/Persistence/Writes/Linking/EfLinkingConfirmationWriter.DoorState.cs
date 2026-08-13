using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task ApplyDoorStateAsync(
        int actorUserId,
        LinkingOperationIntent intent,
        LockedConfirmationState loaded,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var submittedAyahs = intent.Sources
            .SelectMany(source => source.Units)
            .SelectMany(unit => unit.Ayahs)
            .GroupBy(ayah => ayah.AyahId)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(ayah => ayah.WordIds).Distinct().Order().ToList());
        var doorAyahsByAyahId = loaded.DoorAyahs.ToDictionary(ayah => ayah.AyahId);

        foreach (var ayahId in submittedAyahs.Keys.Where(ayahId => !doorAyahsByAyahId.ContainsKey(ayahId)))
        {
            var doorAyah = new LinkingDoorAyah
            {
                DoorId = intent.DoorId,
                AyahId = ayahId,
                CreatedAtUtc = now,
                CreatedBy = actorUserId,
            };

            db.LinkingDoorAyahs.Add(doorAyah);
            doorAyahsByAyahId.Add(ayahId, doorAyah);
        }

        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        var existingWords = loaded.DoorWords
            .Select(word => (word.DoorAyahId, word.QuranWordId))
            .ToHashSet();

        foreach (var (ayahId, wordIds) in submittedAyahs)
        {
            var doorAyah = doorAyahsByAyahId[ayahId];

            db.LinkingDoorAyahWords.AddRange(wordIds
                .Where(wordId => existingWords.Add((doorAyah.Id, wordId)))
                .Select(wordId => new LinkingDoorAyahWord
                {
                    DoorAyahId = doorAyah.Id,
                    QuranWordId = wordId,
                    AyahId = ayahId,
                    CreatedAtUtc = now,
                    CreatedBy = actorUserId,
                }));
        }

        loaded.Door.UpdatedAtUtc = now;
        loaded.Door.UpdatedBy = actorUserId;
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
    }
}
