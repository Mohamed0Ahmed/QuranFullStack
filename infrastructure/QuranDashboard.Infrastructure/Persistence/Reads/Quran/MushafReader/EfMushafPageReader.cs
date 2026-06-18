using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Navigation;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

/// <summary>
/// EF read implementation for a single Mushaf page. Mushaf text always comes
/// from <c>quran_words.text_uthmani</c>; division/sajda markers use the
/// first-line rule (<c>MIN(line_number)</c> per ayah on the page).
/// </summary>
public sealed class EfMushafPageReader(QuranDashboardDbContext db) : IMushafPageReader
{
    public async Task<MushafPageResponse?> GetPageAsync(int pageNumber, CancellationToken ct)
    {
        var pageExists = await db.QuranMushafPages
            .AsNoTracking()
            .AnyAsync(p => p.PageNumber == pageNumber, ct);

        if (!pageExists)
        {
            return null;
        }

        var lines = await db.QuranMushafLines
            .AsNoTracking()
            .Where(l => l.PageNumber == pageNumber)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(ct);

        if (lines.Count == 0)
        {
            return null;
        }

        var words = await db.QuranWords
            .AsNoTracking()
            .Where(w => w.PageNumber == pageNumber)
            .Include(w => w.Ayah)
            .OrderBy(w => w.LineNumber)
            .ThenBy(w => w.LineWordOrder)
            .ToListAsync(ct);

        if (words.Count == 0)
        {
            return null;
        }

        var surahNumbers = words.Select(w => w.SurahNumber).Distinct().OrderBy(n => n).ToList();
        var surahNames = await db.QuranSurahs
            .AsNoTracking()
            .Where(s => surahNumbers.Contains(s.SurahNumber))
            .ToDictionaryAsync(s => s.SurahNumber, s => s.NameArabic, ct);

        var surahs = surahNumbers
            .Select(surahNumber =>
            {
                var surahWords = words.Where(w => w.SurahNumber == surahNumber).ToList();
                return new SurahOnPage(
                    surahNumber,
                    surahNames[surahNumber],
                    surahWords.Min(w => w.AyahNumber),
                    surahWords.Max(w => w.AyahNumber));
            })
            .ToList();

        var orderedWords = words
            .OrderBy(w => w.LineNumber)
            .ThenBy(w => w.LineWordOrder)
            .ToList();

        var firstWord = orderedWords[0];
        var lastWord = orderedWords[^1];
        var ayahRange = new AyahRange(firstWord.Ayah.VerseKey, lastWord.Ayah.VerseKey);

        var distinctAyahs = words
            .Select(w => w.Ayah)
            .DistinctBy(a => a.Id)
            .ToList();

        var navigation = new PageNavigationSummary(
            distinctAyahs.Where(a => a.JuzNumber.HasValue).Select(a => (int)a.JuzNumber!.Value).Distinct().OrderBy(n => n).ToList(),
            distinctAyahs.Where(a => a.HizbNumber.HasValue).Select(a => (int)a.HizbNumber!.Value).Distinct().OrderBy(n => n).ToList(),
            distinctAyahs.Where(a => a.RubNumber.HasValue).Select(a => (int)a.RubNumber!.Value).Distinct().OrderBy(n => n).ToList());

        var ayahIds = distinctAyahs.Select(a => a.Id).ToList();

        var wordsByLine = words
            .GroupBy(w => w.LineNumber)
            .ToDictionary(g => g.Key, g => g.OrderBy(w => w.LineWordOrder).ToList());

        var lineDtos = lines.Select(line => new MushafLineDto(
            line.LineNumber,
            MapLineType(line.LineType),
            line.IsCentered,
            line.SurahNumber,
            wordsByLine.TryGetValue(line.LineNumber, out var lineWords)
                ? lineWords.Select(MapWord).ToList()
                : [])).ToList();

        var markers = await BuildMarkersAsync(pageNumber, ayahIds, words, ct);

        return new MushafPageResponse(
            pageNumber,
            null,
            null,
            surahs,
            ayahRange,
            navigation,
            lineDtos,
            markers);
    }

    private async Task<IReadOnlyList<PageMarkerDto>> BuildMarkersAsync(
        int pageNumber,
        List<int> ayahIds,
        List<QuranWord> words,
        CancellationToken ct)
    {
        var ayahFirstWord = words
            .GroupBy(w => w.AyahId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(w => w.LineNumber).ThenBy(w => w.LineWordOrder).First());

        var markers = new List<PageMarkerDto>();

        var juzMarkers = await db.QuranJuzs
            .AsNoTracking()
            .Where(j => ayahIds.Contains(j.FirstAyahId))
            .ToListAsync(ct);
        markers.AddRange(juzMarkers.Select(j => ToMarker("juz", j.JuzNumber, j.FirstAyahId, ayahFirstWord, null)));

        var hizbMarkers = await db.QuranHizbs
            .AsNoTracking()
            .Where(h => ayahIds.Contains(h.FirstAyahId))
            .ToListAsync(ct);
        markers.AddRange(hizbMarkers.Select(h => ToMarker("hizb", h.HizbNumber, h.FirstAyahId, ayahFirstWord, null)));

        var rubMarkers = await db.QuranRubs
            .AsNoTracking()
            .Where(r => ayahIds.Contains(r.FirstAyahId))
            .ToListAsync(ct);
        markers.AddRange(rubMarkers.Select(r => ToMarker("rub", r.RubNumber, r.FirstAyahId, ayahFirstWord, null)));

        var sajdaMarkers = await db.QuranSajdas
            .AsNoTracking()
            .Where(s => ayahIds.Contains(s.AyahId))
            .ToListAsync(ct);
        markers.AddRange(sajdaMarkers.Select(s =>
            ToMarker("sajda", s.SajdahNumber, s.AyahId, ayahFirstWord, MapSajdahType(s.SajdahType))));

        return markers
            .OrderBy(m => m.LineNumber)
            .ThenBy(m => m.MarkerType, StringComparer.Ordinal)
            .ThenBy(m => m.MarkerNumber)
            .ToList();
    }

    private static PageMarkerDto ToMarker(
        string markerType,
        int markerNumber,
        int ayahId,
        IReadOnlyDictionary<int, QuranWord> ayahFirstWord,
        string? sajdahType)
    {
        var word = ayahFirstWord[ayahId];
        return new PageMarkerDto(
            markerType,
            markerNumber,
            word.Ayah.VerseKey,
            word.LineNumber,
            word.Location,
            sajdahType);
    }

    private static MushafWordDto MapWord(QuranWord word) => new(
        word.Location,
        word.Ayah.VerseKey,
        word.WordNumber,
        word.LineWordOrder,
        word.TextUthmani,
        word.IsAyahMarker);

    private static string MapLineType(MushafLineType lineType) => lineType switch
    {
        MushafLineType.Ayah => "ayah",
        MushafLineType.SurahName => "surah_name",
        MushafLineType.Basmallah => "basmallah",
        _ => "ayah",
    };

    private static string MapSajdahType(SajdahType sajdahType) => sajdahType switch
    {
        SajdahType.Required => "required",
        SajdahType.Optional => "optional",
        _ => "required",
    };
}
