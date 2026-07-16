using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

public sealed class EfMushafPageReader(QuranDashboardDbContext db) : IMushafPageReader
{
    public async Task<MushafPageResponse?> GetPageAsync(int pageNumber, CancellationToken ct)
    {
        // The line read doubles as the page-existence check: a page with no lines is
        // indistinguishable from a page that does not exist, so the prior separate
        // AnyAsync existence probe is redundant and has been removed.
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
            .OrderBy(w => w.LineNumber)
            .ThenBy(w => w.LineWordOrder)
            .Select(w => new PageWordRow(
                w.Location,
                w.AyahId,
                w.Ayah.VerseKey,
                w.SurahNumber,
                w.AyahNumber,
                w.WordNumber,
                w.LineNumber,
                w.LineWordOrder,
                w.TextUthmani,
                w.IsAyahMarker,
                w.Ayah.JuzNumber,
                w.Ayah.HizbNumber,
                w.Ayah.RubNumber))
            .ToListAsync(ct);

        if (words.Count == 0)
        {
            return null;
        }

        var surahNumbers = words.Select(w => w.SurahNumber).Distinct().OrderBy(n => n).ToList();
        var surahNames = await db.QuranSurahs
            .AsNoTracking()
            .Where(s => surahNumbers.Contains(s.SurahNumber))
            .Select(s => new { s.SurahNumber, s.NameArabic })
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
        var ayahRange = new AyahRange(firstWord.VerseKey, lastWord.VerseKey);

        var navigation = new PageNavigationSummary(
            words.Where(w => w.JuzNumber.HasValue).Select(w => (int)w.JuzNumber!.Value).Distinct().OrderBy(n => n).ToList(),
            words.Where(w => w.HizbNumber.HasValue).Select(w => (int)w.HizbNumber!.Value).Distinct().OrderBy(n => n).ToList(),
            words.Where(w => w.RubNumber.HasValue).Select(w => (int)w.RubNumber!.Value).Distinct().OrderBy(n => n).ToList());

        var ayahIds = words.Select(w => w.AyahId).Distinct().ToList();

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
        List<PageWordRow> words,
        CancellationToken ct)
    {
        var ayahFirstWord = words
            .GroupBy(w => w.AyahId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(w => w.LineNumber).ThenBy(w => w.LineWordOrder).First());

        var markers = new List<PageMarkerDto>();

        // Juz, hizb, and rub markers share an identical shape (a number keyed by first_ayah_id),
        // so they are combined into one UNION ALL projection instead of three separate reads.
        // Sajda markers carry an extra SajdahType column with its own value-converted storage
        // representation, so they are kept as their own read rather than folded into the union;
        // that keeps sajda type/number semantics exactly as they were, at the cost of one extra
        // command, per the "smallest safe reduction" guidance for this finding.
        var juzRows = db.QuranJuzs
            .AsNoTracking()
            .Where(j => ayahIds.Contains(j.FirstAyahId))
            .Select(j => new { MarkerType = "juz", MarkerNumber = j.JuzNumber, AyahId = j.FirstAyahId });

        var hizbRows = db.QuranHizbs
            .AsNoTracking()
            .Where(h => ayahIds.Contains(h.FirstAyahId))
            .Select(h => new { MarkerType = "hizb", MarkerNumber = h.HizbNumber, AyahId = h.FirstAyahId });

        var rubRows = db.QuranRubs
            .AsNoTracking()
            .Where(r => ayahIds.Contains(r.FirstAyahId))
            .Select(r => new { MarkerType = "rub", MarkerNumber = r.RubNumber, AyahId = r.FirstAyahId });

        var numberedMarkers = await juzRows.Concat(hizbRows).Concat(rubRows).ToListAsync(ct);
        markers.AddRange(numberedMarkers.Select(m =>
            ToMarker(m.MarkerType, m.MarkerNumber, m.AyahId, ayahFirstWord, null)));

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
        IReadOnlyDictionary<int, PageWordRow> ayahFirstWord,
        string? sajdahType)
    {
        var word = ayahFirstWord[ayahId];
        return new PageMarkerDto(
            markerType,
            markerNumber,
            word.VerseKey,
            word.LineNumber,
            word.Location,
            sajdahType);
    }

    private static MushafWordDto MapWord(PageWordRow word) => new(
        word.Location,
        word.VerseKey,
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

    private sealed record PageWordRow(
        string Location,
        int AyahId,
        string VerseKey,
        short SurahNumber,
        short AyahNumber,
        short WordNumber,
        short LineNumber,
        short LineWordOrder,
        string TextUthmani,
        bool IsAyahMarker,
        short? JuzNumber,
        short? HizbNumber,
        short? RubNumber);
}
