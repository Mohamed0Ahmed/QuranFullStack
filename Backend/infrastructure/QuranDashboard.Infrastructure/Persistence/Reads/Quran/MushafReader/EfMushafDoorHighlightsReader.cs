using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

internal sealed class EfMushafDoorHighlightsReader(QuranDashboardDbContext db) : IMushafDoorHighlightsReader
{
    public async Task<MushafDoorHighlightsResponse> GetHighlightsAsync(
        int pageNumber,
        IReadOnlyList<int> doorIds,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(doorIds);

        if (doorIds.Count == 0)
        {
            return new MushafDoorHighlightsResponse(pageNumber, [], [], []);
        }

        var requestedDoorIds = doorIds.ToList();
        var activeDoorIds = await db.AbwabDoors
            .AsNoTracking()
            .Where(door => requestedDoorIds.Contains(door.Id) && door.DeletedAtUtc == null)
            .Select(door => door.Id)
            .ToListAsync(ct);
        var activeDoorIdSet = activeDoorIds.ToHashSet();
        var unavailableDoorIds = requestedDoorIds
            .Where(doorId => !activeDoorIdSet.Contains(doorId))
            .ToList();

        if (activeDoorIds.Count == 0)
        {
            return new MushafDoorHighlightsResponse(pageNumber, [], [], unavailableDoorIds);
        }

        var ayahRows = await (
            from doorAyah in db.LinkingDoorAyahs.AsNoTracking()
            join ayah in db.QuranAyahs.AsNoTracking() on doorAyah.AyahId equals ayah.Id
            where activeDoorIds.Contains(doorAyah.DoorId)
                && ayah.PageFrom <= pageNumber
                && ayah.PageTo >= pageNumber
            select new AyahHighlightRow(
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber,
                doorAyah.DoorId))
            .ToListAsync(ct);

        var wordRows = await (
            from selectedWord in db.LinkingDoorAyahWords.AsNoTracking()
            join doorAyah in db.LinkingDoorAyahs.AsNoTracking()
                on selectedWord.DoorAyahId equals doorAyah.Id
            join quranWord in db.QuranWords.AsNoTracking()
                on selectedWord.QuranWordId equals quranWord.Id
            where activeDoorIds.Contains(doorAyah.DoorId)
                && selectedWord.AyahId == doorAyah.AyahId
                && quranWord.AyahId == selectedWord.AyahId
                && quranWord.PageNumber == pageNumber
            select new WordHighlightRow(
                quranWord.Location,
                quranWord.LineNumber,
                quranWord.LineWordOrder,
                doorAyah.DoorId))
            .ToListAsync(ct);

        var doorOrder = requestedDoorIds
            .Select((doorId, index) => new { doorId, index })
            .ToDictionary(item => item.doorId, item => item.index);
        var ayahs = ayahRows
            .GroupBy(row => new { row.VerseKey, row.SurahNumber, row.AyahNumber })
            .OrderBy(group => group.Key.SurahNumber)
            .ThenBy(group => group.Key.AyahNumber)
            .Select(group => new MushafDoorAyahHighlightDto(
                group.Key.VerseKey,
                OrderDoorIds(group.Select(row => row.DoorId), doorOrder)))
            .ToList();
        var words = wordRows
            .GroupBy(row => new { row.WordLocation, row.LineNumber, row.LineWordOrder })
            .OrderBy(group => group.Key.LineNumber)
            .ThenBy(group => group.Key.LineWordOrder)
            .Select(group => new MushafDoorWordHighlightDto(
                group.Key.WordLocation,
                OrderDoorIds(group.Select(row => row.DoorId), doorOrder)))
            .ToList();

        return new MushafDoorHighlightsResponse(pageNumber, ayahs, words, unavailableDoorIds);
    }

    private static IReadOnlyList<int> OrderDoorIds(
        IEnumerable<int> doorIds,
        IReadOnlyDictionary<int, int> doorOrder) =>
        doorIds
            .Distinct()
            .OrderBy(doorId => doorOrder[doorId])
            .ToList();

    private sealed record AyahHighlightRow(
        string VerseKey,
        short SurahNumber,
        short AyahNumber,
        int DoorId);

    private sealed record WordHighlightRow(
        string WordLocation,
        short LineNumber,
        short LineWordOrder,
        int DoorId);
}
