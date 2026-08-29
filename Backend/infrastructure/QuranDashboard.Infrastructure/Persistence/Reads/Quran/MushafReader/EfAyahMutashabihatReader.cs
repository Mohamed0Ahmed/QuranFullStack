using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Mutashabihat;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

public sealed class EfAyahMutashabihatReader(QuranDashboardDbContext db) : IAyahMutashabihatReader
{
    public async Task<AyahMutashabihatResponse?> GetAyahMutashabihatAsync(string verseKey, CancellationToken ct)
    {
        var selectedAyah = await db.QuranAyahs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.VerseKey == verseKey, ct);

        if (selectedAyah is null)
        {
            return null;
        }

        var selectedOccurrences = await db.MutashabihatOccurrences
            .AsNoTracking()
            .Where(occurrence => occurrence.AyahId == selectedAyah.Id)
            .ToListAsync(ct);

        if (selectedOccurrences.Count == 0)
        {
            return new AyahMutashabihatResponse(verseKey, 0, []);
        }

        var groupIds = selectedOccurrences
            .Select(occurrence => occurrence.GroupId)
            .Distinct()
            .ToList();

        var groups = await db.MutashabihatGroups
            .AsNoTracking()
            .Where(group => groupIds.Contains(group.Id))
            .ToListAsync(ct);

        var allOccurrences = await db.MutashabihatOccurrences
            .AsNoTracking()
            .Where(occurrence => groupIds.Contains(occurrence.GroupId))
            .ToListAsync(ct);

        var ayahIds = allOccurrences
            .Select(occurrence => occurrence.AyahId)
            .Concat(groups.Select(group => group.RepresentativeAyahId))
            .Distinct()
            .ToList();

        var ayahs = await db.QuranAyahs
            .AsNoTracking()
            .Include(ayah => ayah.Surah)
            .Where(ayah => ayahIds.Contains(ayah.Id))
            .ToDictionaryAsync(ayah => ayah.Id, ct);

        var wordsByAyah = await LoadWordsByAyahAsync(ayahIds, ct);

        var groupDtos = groups
            .OrderBy(group => group.SourceGroupId)
            .Select(group => MapGroup(
                group,
                selectedAyah.Id,
                verseKey,
                allOccurrences,
                ayahs,
                wordsByAyah))
            .ToList();

        return new AyahMutashabihatResponse(verseKey, groupDtos.Count, groupDtos);
    }

    private async Task<IReadOnlyDictionary<int, IReadOnlyList<MutashabihatWordRow>>> LoadWordsByAyahAsync(
        IReadOnlyList<int> ayahIds,
        CancellationToken ct)
    {

        var words = await db.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .OrderBy(word => word.WordNumber)
            .Select(word => new MutashabihatWordRow(
                word.Id,
                word.AyahId,
                word.WordNumber,
                word.TextUthmani))
            .ToListAsync(ct);

        return words
            .GroupBy(word => word.AyahId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<MutashabihatWordRow>)group.ToList());
    }

    private static MutashabihatGroupDto MapGroup(
        MutashabihatGroup group,
        int selectedAyahId,
        string selectedVerseKey,
        IReadOnlyList<MutashabihatOccurrence> allOccurrences,
        IReadOnlyDictionary<int, Ayah> ayahs,
        IReadOnlyDictionary<int, IReadOnlyList<MutashabihatWordRow>> wordsByAyah)
    {
        var groupOccurrences = allOccurrences
            .Where(occurrence => occurrence.GroupId == group.Id)
            .ToList();

        var representativeAyah = ayahs[group.RepresentativeAyahId];
        var representativePhrase = DerivePhraseText(
            wordsByAyah,
            group.RepresentativeAyahId,
            group.RepresentativeWordFrom,
            group.RepresentativeWordTo);

        var selectedOccurrenceDtos = groupOccurrences
            .Where(occurrence => occurrence.AyahId == selectedAyahId)
            .OrderBy(occurrence => occurrence.WordFrom)
            .Select(occurrence => new MutashabihatSelectedOccurrenceDto(
                selectedVerseKey,
                occurrence.WordFrom,
                occurrence.WordTo,
                occurrence.IsRepresentative,
                DerivePhraseText(wordsByAyah, occurrence.AyahId, occurrence.WordFrom, occurrence.WordTo)))
            .ToList();

        var occurrenceDtos = groupOccurrences
            .Select(occurrence => MapOccurrence(occurrence, selectedAyahId, ayahs, wordsByAyah))
            .OrderBy(occurrence => occurrence.SurahNumber)
            .ThenBy(occurrence => occurrence.AyahNumber)
            .ThenBy(occurrence => occurrence.WordFrom)
            .ToList();

        return new MutashabihatGroupDto(
            MutashabihatGroupKeys.FromSourceGroupId(group.SourceGroupId),
            group.SourceGroupId,
            representativeAyah.VerseKey,
            group.RepresentativeWordFrom,
            group.RepresentativeWordTo,
            representativePhrase,
            group.OccurrenceCount,
            group.DistinctAyahCount,
            group.DistinctSurahCount,
            selectedOccurrenceDtos,
            occurrenceDtos);
    }

    private static MutashabihatOccurrenceDto MapOccurrence(
        MutashabihatOccurrence occurrence,
        int selectedAyahId,
        IReadOnlyDictionary<int, Ayah> ayahs,
        IReadOnlyDictionary<int, IReadOnlyList<MutashabihatWordRow>> wordsByAyah)
    {
        var ayah = ayahs[occurrence.AyahId];

        return new MutashabihatOccurrenceDto(
            occurrence.AyahId,
            ayah.VerseKey,
            ayah.SurahNumber,
            ayah.Surah.NameArabic,
            ayah.AyahNumber,
            ayah.PageFrom,
            occurrence.WordFrom,
            occurrence.WordTo,
            occurrence.AyahId == selectedAyahId,
            occurrence.IsRepresentative,
            ayah.TextUthmani,
            DerivePhraseText(wordsByAyah, occurrence.AyahId, occurrence.WordFrom, occurrence.WordTo),
            MatchedQuranWordIds(wordsByAyah, occurrence.AyahId, occurrence.WordFrom, occurrence.WordTo));
    }

    private static string? DerivePhraseText(
        IReadOnlyDictionary<int, IReadOnlyList<MutashabihatWordRow>> wordsByAyah,
        int ayahId,
        int wordFrom,
        int wordTo)
    {
        if (!wordsByAyah.TryGetValue(ayahId, out var words))
        {
            return null;
        }

        var expectedCount = wordTo - wordFrom + 1;
        var selectedWords = words
            .Where(word => word.WordNumber >= wordFrom && word.WordNumber <= wordTo)
            .ToList();

        if (selectedWords.Count != expectedCount)
        {
            return null;
        }

        return string.Join(' ', selectedWords.Select(word => word.TextUthmani));
    }

    private static IReadOnlyList<int> MatchedQuranWordIds(
        IReadOnlyDictionary<int, IReadOnlyList<MutashabihatWordRow>> wordsByAyah,
        int ayahId,
        int wordFrom,
        int wordTo)
    {
        if (!wordsByAyah.TryGetValue(ayahId, out var words))
        {
            return [];
        }

        var expectedCount = wordTo - wordFrom + 1;
        var selectedWords = words
            .Where(word => word.WordNumber >= wordFrom && word.WordNumber <= wordTo)
            .ToList();

        return selectedWords.Count == expectedCount
            ? [.. selectedWords.Select(word => word.Id)]
            : [];
    }

    private sealed record MutashabihatWordRow(
        int Id,
        int AyahId,
        int WordNumber,
        string TextUthmani);
}
