using Microsoft.EntityFrameworkCore;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

/// <summary>
/// EF Core read model for the Stems Explorer (Feature 016). All queries are
/// read-only and <c>AsNoTracking</c>. The catalogue/summary methods are loaded
/// in one bounded whole-summary aggregation and the later detail methods remain
/// stubbed for subsequent phases. Ayah and words detail are implemented in the
/// corresponding Feature 016 story phases.
/// </summary>
public sealed partial class EfStemsReader(QuranDashboardDbContext db) : IStemsReader
{
    private readonly QuranDashboardDbContext _db = db;

    public async Task<PagedResult<StemListItemDto>> GetStemsPageAsync(
        string? search,
        StemSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        return StemsListDerivation.ToPage(all, search, sort, page, pageSize);
    }

    public async Task<StemSummaryDto?> GetStemSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        return StemsListDerivation.ToSummary(all, id);
    }

    public Task<PagedResult<StemWordItemDto>?> GetStemWordsAsync(
        int id,
        StemWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => GetStemWordsPageAsync(id, wordKind, page, pageSize, cancellationToken);

    public async Task<PagedResult<StemAyahMatchDto>?> GetStemAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var stemExists = await _db.QuranStems
            .AsNoTracking()
            .AnyAsync(s => s.Id == id, cancellationToken);
        if (!stemExists)
        {
            return null;
        }

        var matchedAyahIds = _db.WordMorphologies
            .AsNoTracking()
            .Where(m => m.StemId == id)
            .Join(
                _db.QuranWords.AsNoTracking(),
                m => m.QuranWordId,
                w => w.Id,
                (_, w) => w.AyahId)
            .Distinct();

        var totalCount = await matchedAyahIds.CountAsync(cancellationToken);

        var pageAyahs = await (
            from ayah in _db.QuranAyahs.AsNoTracking()
            join surah in _db.QuranSurahs.AsNoTracking()
                on ayah.SurahNumber equals surah.SurahNumber
            where matchedAyahIds.Contains(ayah.Id)
            orderby ayah.SurahNumber, ayah.AyahNumber
            select new AyahMetaRow(
                ayah.Id,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber,
                surah.NameArabic))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (pageAyahs.Count == 0)
        {
            return new PagedResult<StemAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var ayahIds = pageAyahs.Select(a => a.AyahId).ToList();

        var matchedRows = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            where m.StemId == id && ayahIds.Contains(w.AyahId)
            orderby w.SurahNumber, w.AyahNumber, w.WordNumber, w.Id
            select new { w.AyahId, w.Id })
            .ToListAsync(cancellationToken);

        var matchedIdsByAyah = matchedRows
            .GroupBy(r => r.AyahId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(x => x.Id).Distinct().ToList());

        var wordsByAyah = await _db.QuranWords
            .AsNoTracking()
            .Where(w => ayahIds.Contains(w.AyahId))
            .OrderBy(w => w.SurahNumber)
            .ThenBy(w => w.AyahNumber)
            .ThenBy(w => w.WordNumber)
            .Select(w => new AyahWordRow(
                w.AyahId,
                w.Id,
                w.WordNumber,
                w.PageNumber,
                w.TextUthmani,
                w.IsAyahMarker))
            .ToListAsync(cancellationToken);

        var wordsGrouped = wordsByAyah
            .GroupBy(w => w.AyahId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = pageAyahs
            .Select(ayah =>
            {
                var words = wordsGrouped.GetValueOrDefault(ayah.AyahId, []);
                return new StemAyahMatchDto(
                    ayah.AyahId,
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.SurahNameArabic,
                    ayah.AyahNumber,
                    ResolveAyahPageNumber(words),
                    matchedIdsByAyah.GetValueOrDefault(ayah.AyahId, []),
                    words.Select(w => new AyahWordForHighlightDto(
                        w.QuranWordId,
                        w.WordNumber,
                        w.TextUthmani,
                        w.IsAyahMarker)).ToList());
            })
            .ToList();

        return new PagedResult<StemAyahMatchDto>(page, pageSize, totalCount, items);
    }

    public async Task<StemSurahsResponse?> GetStemMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var stem = await _db.QuranStems
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new { s.Id, s.StemText })
            .FirstOrDefaultAsync(cancellationToken);
        if (stem is null)
        {
            return null;
        }

        var surahGroups = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            where m.StemId == id
            group w by w.SurahNumber into g
            orderby g.Key
            select new SurahOccurrenceRow(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        if (surahGroups.Count == 0)
        {
            return new StemSurahsResponse(id, stem.StemText, 0, []);
        }

        var surahNumbers = surahGroups.Select(r => r.SurahNumber).ToList();
        var surahNames = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => surahNumbers.Contains(s.SurahNumber))
            .ToDictionaryAsync(s => s.SurahNumber, s => s.NameArabic, cancellationToken);

        var surahs = surahGroups
            .Select(r => new StemSurahItemDto(r.SurahNumber, surahNames[r.SurahNumber], r.OccurrencesInSurah))
            .ToList();

        return new StemSurahsResponse(id, stem.StemText, surahs.Count, surahs);
    }

    public async Task<StemMissingSurahsResponse?> GetStemMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var stem = await _db.QuranStems
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new { s.Id, s.StemText })
            .FirstOrDefaultAsync(cancellationToken);
        if (stem is null)
        {
            return null;
        }

        var mentionedSurahNumbers = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            where m.StemId == id
            select w.SurahNumber)
            .Distinct()
            .ToListAsync(cancellationToken);

        var missingSurahs = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => !mentionedSurahNumbers.Contains(s.SurahNumber))
            .OrderBy(s => s.SurahNumber)
            .Select(s => new MissingSurahItemDto(s.SurahNumber, s.NameArabic))
            .ToListAsync(cancellationToken);

        return new StemMissingSurahsResponse(id, stem.StemText, missingSurahs.Count, missingSurahs);
    }

    public async Task<StemLemmasResponse?> GetStemLemmasAsync(int id, CancellationToken cancellationToken)
    {
        var stem = await _db.QuranStems
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new { s.Id, s.StemText })
            .FirstOrDefaultAsync(cancellationToken);
        if (stem is null)
        {
            return null;
        }

        var rows = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join l in _db.QuranLemmas.AsNoTracking() on m.LemmaId equals l.Id
            where m.StemId == id && m.LemmaId != null
            select new { m.LemmaId, l.LemmaText, l.LemmaBuckwalter, m.QuranWordId })
            .ToListAsync(cancellationToken);

        var lemmas = MorphologyRelatedItemsOrdering.OrderStemLemmas(
            rows.Select(r => (r.LemmaId!.Value, r.LemmaText, (string?)r.LemmaBuckwalter, r.QuranWordId)));

        return new StemLemmasResponse(id, stem.StemText, lemmas.Count, lemmas);
    }

    private static StemRelationRow? BuildDominantLemma(IReadOnlyList<StemTypeOccurrenceRow> rows)
    {
        return rows
            .Where(r => r.LemmaId.HasValue)
            .GroupBy(r => r.LemmaId!.Value)
            .Select(g =>
            {
                var first = g
                    .OrderBy(x => x.SurahNumber)
                    .ThenBy(x => x.AyahNumber)
                    .ThenBy(x => x.WordNumber)
                    .ThenBy(x => x.QuranWordId)
                    .First();

                return new StemRelationRow(
                    g.Key,
                    first.LemmaText ?? string.Empty,
                    first.LemmaBuckwalter,
                    g.Count(),
                    first.SurahNumber,
                    first.AyahNumber,
                    first.WordNumber);
            })
            .OrderByDescending(r => r.OccurrencesCount)
            .ThenBy(r => r.FirstSurahNumber)
            .ThenBy(r => r.FirstAyahNumber)
            .ThenBy(r => r.FirstWordNumber)
            .ThenBy(r => r.Id)
            .FirstOrDefault();
    }

    private static StemRelationRow? BuildDominantRoot(IReadOnlyList<StemTypeOccurrenceRow> rows)
    {
        return rows
            .Where(r => r.RootId.HasValue)
            .GroupBy(r => r.RootId!.Value)
            .Select(g =>
            {
                var first = g
                    .OrderBy(x => x.SurahNumber)
                    .ThenBy(x => x.AyahNumber)
                    .ThenBy(x => x.WordNumber)
                    .ThenBy(x => x.QuranWordId)
                    .First();

                return new StemRelationRow(
                    g.Key,
                    first.RootText ?? string.Empty,
                    first.RootBuckwalter,
                    g.Count(),
                    first.SurahNumber,
                    first.AyahNumber,
                    first.WordNumber);
            })
            .OrderByDescending(r => r.OccurrencesCount)
            .ThenBy(r => r.FirstSurahNumber)
            .ThenBy(r => r.FirstAyahNumber)
            .ThenBy(r => r.FirstWordNumber)
            .ThenBy(r => r.Id)
            .FirstOrDefault();
    }

    private static IReadOnlyList<StemTypeDistributionRow> MaterializeTypeDistribution(IReadOnlyList<StemTypeOccurrenceRow> rows)
    {
        return rows
            .GroupBy(r => r.Code)
            .Select(g =>
            {
                var first = g
                    .OrderBy(x => x.SurahNumber)
                    .ThenBy(x => x.AyahNumber)
                    .ThenBy(x => x.WordNumber)
                    .ThenBy(x => x.QuranWordId)
                    .First();

                return new StemTypeDistributionRow(
                    g.Key,
                    first.ArabicLabel,
                    first.EnglishLabel,
                    g.Count(),
                    first.SurahNumber,
                    first.AyahNumber,
                    first.WordNumber);
            })
            .OrderByDescending(r => r.OccurrencesCount)
            .ThenBy(r => r.FirstSurahNumber)
            .ThenBy(r => r.FirstAyahNumber)
            .ThenBy(r => r.FirstWordNumber)
            .ThenBy(r => r.Code, StringComparer.Ordinal)
            .ToList();
    }

    private static string BuildFirstVerseKey(int? firstSurahNumber, int? firstAyahNumber) =>
        firstSurahNumber is > 0 && firstAyahNumber is > 0
            ? $"{firstSurahNumber}:{firstAyahNumber}"
            : string.Empty;

    private async Task<PagedResult<StemWordItemDto>?> GetStemWordsPageAsync(
        int id,
        StemWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var stemExists = await _db.QuranStems
            .AsNoTracking()
            .AnyAsync(s => s.Id == id, cancellationToken);
        if (!stemExists)
        {
            return null;
        }

        var rows = wordKind == StemWordKind.Simple
            ? await LoadStemWordRowsAsync(id, useSimpleWordIds: true, cancellationToken)
            : await LoadStemWordRowsAsync(id, useSimpleWordIds: false, cancellationToken);

        var grouped = rows
            .Where(r => r.UniqueWordId.HasValue)
            .GroupBy(r => r.UniqueWordId!.Value)
            .Select(g =>
            {
                var first = g
                    .OrderBy(x => x.SurahNumber)
                    .ThenBy(x => x.AyahNumber)
                    .ThenBy(x => x.WordNumber)
                    .ThenBy(x => x.QuranWordId)
                    .First();

                return new StemWordGroupRow(
                    g.Key,
                    first.DisplayTextUthmani,
                    g.Count(),
                    first.SurahNumber,
                    first.AyahNumber,
                    first.WordNumber);
            })
            .OrderBy(x => x.FirstSurahNumber)
            .ThenBy(x => x.FirstAyahNumber)
            .ThenBy(x => x.FirstWordNumber)
            .ThenBy(x => x.UniqueWordId)
            .ToList();

        var items = grouped
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new StemWordItemDto(
                row.UniqueWordId,
                wordKind == StemWordKind.Simple ? StemWordKindKeys.Simple : StemWordKindKeys.Tashkeel,
                row.DisplayTextUthmani,
                row.OccurrencesCount,
                BuildFirstVerseKey(row.FirstSurahNumber, row.FirstAyahNumber)))
            .ToList();

        return new PagedResult<StemWordItemDto>(page, pageSize, grouped.Count, items);
    }

    private async Task<IReadOnlyList<StemWordOccurrenceRow>> LoadStemWordRowsAsync(
        int id,
        bool useSimpleWordIds,
        CancellationToken cancellationToken)
    {
        return useSimpleWordIds
            ? await (
                from m in _db.WordMorphologies.AsNoTracking()
                join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
                where m.StemId == id
                select new StemWordOccurrenceRow(
                    w.UniqueSimpleWordId,
                    w.TextUthmani,
                    w.SurahNumber,
                    w.AyahNumber,
                    w.WordNumber,
                    w.Id))
                .ToListAsync(cancellationToken)
            : await (
                from m in _db.WordMorphologies.AsNoTracking()
                join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
                where m.StemId == id
                select new StemWordOccurrenceRow(
                    w.UniqueTashkeelWordId,
                    w.TextUthmani,
                    w.SurahNumber,
                    w.AyahNumber,
                    w.WordNumber,
                    w.Id))
                .ToListAsync(cancellationToken);
    }

    private sealed record StemAggregationRow(
        int Id,
        string StemText,
        string NormalizedStemText,
        int OccurrencesCount,
        int AyahsCount,
        int SurahsCount,
        int SimpleWordsCount,
        int TashkeelWordsCount,
        int? FirstSurahNumber,
        int? FirstAyahNumber,
        int? FirstWordNumber,
        int FirstWordOrderInMushaf);

    private sealed record StemTypeOccurrenceRow(
        int StemId,
        int QuranWordId,
        int? LemmaId,
        string? LemmaText,
        string? LemmaBuckwalter,
        int? RootId,
        string? RootText,
        string? RootBuckwalter,
        string Code,
        string ArabicLabel,
        string EnglishLabel,
        int SurahNumber,
        int AyahNumber,
        int WordNumber);

    private sealed record StemRelationRow(
        int Id,
        string Text,
        string? Buckwalter,
        int OccurrencesCount,
        int FirstSurahNumber,
        int FirstAyahNumber,
        int FirstWordNumber);

    private sealed record AyahMetaRow(
        int AyahId,
        string VerseKey,
        int SurahNumber,
        int AyahNumber,
        string SurahNameArabic);

    private sealed record AyahWordRow(
        int AyahId,
        int QuranWordId,
        int WordNumber,
        short PageNumber,
        string TextUthmani,
        bool IsAyahMarker);

    private sealed record StemWordOccurrenceRow(
        int? UniqueWordId,
        string DisplayTextUthmani,
        int SurahNumber,
        int AyahNumber,
        int WordNumber,
        int QuranWordId);

    private sealed record StemWordGroupRow(
        int UniqueWordId,
        string DisplayTextUthmani,
        int OccurrencesCount,
        int FirstSurahNumber,
        int FirstAyahNumber,
        int FirstWordNumber);

    private sealed record SurahOccurrenceRow(short SurahNumber, int OccurrencesInSurah);

    private static short ResolveAyahPageNumber(IReadOnlyList<AyahWordRow> words)
    {
        var firstReadableWord = words.FirstOrDefault(w => !w.IsAyahMarker);
        if (firstReadableWord is not null)
        {
            return firstReadableWord.PageNumber;
        }

        return words.FirstOrDefault()?.PageNumber ?? 0;
    }
}
