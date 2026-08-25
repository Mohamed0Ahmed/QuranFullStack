using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

public sealed class EfRootsReader(QuranDashboardDbContext db) : IRootsReader
{
    private readonly QuranDashboardDbContext _db = db;

    public async Task<PagedResult<RootListItemDto>> GetRootsPageAsync(
        string? search,
        RootSortSpec sort,
        RootsCountFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        return RootsListDerivation.ToPage(all, filter, search, sort, page, pageSize);
    }

    public async Task<RootSummaryDto?> GetRootSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        var row = all.FirstOrDefault(item => item.Id == id);
        if (row is null)
        {
            return null;
        }

        var typeDistribution = await LoadRootTypeDistributionAsync(id, cancellationToken);
        return RootsListDerivation.ToSummary([row with { TypeDistribution = typeDistribution }], id);
    }

    public async Task<PagedResult<RootWordItemDto>?> GetRootWordsAsync(
        int id,
        RootWordKind wordKind,
        string? typeCode,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var grouped = await LoadGroupedRootWordsAsync(id, wordKind, typeCode, cancellationToken);
        return grouped is null
            ? null
            : RootsWordsDerivation.ToPage(grouped, page, pageSize);
    }

    internal async Task<IReadOnlyList<RootWordItemDto>?> LoadGroupedRootWordsAsync(
        int id,
        RootWordKind wordKind,
        string? typeCode,
        CancellationToken cancellationToken)
    {
        var rootExists = await _db.QuranRoots
            .AsNoTracking()
            .AnyAsync(r => r.Id == id, cancellationToken);
        if (!rootExists)
        {
            return null;
        }

        var normalizedTypeCode = string.IsNullOrWhiteSpace(typeCode) ? null : typeCode.Trim();
        var rows = wordKind == RootWordKind.Simple
            ? await (
                from m in _db.WordMorphologies.AsNoTracking()
                join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
                join u in _db.QuranWordsUniqueSimple.AsNoTracking() on w.UniqueSimpleWordId!.Value equals u.Id
                where m.RootId == id
                    && (normalizedTypeCode == null || m.HeadPos == normalizedTypeCode)
                    && w.UniqueSimpleWordId != null
                select new RootWordOccurrenceRow(
                    w.UniqueSimpleWordId!.Value,
                    w.SurahNumber,
                    w.AyahNumber,
                    w.WordNumber,
                    u.TextImlaeiSimple))
                .ToListAsync(cancellationToken)
            : await (
                from m in _db.WordMorphologies.AsNoTracking()
                join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
                join u in _db.QuranWordsUniqueTashkeel.AsNoTracking() on w.UniqueTashkeelWordId!.Value equals u.Id
                where m.RootId == id
                    && (normalizedTypeCode == null || m.HeadPos == normalizedTypeCode)
                    && w.UniqueTashkeelWordId != null
                select new RootWordOccurrenceRow(
                    w.UniqueTashkeelWordId!.Value,
                    w.SurahNumber,
                    w.AyahNumber,
                    w.WordNumber,
                    u.TextUthmani))
                .ToListAsync(cancellationToken);

        var kindKey = wordKind == RootWordKind.Simple
            ? RootWordKindKeys.Simple
            : RootWordKindKeys.Tashkeel;

        return rows
            .GroupBy(r => r.UniqueWordId)
            .Select(g =>
            {
                var first = g
                    .OrderBy(x => x.SurahNumber)
                    .ThenBy(x => x.AyahNumber)
                    .ThenBy(x => x.WordNumber)
                    .First();
                return new GroupedRootWordRow(
                    g.Key,
                    g.Count(),
                    first.SurahNumber,
                    first.AyahNumber,
                    first.WordNumber,
                    first.DisplayText);
            })
            .OrderBy(x => x.SurahNumber)
            .ThenBy(x => x.AyahNumber)
            .ThenBy(x => x.WordNumber)
            .Select(x => new RootWordItemDto(
                x.UniqueWordId,
                kindKey,
                x.DisplayText,
                x.OccurrencesCount))
            .ToList();
    }

    public async Task<PagedResult<RootAyahMatchDto>?> GetRootAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        string? typeCode,
        CancellationToken cancellationToken)
    {
        var normalizedTypeCode = string.IsNullOrWhiteSpace(typeCode) ? null : typeCode.Trim();

        var rootExists = await _db.QuranRoots
            .AsNoTracking()
            .AnyAsync(r => r.Id == id, cancellationToken);
        if (!rootExists)
        {
            return null;
        }

        var matchedAyahIds = _db.WordMorphologies
            .AsNoTracking()
            .Where(m => m.RootId == id && (normalizedTypeCode == null || m.HeadPos == normalizedTypeCode))
            .Join(
                _db.QuranWords.AsNoTracking(),
                m => m.QuranWordId,
                w => w.Id,
                (_, w) => w.AyahId)
            .Distinct();

        var totalCount = await matchedAyahIds.CountAsync(cancellationToken);
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<RootAyahMatchDto>(page, pageSize, totalCount, []);
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
            return new PagedResult<RootAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var ayahIds = pageAyahs.Select(a => a.AyahId).ToList();

        var matchedRows = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            where m.RootId == id
                && (normalizedTypeCode == null || m.HeadPos == normalizedTypeCode)
                && ayahIds.Contains(w.AyahId)
            select new { w.AyahId, w.Id })
            .ToListAsync(cancellationToken);

        var matchedIdsByAyah = matchedRows
            .GroupBy(r => r.AyahId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(x => x.Id).ToList());

        var items = await AyahWordHydration.ProjectAyahMatchesAsync(
            _db,
            pageAyahs,
            ayah => ayah.AyahId,
            (ayah, words, pageNumber) =>
            {
                var matchedSet = matchedIdsByAyah.GetValueOrDefault(ayah.AyahId, []);
                return new RootAyahMatchDto(
                    ayah.AyahId,
                    ayah.VerseKey,
                    ayah.SurahNameArabic,
                    pageNumber,
                    words.Select(w => new RootAyahWordDto(
                        w.TextUthmani,
                        matchedSet.Contains(w.QuranWordId))).ToList());
            },
            cancellationToken);

        return new PagedResult<RootAyahMatchDto>(page, pageSize, totalCount, items);
    }

    public async Task<RootSurahsResponse?> GetRootMentionedSurahsAsync(int id, CancellationToken cancellationToken)
    {
        var rootExists = await _db.QuranRoots
            .AsNoTracking()
            .AnyAsync(r => r.Id == id, cancellationToken);
        if (!rootExists)
        {
            return null;
        }

        var surahGroups = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            where m.RootId == id
            group w by w.SurahNumber into g
            orderby g.Key
            select new SurahOccurrenceRow(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        if (surahGroups.Count == 0)
        {
            return new RootSurahsResponse([]);
        }

        var surahNumbers = surahGroups.Select(r => r.SurahNumber).ToList();
        var surahNames = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => surahNumbers.Contains(s.SurahNumber))
            .ToDictionaryAsync(s => s.SurahNumber, s => s.NameArabic, cancellationToken);

        var surahs = surahGroups
            .Select(r => new RootSurahItemDto(r.SurahNumber, surahNames[r.SurahNumber], r.OccurrencesInSurah))
            .ToList();

        return new RootSurahsResponse(surahs);
    }

    public async Task<RootMissingSurahsResponse?> GetRootMissingSurahsAsync(int id, CancellationToken cancellationToken)
    {
        var rootExists = await _db.QuranRoots
            .AsNoTracking()
            .AnyAsync(r => r.Id == id, cancellationToken);
        if (!rootExists)
        {
            return null;
        }

        var mentionedSurahNumbers = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            where m.RootId == id
            select w.SurahNumber)
            .Distinct()
            .ToListAsync(cancellationToken);

        var missingSurahs = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => !mentionedSurahNumbers.Contains(s.SurahNumber))
            .OrderBy(s => s.SurahNumber)
            .Select(s => new MissingSurahItemDto(s.SurahNumber, s.NameArabic))
            .ToListAsync(cancellationToken);

        return new RootMissingSurahsResponse(missingSurahs);
    }

    public async Task<RootLemmasResponse?> GetRootLemmasAsync(int id, CancellationToken cancellationToken)
    {
        var rootExists = await _db.QuranRoots
            .AsNoTracking()
            .AnyAsync(r => r.Id == id, cancellationToken);
        if (!rootExists)
        {
            return null;
        }

        var rows = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join l in _db.QuranLemmas.AsNoTracking() on m.LemmaId equals l.Id
            where m.RootId == id && m.LemmaId != null
            select new { m.LemmaId, l.LemmaText, m.QuranWordId })
            .ToListAsync(cancellationToken);

        var lemmas = rows
            .GroupBy(r => r.LemmaId!.Value)
            .Select(g =>
            {
                var first = g.OrderBy(x => x.QuranWordId).First();
                return new
                {
                    Item = new RootLemmaItemDto(g.Key, first.LemmaText, g.Count()),
                    first.QuranWordId,
                };
            })
            .OrderBy(x => x.QuranWordId)
            .Select(x => x.Item)
            .ToList();

        return new RootLemmasResponse(lemmas);
    }

    public async Task<RootStemsResponse?> GetRootStemsAsync(int id, CancellationToken cancellationToken)
    {
        var rootExists = await _db.QuranRoots
            .AsNoTracking()
            .AnyAsync(r => r.Id == id, cancellationToken);
        if (!rootExists)
        {
            return null;
        }

        var rows = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join s in _db.QuranStems.AsNoTracking() on m.StemId equals s.Id
            where m.RootId == id && m.StemId != null
            select new { m.StemId, s.StemText, m.QuranWordId })
            .ToListAsync(cancellationToken);

        var stems = rows
            .GroupBy(r => r.StemId!.Value)
            .Select(g =>
            {
                var first = g.OrderBy(x => x.QuranWordId).First();
                return new
                {
                    Item = new RootStemItemDto(g.Key, first.StemText, g.Count()),
                    first.QuranWordId,
                };
            })
            .OrderBy(x => x.QuranWordId)
            .Select(x => x.Item)
            .ToList();

        return new RootStemsResponse(stems);
    }

    internal async Task<IReadOnlyList<RootSummaryRow>> LoadWholeSummaryAsync(CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                r.id AS "{nameof(RootAggregationRow.Id)}",
                r.root_text AS "{nameof(RootAggregationRow.RootText)}",
                replace(translate(lower(r.root_text), @foldFrom, @foldTo), ' ', '') AS "{nameof(RootAggregationRow.NormalizedRootText)}",
                r.words_count AS "{nameof(RootAggregationRow.OccurrencesCount)}",
                COALESCE(agg.ayahs_count, 0) AS "{nameof(RootAggregationRow.AyahsCount)}",
                COALESCE(agg.surahs_count, 0) AS "{nameof(RootAggregationRow.SurahsCount)}",
                COALESCE(agg.simple_words_count, 0) AS "{nameof(RootAggregationRow.SimpleWordsCount)}",
                COALESCE(agg.tashkeel_words_count, 0) AS "{nameof(RootAggregationRow.TashkeelWordsCount)}",
                COALESCE(agg.distinct_lemmas_count, r.distinct_lemmas_count) AS "{nameof(RootAggregationRow.LemmasCount)}",
                COALESCE(agg.stems_count, 0) AS "{nameof(RootAggregationRow.StemsCount)}",
                r.first_word_order_in_mushaf AS "{nameof(RootAggregationRow.FirstWordOrderInMushaf)}"
            FROM quran_roots r
            LEFT JOIN (
                SELECT
                    m.root_id AS rid,
                    COUNT(DISTINCT w.ayah_id) AS ayahs_count,
                    COUNT(DISTINCT w.surah_number) AS surahs_count,
                    COUNT(DISTINCT w.unique_simple_word_id) AS simple_words_count,
                    COUNT(DISTINCT w.unique_tashkeel_word_id) AS tashkeel_words_count,
                    COUNT(DISTINCT m.lemma_id) AS distinct_lemmas_count,
                    COUNT(DISTINCT m.stem_id) AS stems_count
                FROM quran_word_morphology m
                JOIN quran_words w ON w.id = m.quran_word_id
                WHERE m.root_id IS NOT NULL
                GROUP BY m.root_id
            ) agg ON agg.rid = r.id
            """;

        var aggregates = await _db.Database.SqlQueryRaw<RootAggregationRow>(
            sql,
            new NpgsqlParameter("foldFrom", ArabicSearchQueryNormalizer.FoldFrom),
            new NpgsqlParameter("foldTo", ArabicSearchQueryNormalizer.FoldTo))
            .ToListAsync(cancellationToken);

        if (aggregates.Count == 0)
        {
            return [];
        }

        return aggregates
            .Select(row => new RootSummaryRow(
                row.Id,
                row.RootText,
                row.NormalizedRootText,
                row.OccurrencesCount,
                row.AyahsCount,
                row.SurahsCount,
                row.SimpleWordsCount,
                row.TashkeelWordsCount,
                row.LemmasCount,
                row.StemsCount,
                row.FirstWordOrderInMushaf,
                []))
            .ToList();
    }

    internal async Task<IReadOnlyList<RootTypeDistributionRow>> LoadRootTypeDistributionAsync(
        int rootId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from morphology in _db.WordMorphologies.AsNoTracking()
            join tag in _db.PosTags.AsNoTracking() on morphology.HeadPos equals tag.Code
            where morphology.RootId == rootId
            group morphology by new
            {
                RootId = morphology.RootId!.Value,
                tag.Code,
                tag.ArabicLabel,
            }
            into groupRows
            select new RootTypeDistributionRow(
                groupRows.Key.RootId,
                groupRows.Key.Code,
                groupRows.Key.ArabicLabel,
                groupRows.Count(),
                groupRows.Min(row => row.QuranWordId)))
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(row => row.OccurrencesCount)
            .ThenBy(row => row.FirstQuranWordId)
            .ThenBy(row => row.Code, StringComparer.Ordinal)
            .ToList();
    }

    private sealed record AyahMetaRow(
        int AyahId,
        string VerseKey,
        short SurahNumber,
        short AyahNumber,
        string SurahNameArabic);

    private sealed record GroupedRootWordRow(
        int UniqueWordId,
        int OccurrencesCount,
        short SurahNumber,
        short AyahNumber,
        short WordNumber,
        string DisplayText);

    private sealed record RootWordOccurrenceRow(
        int UniqueWordId,
        short SurahNumber,
        short AyahNumber,
        short WordNumber,
        string DisplayText);

    private sealed record SurahOccurrenceRow(short SurahNumber, int OccurrencesInSurah);
}
