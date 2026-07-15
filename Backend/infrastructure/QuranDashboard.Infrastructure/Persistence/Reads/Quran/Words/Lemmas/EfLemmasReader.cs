using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;

/// <summary>
/// EF Core read model for the Lemmas Explorer (Feature 016). All queries are
/// read-only and <c>AsNoTracking</c>. The lemma catalogue (list/summary) is
/// implemented in T032/T033 as a single bounded whole-summary aggregation with
/// owned-root (<c>quran_lemmas.root_id</c>) semantics, ordered type distribution,
/// normalized Arabic contains search, deterministic sort, and in-memory paging.
/// Ayah and words detail are implemented in the corresponding Feature 016 story
/// phases; the remaining detail methods stay stubbed for later story phases.
/// </summary>
public sealed class EfLemmasReader(QuranDashboardDbContext db) : ILemmasReader
{
    private readonly QuranDashboardDbContext _db = db;

    public async Task<PagedResult<LemmaListItemDto>> GetLemmasPageAsync(
        string? search,
        LemmaSort sort,
        LemmasCountFilter filter,
        LemmasAssociationFilter association,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        return LemmasListDerivation.ToPage(all, filter, association, search, sort, page, pageSize);
    }

    public async Task<LemmaSummaryDto?> GetLemmaSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        return LemmasListDerivation.ToSummary(all, id);
    }

    public Task<PagedResult<LemmaWordItemDto>?> GetLemmaWordsAsync(
        int id,
        LemmaWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => GetLemmaWordsPageAsync(id, wordKind, page, pageSize, cancellationToken);

    public async Task<PagedResult<LemmaAyahMatchDto>?> GetLemmaAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        string? typeCode,
        CancellationToken cancellationToken)
    {
        var normalizedTypeCode = NormalizeTypeCode(typeCode);

        var lemmaExists = await _db.QuranLemmas
            .AsNoTracking()
            .AnyAsync(l => l.Id == id, cancellationToken);
        if (!lemmaExists)
        {
            return null;
        }

        var matchedAyahIds = _db.WordMorphologySegments
            .AsNoTracking()
            .Where(s => s.LemmaId == id && (normalizedTypeCode == null || s.Pos == normalizedTypeCode))
            .Join(
                _db.QuranWords.AsNoTracking(),
                s => s.QuranWordId,
                w => w.Id,
                (_, w) => w.AyahId)
            .Distinct();

        var totalCount = await matchedAyahIds.CountAsync(cancellationToken);
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<LemmaAyahMatchDto>(page, pageSize, totalCount, []);
        }

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
            .Skip(skip.Value)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (pageAyahs.Count == 0)
        {
            return new PagedResult<LemmaAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var ayahIds = pageAyahs.Select(a => a.AyahId).ToList();

        var matchedRows = await (
            from s in _db.WordMorphologySegments.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
            where s.LemmaId == id
                && ayahIds.Contains(w.AyahId)
                && (normalizedTypeCode == null || s.Pos == normalizedTypeCode)
            orderby w.SurahNumber, w.AyahNumber, w.WordNumber, w.Id
            select new { w.AyahId, w.Id })
            .Distinct()
            .ToListAsync(cancellationToken);

        var matchedIdsByAyah = matchedRows
            .GroupBy(r => r.AyahId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToHashSet());

        var wordsByAyah = await _db.QuranWords
            .AsNoTracking()
            .Where(w => ayahIds.Contains(w.AyahId) && !w.IsAyahMarker)
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
                var matchedSet = matchedIdsByAyah.GetValueOrDefault(ayah.AyahId, []);
                return new LemmaAyahMatchDto(
                    ayah.AyahId,
                    ayah.VerseKey,
                    ayah.SurahNameArabic,
                    ResolveAyahPageNumber(words),
                    words.Select(w => new LemmaAyahWordDto(
                        w.TextUthmani,
                        matchedSet.Contains(w.QuranWordId))).ToList());
            })
            .ToList();

        return new PagedResult<LemmaAyahMatchDto>(page, pageSize, totalCount, items);
    }

    public async Task<LemmaSurahsResponse?> GetLemmaMentionedSurahsAsync(int id, CancellationToken cancellationToken)
    {
        var lemma = await _db.QuranLemmas
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new { l.Id, l.LemmaText })
            .FirstOrDefaultAsync(cancellationToken);
        if (lemma is null)
        {
            return null;
        }

        var surahGroups = await (
            from s in _db.WordMorphologySegments.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
            where s.LemmaId == id
            group w by w.SurahNumber into g
            orderby g.Key
            select new SurahOccurrenceRow(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        if (surahGroups.Count == 0)
        {
            return new LemmaSurahsResponse([]);
        }

        var surahNumbers = surahGroups.Select(r => r.SurahNumber).ToList();
        var surahNames = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => surahNumbers.Contains(s.SurahNumber))
            .ToDictionaryAsync(s => s.SurahNumber, s => s.NameArabic, cancellationToken);

        var surahs = surahGroups
            .Select(r => new LemmaSurahItemDto(r.SurahNumber, surahNames[r.SurahNumber], r.OccurrencesInSurah))
            .ToList();

        return new LemmaSurahsResponse(surahs);
    }

    public async Task<LemmaMissingSurahsResponse?> GetLemmaMissingSurahsAsync(int id, CancellationToken cancellationToken)
    {
        var lemma = await _db.QuranLemmas
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new { l.Id, l.LemmaText })
            .FirstOrDefaultAsync(cancellationToken);
        if (lemma is null)
        {
            return null;
        }

        var mentionedSurahNumbers = await (
            from s in _db.WordMorphologySegments.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
            where s.LemmaId == id
            select w.SurahNumber)
            .Distinct()
            .ToListAsync(cancellationToken);

        var missingSurahs = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => !mentionedSurahNumbers.Contains(s.SurahNumber))
            .OrderBy(s => s.SurahNumber)
            .Select(s => new MissingSurahItemDto(s.SurahNumber, s.NameArabic))
            .ToListAsync(cancellationToken);

        return new LemmaMissingSurahsResponse(missingSurahs);
    }

    public async Task<LemmaStemsResponse?> GetLemmaStemsAsync(int id, CancellationToken cancellationToken)
    {
        var lemma = await _db.QuranLemmas
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new { l.Id, l.LemmaText })
            .FirstOrDefaultAsync(cancellationToken);
        if (lemma is null)
        {
            return null;
        }

        // Segment rows do not carry stem_id; related stems come from the word-level
        // morphology row for each Quran word that has at least one matching segment.
        var rows = await (
            from seg in _db.WordMorphologySegments.AsNoTracking()
            join m in _db.WordMorphologies.AsNoTracking() on seg.QuranWordId equals m.QuranWordId
            join stem in _db.QuranStems.AsNoTracking() on m.StemId equals stem.Id
            where seg.LemmaId == id && m.StemId != null
            select new { StemId = m.StemId!.Value, stem.StemText, seg.QuranWordId })
            .Distinct()
            .ToListAsync(cancellationToken);

        var stems = MorphologyRelatedItemsOrdering.OrderLemmaStems(
            rows.Select(r => (r.StemId, r.StemText, r.QuranWordId)));

        return new LemmaStemsResponse(stems);
    }

    /// <summary>
    /// Loads the complete lemma summary list in a bounded aggregation: identity,
    /// owned root, segment-matched occurrence/ayah/surah counts, matched-word
    /// simple/tashkeel/stem counts, first verse key, and the ordered per-lemma POS
    /// distribution. Type ordering (count desc, earliest Mushaf occurrence asc)
    /// is finalized in C# so the dominant type is always the first entry.
    /// </summary>
    internal async Task<IReadOnlyList<LemmaSummaryRow>> LoadWholeSummaryAsync(CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                l.id AS "{nameof(LemmaAggregationRow.Id)}",
                l.lemma_text AS "{nameof(LemmaAggregationRow.LemmaText)}",
                l.lemma_buckwalter AS "{nameof(LemmaAggregationRow.LemmaBuckwalter)}",
                replace(translate(lower(l.lemma_text), @foldFrom, @foldTo), ' ', '') AS "{nameof(LemmaAggregationRow.NormalizedLemmaText)}",
                l.root_id AS "{nameof(LemmaAggregationRow.RootId)}",
                r.root_text AS "{nameof(LemmaAggregationRow.RootText)}",
                r.root_buckwalter AS "{nameof(LemmaAggregationRow.RootBuckwalter)}",
                COALESCE(seg_agg.occurrences_count, 0) AS "{nameof(LemmaAggregationRow.OccurrencesCount)}",
                COALESCE(seg_agg.ayahs_count, 0) AS "{nameof(LemmaAggregationRow.AyahsCount)}",
                COALESCE(seg_agg.surahs_count, 0) AS "{nameof(LemmaAggregationRow.SurahsCount)}",
                COALESCE(matched_word_agg.simple_words_count, 0) AS "{nameof(LemmaAggregationRow.SimpleWordsCount)}",
                COALESCE(matched_word_agg.tashkeel_words_count, 0) AS "{nameof(LemmaAggregationRow.TashkeelWordsCount)}",
                COALESCE(matched_word_agg.stems_count, 0) AS "{nameof(LemmaAggregationRow.StemsCount)}",
                seg_agg.first_surah_number AS "{nameof(LemmaAggregationRow.FirstSurahNumber)}",
                seg_agg.first_ayah_number AS "{nameof(LemmaAggregationRow.FirstAyahNumber)}",
                seg_agg.first_word_number AS "{nameof(LemmaAggregationRow.FirstWordNumber)}",
                l.first_word_order_in_mushaf AS "{nameof(LemmaAggregationRow.FirstWordOrderInMushaf)}"
            FROM quran_lemmas l
            LEFT JOIN quran_roots r ON r.id = l.root_id
            LEFT JOIN (
                SELECT
                    s.lemma_id AS lid,
                    COUNT(*) AS occurrences_count,
                    COUNT(DISTINCT w.ayah_id) AS ayahs_count,
                    COUNT(DISTINCT w.surah_number) AS surahs_count,
                    (ARRAY_AGG(w.surah_number ORDER BY s.quran_word_id))[1] AS first_surah_number,
                    (ARRAY_AGG(w.ayah_number ORDER BY s.quran_word_id))[1] AS first_ayah_number,
                    (ARRAY_AGG(w.word_number ORDER BY s.quran_word_id))[1] AS first_word_number
                FROM quran_word_morphology_segments s
                JOIN quran_words w ON w.id = s.quran_word_id
                WHERE s.lemma_id IS NOT NULL
                GROUP BY s.lemma_id
            ) seg_agg ON seg_agg.lid = l.id
            LEFT JOIN (
                SELECT
                    matched.lid,
                    COUNT(DISTINCT CASE WHEN NOT w.is_ayah_marker THEN w.unique_simple_word_id END) AS simple_words_count,
                    COUNT(DISTINCT CASE WHEN NOT w.is_ayah_marker THEN w.unique_tashkeel_word_id END) AS tashkeel_words_count,
                    COUNT(DISTINCT m.stem_id) AS stems_count
                FROM (
                    SELECT DISTINCT s.lemma_id AS lid, s.quran_word_id
                    FROM quran_word_morphology_segments s
                    WHERE s.lemma_id IS NOT NULL
                ) matched
                JOIN quran_words w ON w.id = matched.quran_word_id
                LEFT JOIN quran_word_morphology m ON m.quran_word_id = matched.quran_word_id
                GROUP BY matched.lid
            ) matched_word_agg ON matched_word_agg.lid = l.id
            """;

        var aggregates = await _db.Database.SqlQueryRaw<LemmaAggregationRow>(
            sql,
            new NpgsqlParameter("foldFrom", LemmasListDerivation.ArabicFoldFrom),
            new NpgsqlParameter("foldTo", LemmasListDerivation.ArabicFoldTo))
            .ToListAsync(cancellationToken);

        if (aggregates.Count == 0)
        {
            return [];
        }

        // Load raw occurrence rows and aggregate in C# so the per-type "first
        // occurrence" is the coordinate tuple of the earliest matching word
        // (ordered by quran_word_id, the monotonic mushaf key), not three
        // independent minimums that could form a non-existent coordinate.
        var rawRows = await (
            from s in _db.WordMorphologySegments.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
            join t in _db.PosTags.AsNoTracking() on s.Pos equals t.Code
            where s.LemmaId != null
            select new
            {
                LemmaId = s.LemmaId!.Value,
                s.QuranWordId,
                t.Code,
                t.ArabicLabel,
                t.EnglishLabel,
                w.SurahNumber,
                w.AyahNumber,
                w.WordNumber,
            })
            .ToListAsync(cancellationToken);

        var occurrenceRows = rawRows
            .Select(r => new LemmaTypeOccurrenceRow(
                r.LemmaId,
                r.QuranWordId,
                r.Code,
                r.ArabicLabel,
                r.EnglishLabel,
                r.SurahNumber,
                r.AyahNumber,
                r.WordNumber))
            .ToList();

        var typesByLemma = occurrenceRows
            .GroupBy(r => r.LemmaId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<LemmaTypeDistributionRow>)MaterializeTypeDistribution(g));

        return aggregates
            .Select(a => new LemmaSummaryRow(
                a.Id,
                a.LemmaText,
                a.LemmaBuckwalter,
                LemmasListDerivation.NormalizeArabicQuery(a.LemmaText) ?? string.Empty,
                a.RootId,
                a.RootText,
                a.RootBuckwalter,
                a.OccurrencesCount,
                a.AyahsCount,
                a.SurahsCount,
                a.SimpleWordsCount,
                a.TashkeelWordsCount,
                a.StemsCount,
                BuildFirstVerseKey(a.FirstSurahNumber, a.FirstAyahNumber),
                a.FirstWordOrderInMushaf,
                typesByLemma.GetValueOrDefault(a.Id, [])))
            .ToList();
    }

    private static IReadOnlyList<LemmaTypeDistributionRow> MaterializeTypeDistribution(
        IEnumerable<LemmaTypeOccurrenceRow> rows)
    {
        // Group by POS code; for each group, the dominant first-occurrence
        // coordinate is the one of the earliest matching word by quran_word_id.
        return rows
            .GroupBy(r => r.Code)
            .Select(g =>
            {
                var first = g.OrderBy(x => x.QuranWordId).First();
                return new LemmaTypeDistributionRow(
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

    private async Task<PagedResult<LemmaWordItemDto>?> GetLemmaWordsPageAsync(
        int id,
        LemmaWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var lemmaExists = await _db.QuranLemmas
            .AsNoTracking()
            .AnyAsync(l => l.Id == id, cancellationToken);
        if (!lemmaExists)
        {
            return null;
        }

        var rows = wordKind == LemmaWordKind.Simple
            ? await LoadLemmaWordRowsAsync(id, useSimpleWordIds: true, cancellationToken)
            : await LoadLemmaWordRowsAsync(id, useSimpleWordIds: false, cancellationToken);

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

                return new LemmaWordGroupRow(
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

        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, grouped.Count);
        if (skip is null)
        {
            return new PagedResult<LemmaWordItemDto>(page, pageSize, grouped.Count, []);
        }

        var items = grouped
            .Skip(skip.Value)
            .Take(pageSize)
            .Select(row => new LemmaWordItemDto(
                row.UniqueWordId,
                row.DisplayTextUthmani,
                row.OccurrencesCount))
            .ToList();

        return new PagedResult<LemmaWordItemDto>(page, pageSize, grouped.Count, items);
    }

    private async Task<IReadOnlyList<LemmaWordOccurrenceRow>> LoadLemmaWordRowsAsync(
        int id,
        bool useSimpleWordIds,
        CancellationToken cancellationToken)
    {
        return useSimpleWordIds
            ? await (
                from s in _db.WordMorphologySegments.AsNoTracking()
                join w in _db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
                where s.LemmaId == id && !w.IsAyahMarker
                select new LemmaWordOccurrenceRow(
                    w.UniqueSimpleWordId,
                    w.TextUthmaniSimple,
                    w.SurahNumber,
                    w.AyahNumber,
                    w.WordNumber,
                    w.Id))
                .ToListAsync(cancellationToken)
            : await (
                from s in _db.WordMorphologySegments.AsNoTracking()
                join w in _db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
                where s.LemmaId == id && !w.IsAyahMarker
                select new LemmaWordOccurrenceRow(
                    w.UniqueTashkeelWordId,
                    w.TextUthmani,
                    w.SurahNumber,
                    w.AyahNumber,
                    w.WordNumber,
                    w.Id))
                .ToListAsync(cancellationToken);
    }

    private sealed record LemmaAggregationRow(
        int Id,
        string LemmaText,
        string? LemmaBuckwalter,
        string NormalizedLemmaText,
        int? RootId,
        string? RootText,
        string? RootBuckwalter,
        int OccurrencesCount,
        int AyahsCount,
        int SurahsCount,
        int SimpleWordsCount,
        int TashkeelWordsCount,
        int StemsCount,
        int? FirstSurahNumber,
        int? FirstAyahNumber,
        int? FirstWordNumber,
        int FirstWordOrderInMushaf);

    private sealed record LemmaTypeOccurrenceRow(
        int LemmaId,
        int QuranWordId,
        string Code,
        string ArabicLabel,
        string EnglishLabel,
        int SurahNumber,
        int AyahNumber,
        int WordNumber);

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

    private sealed record LemmaWordOccurrenceRow(
        int? UniqueWordId,
        string DisplayTextUthmani,
        int SurahNumber,
        int AyahNumber,
        int WordNumber,
        int QuranWordId);

    private sealed record LemmaWordGroupRow(
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

    private static string? NormalizeTypeCode(string? typeCode) =>
        string.IsNullOrWhiteSpace(typeCode) ? null : typeCode.Trim();
}
