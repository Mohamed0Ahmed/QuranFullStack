using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

/// <summary>
/// EF Core read model for the Stems Explorer (Feature 016). All queries are
/// read-only and <c>AsNoTracking</c>. The catalogue/summary methods are loaded
/// in one bounded whole-summary aggregation and the later detail methods remain
/// stubbed for subsequent phases. Ayah and words detail are implemented in the
/// corresponding Feature 016 story phases.
/// </summary>
public sealed class EfStemsReader(QuranDashboardDbContext db) : IStemsReader
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

    public Task<StemLemmasResponse?> GetStemLemmasAsync(
        int id,
        CancellationToken cancellationToken)
        => throw new NotImplementedException();

    /// <summary>
    /// Loads the complete stem summary list in a bounded aggregation: identity,
    /// nullable dominant lemma/root relationships, derived counts, first verse
    /// key, and the ordered per-stem POS distribution.
    /// </summary>
    internal async Task<IReadOnlyList<StemSummaryRow>> LoadWholeSummaryAsync(CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                s.id AS "{nameof(StemAggregationRow.Id)}",
                s.stem_text AS "{nameof(StemAggregationRow.StemText)}",
                replace(translate(lower(s.stem_text), @foldFrom, @foldTo), ' ', '') AS "{nameof(StemAggregationRow.NormalizedStemText)}",
                COALESCE(agg.occurrences_count, 0) AS "{nameof(StemAggregationRow.OccurrencesCount)}",
                COALESCE(agg.ayahs_count, 0) AS "{nameof(StemAggregationRow.AyahsCount)}",
                COALESCE(agg.surahs_count, 0) AS "{nameof(StemAggregationRow.SurahsCount)}",
                COALESCE(agg.simple_words_count, 0) AS "{nameof(StemAggregationRow.SimpleWordsCount)}",
                COALESCE(agg.tashkeel_words_count, 0) AS "{nameof(StemAggregationRow.TashkeelWordsCount)}",
                agg.first_surah_number AS "{nameof(StemAggregationRow.FirstSurahNumber)}",
                agg.first_ayah_number AS "{nameof(StemAggregationRow.FirstAyahNumber)}",
                agg.first_word_number AS "{nameof(StemAggregationRow.FirstWordNumber)}",
                s.first_word_order_in_mushaf AS "{nameof(StemAggregationRow.FirstWordOrderInMushaf)}"
            FROM quran_stems s
            LEFT JOIN (
                SELECT
                    m.stem_id AS sid,
                    COUNT(*) AS occurrences_count,
                    COUNT(DISTINCT w.ayah_id) AS ayahs_count,
                    COUNT(DISTINCT w.surah_number) AS surahs_count,
                    COUNT(DISTINCT w.unique_simple_word_id) AS simple_words_count,
                    COUNT(DISTINCT w.unique_tashkeel_word_id) AS tashkeel_words_count,
                    (ARRAY_AGG(w.surah_number ORDER BY w.surah_number, w.ayah_number, w.word_number))[1] AS first_surah_number,
                    (ARRAY_AGG(w.ayah_number ORDER BY w.surah_number, w.ayah_number, w.word_number))[1] AS first_ayah_number,
                    (ARRAY_AGG(w.word_number ORDER BY w.surah_number, w.ayah_number, w.word_number))[1] AS first_word_number
                FROM quran_word_morphology m
                JOIN quran_words w ON w.id = m.quran_word_id
                WHERE m.stem_id IS NOT NULL
                GROUP BY m.stem_id
            ) agg ON agg.sid = s.id
            """;

        var aggregates = await _db.Database.SqlQueryRaw<StemAggregationRow>(
            sql,
            new NpgsqlParameter("foldFrom", StemsListDerivation.ArabicFoldFrom),
            new NpgsqlParameter("foldTo", StemsListDerivation.ArabicFoldTo))
            .ToListAsync(cancellationToken);

        if (aggregates.Count == 0)
        {
            return [];
        }

        var occurrenceRows = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            join t in _db.PosTags.AsNoTracking() on m.HeadPos equals t.Code
            join l in _db.QuranLemmas.AsNoTracking() on m.LemmaId equals l.Id into lemmaJoin
            from l in lemmaJoin.DefaultIfEmpty()
            join r in _db.QuranRoots.AsNoTracking() on m.RootId equals r.Id into rootJoin
            from r in rootJoin.DefaultIfEmpty()
            where m.StemId != null
            select new StemTypeOccurrenceRow(
                m.StemId!.Value,
                m.QuranWordId,
                m.LemmaId,
                l == null ? null : l.LemmaText,
                l == null ? null : l.LemmaBuckwalter,
                m.RootId,
                r == null ? null : r.RootText,
                r == null ? null : r.RootBuckwalter,
                t.Code,
                t.ArabicLabel,
                t.EnglishLabel,
                w.SurahNumber,
                w.AyahNumber,
                w.WordNumber))
            .ToListAsync(cancellationToken);

        var rowsByStem = occurrenceRows
            .GroupBy(r => r.StemId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<StemTypeOccurrenceRow>)g.ToList());

        return aggregates
            .Select(a =>
            {
                var rows = rowsByStem.GetValueOrDefault(a.Id, []);
                var dominantLemma = BuildDominantLemma(rows);
                var dominantRoot = BuildDominantRoot(rows);
                var typeDistribution = MaterializeTypeDistribution(rows);

                return new StemSummaryRow(
                    a.Id,
                    a.StemText,
                    StemsListDerivation.NormalizeArabicQuery(a.StemText) ?? string.Empty,
                    dominantLemma?.Id,
                    dominantLemma?.Text,
                    dominantLemma?.Buckwalter,
                    dominantRoot?.Id,
                    dominantRoot?.Text,
                    dominantRoot?.Buckwalter,
                    a.OccurrencesCount,
                    a.AyahsCount,
                    a.SurahsCount,
                    a.SimpleWordsCount,
                    a.TashkeelWordsCount,
                    BuildFirstVerseKey(a.FirstSurahNumber, a.FirstAyahNumber),
                    a.FirstWordOrderInMushaf,
                    typeDistribution);
            })
            .ToList();
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
