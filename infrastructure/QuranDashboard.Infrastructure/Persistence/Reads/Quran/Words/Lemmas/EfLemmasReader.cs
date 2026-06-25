using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;

/// <summary>
/// EF Core read model for the Lemmas Explorer (Feature 016). All queries are
/// read-only and <c>AsNoTracking</c>. The lemma catalogue (list/summary) is
/// implemented in T032/T033 as a single bounded whole-summary aggregation with
/// owned-root (<c>quran_lemmas.root_id</c>) semantics, ordered type distribution,
/// normalized Arabic contains search, deterministic sort, and in-memory paging.
/// Ayah detail is implemented in the Feature 016 ayah phase; the remaining detail
/// methods stay stubbed for later story phases.
/// </summary>
public sealed class EfLemmasReader(QuranDashboardDbContext db) : ILemmasReader
{
    private readonly QuranDashboardDbContext _db = db;

    public async Task<PagedResult<LemmaListItemDto>> GetLemmasPageAsync(
        string? search,
        LemmaSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        return LemmasListDerivation.ToPage(all, search, sort, page, pageSize);
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
        => throw new NotImplementedException();

    public async Task<PagedResult<LemmaAyahMatchDto>?> GetLemmaAyahMatchesAsync(
        int id,
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

        var matchedAyahIds = _db.WordMorphologies
            .AsNoTracking()
            .Where(m => m.LemmaId == id)
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
            return new PagedResult<LemmaAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var ayahIds = pageAyahs.Select(a => a.AyahId).ToList();

        var matchedRows = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            where m.LemmaId == id && ayahIds.Contains(w.AyahId)
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
                return new LemmaAyahMatchDto(
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

        return new PagedResult<LemmaAyahMatchDto>(page, pageSize, totalCount, items);
    }

    public Task<LemmaSurahsResponse?> GetLemmaMentionedSurahsAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<LemmaMissingSurahsResponse?> GetLemmaMissingSurahsAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<LemmaStemsResponse?> GetLemmaStemsAsync(int id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    /// <summary>
    /// Loads the complete lemma summary list in a bounded aggregation: identity,
    /// owned root, derived counts, first verse key, and the ordered per-lemma POS
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
                COALESCE(agg.occurrences_count, 0) AS "{nameof(LemmaAggregationRow.OccurrencesCount)}",
                COALESCE(agg.ayahs_count, 0) AS "{nameof(LemmaAggregationRow.AyahsCount)}",
                COALESCE(agg.surahs_count, 0) AS "{nameof(LemmaAggregationRow.SurahsCount)}",
                COALESCE(agg.simple_words_count, 0) AS "{nameof(LemmaAggregationRow.SimpleWordsCount)}",
                COALESCE(agg.tashkeel_words_count, 0) AS "{nameof(LemmaAggregationRow.TashkeelWordsCount)}",
                COALESCE(agg.stems_count, 0) AS "{nameof(LemmaAggregationRow.StemsCount)}",
                agg.first_surah_number AS "{nameof(LemmaAggregationRow.FirstSurahNumber)}",
                agg.first_ayah_number AS "{nameof(LemmaAggregationRow.FirstAyahNumber)}",
                agg.first_word_number AS "{nameof(LemmaAggregationRow.FirstWordNumber)}",
                l.first_word_order_in_mushaf AS "{nameof(LemmaAggregationRow.FirstWordOrderInMushaf)}"
            FROM quran_lemmas l
            LEFT JOIN quran_roots r ON r.id = l.root_id
            LEFT JOIN (
                SELECT
                    m.lemma_id AS lid,
                    COUNT(*) AS occurrences_count,
                    COUNT(DISTINCT w.ayah_id) AS ayahs_count,
                    COUNT(DISTINCT w.surah_number) AS surahs_count,
                    COUNT(DISTINCT w.unique_simple_word_id) AS simple_words_count,
                    COUNT(DISTINCT w.unique_tashkeel_word_id) AS tashkeel_words_count,
                    COUNT(DISTINCT m.stem_id) AS stems_count,
                    (ARRAY_AGG(w.surah_number ORDER BY m.quran_word_id))[1] AS first_surah_number,
                    (ARRAY_AGG(w.ayah_number ORDER BY m.quran_word_id))[1] AS first_ayah_number,
                    (ARRAY_AGG(w.word_number ORDER BY m.quran_word_id))[1] AS first_word_number
                FROM quran_word_morphology m
                JOIN quran_words w ON w.id = m.quran_word_id
                WHERE m.lemma_id IS NOT NULL
                GROUP BY m.lemma_id
            ) agg ON agg.lid = l.id
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

        var lemmaIds = aggregates.Select(a => a.Id).ToList();

        // Load raw occurrence rows and aggregate in C# so the per-type "first
        // occurrence" is the coordinate tuple of the earliest matching word
        // (ordered by quran_word_id, the monotonic mushaf key), not three
        // independent minimums that could form a non-existent coordinate.
        var rawRows = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            join t in _db.PosTags.AsNoTracking() on m.HeadPos equals t.Code
            where m.LemmaId != null
            select new
            {
                LemmaId = m.LemmaId!.Value,
                m.QuranWordId,
                t.Code,
                t.ArabicLabel,
                t.EnglishLabel,
                w.SurahNumber,
                w.AyahNumber,
                w.WordNumber,
            })
            .ToListAsync(cancellationToken);

        var occurrenceRows = rawRows
            .Where(r => lemmaIds.Contains(r.LemmaId))
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
