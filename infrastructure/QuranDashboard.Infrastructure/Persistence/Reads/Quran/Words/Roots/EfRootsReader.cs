using Microsoft.EntityFrameworkCore;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

public sealed class EfRootsReader(QuranDashboardDbContext db) : IRootsReader
{
    private readonly QuranDashboardDbContext _db = db;

    public async Task<PagedResult<RootListItemDto>> GetRootsPageAsync(
        string? search,
        RootSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        return RootsListDerivation.ToPage(all, search, sort, page, pageSize);
    }

    public async Task<RootSummaryDto?> GetRootSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        return RootsListDerivation.ToSummary(all, id);
    }

    public async Task<PagedResult<RootWordItemDto>?> GetRootWordsAsync(
        int id,
        RootWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var grouped = await LoadGroupedRootWordsAsync(id, wordKind, cancellationToken);
        return grouped is null
            ? null
            : RootsWordsDerivation.ToPage(grouped, page, pageSize);
    }

    internal async Task<IReadOnlyList<RootWordItemDto>?> LoadGroupedRootWordsAsync(
        int id,
        RootWordKind wordKind,
        CancellationToken cancellationToken)
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
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            where m.RootId == id
            select new RootWordOccurrenceRow(
                wordKind == RootWordKind.Simple ? w.UniqueSimpleWordId : w.UniqueTashkeelWordId,
                w.SurahNumber,
                w.AyahNumber,
                w.WordNumber,
                w.TextUthmani))
            .ToListAsync(cancellationToken);

        var kindKey = wordKind == RootWordKind.Simple
            ? RootWordKindKeys.Simple
            : RootWordKindKeys.Tashkeel;

        return rows
            .Where(r => r.UniqueWordId.HasValue)
            .GroupBy(r => r.UniqueWordId!.Value)
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
                    first.TextUthmani);
            })
            .OrderBy(x => x.SurahNumber)
            .ThenBy(x => x.AyahNumber)
            .ThenBy(x => x.WordNumber)
            .Select(x => new RootWordItemDto(
                x.UniqueWordId,
                kindKey,
                x.TextUthmani,
                x.OccurrencesCount,
                $"{x.SurahNumber}:{x.AyahNumber}"))
            .ToList();
    }

    public async Task<PagedResult<RootAyahMatchDto>?> GetRootAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rootExists = await _db.QuranRoots
            .AsNoTracking()
            .AnyAsync(r => r.Id == id, cancellationToken);
        if (!rootExists)
        {
            return null;
        }

        var matchedAyahIds = _db.WordMorphologies
            .AsNoTracking()
            .Where(m => m.RootId == id)
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
            return new PagedResult<RootAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var ayahIds = pageAyahs.Select(a => a.AyahId).ToList();

        var matchedRows = await (
            from m in _db.WordMorphologies.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
            where m.RootId == id && ayahIds.Contains(w.AyahId)
            select new { w.AyahId, w.Id })
            .ToListAsync(cancellationToken);

        var matchedIdsByAyah = matchedRows
            .GroupBy(r => r.AyahId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(x => x.Id).ToList());

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
                return new RootAyahMatchDto(
                    ayah.AyahId,
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.SurahNameArabic,
                    ayah.AyahNumber,
                    ResolveAyahPageNumber(words),
                    matchedIdsByAyah.GetValueOrDefault(ayah.AyahId, []),
                    words.Select(w => new AyahWordForHighlightDto(
                        w.QuranWordId,
                        w.TextUthmani,
                        w.IsAyahMarker)).ToList());
            })
            .ToList();

        return new PagedResult<RootAyahMatchDto>(page, pageSize, totalCount, items);
    }

    public async Task<RootSurahsResponse?> GetRootMentionedSurahsAsync(int id, CancellationToken cancellationToken)
    {
        var root = await _db.QuranRoots
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.Id, r.RootText })
            .FirstOrDefaultAsync(cancellationToken);
        if (root is null)
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
            return new RootSurahsResponse(id, root.RootText, 0, []);
        }

        var surahNumbers = surahGroups.Select(r => r.SurahNumber).ToList();
        var surahNames = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => surahNumbers.Contains(s.SurahNumber))
            .ToDictionaryAsync(s => s.SurahNumber, s => s.NameArabic, cancellationToken);

        var surahs = surahGroups
            .Select(r => new RootSurahItemDto(r.SurahNumber, surahNames[r.SurahNumber], r.OccurrencesInSurah))
            .ToList();

        return new RootSurahsResponse(id, root.RootText, surahs.Count, surahs);
    }

    public async Task<RootMissingSurahsResponse?> GetRootMissingSurahsAsync(int id, CancellationToken cancellationToken)
    {
        var root = await _db.QuranRoots
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.Id, r.RootText })
            .FirstOrDefaultAsync(cancellationToken);
        if (root is null)
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

        return new RootMissingSurahsResponse(id, root.RootText, missingSurahs.Count, missingSurahs);
    }

    public async Task<RootLemmasResponse?> GetRootLemmasAsync(int id, CancellationToken cancellationToken)
    {
        var root = await _db.QuranRoots
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.Id, r.RootText })
            .FirstOrDefaultAsync(cancellationToken);
        if (root is null)
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

        return new RootLemmasResponse(id, root.RootText, lemmas.Count, lemmas);
    }

    public async Task<RootStemsResponse?> GetRootStemsAsync(int id, CancellationToken cancellationToken)
    {
        var root = await _db.QuranRoots
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new { r.Id, r.RootText })
            .FirstOrDefaultAsync(cancellationToken);
        if (root is null)
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

        return new RootStemsResponse(id, root.RootText, stems.Count, stems);
    }

    internal async Task<IReadOnlyList<RootSummaryRow>> LoadWholeSummaryAsync(CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                r.id AS "{nameof(RootSummaryRow.Id)}",
                r.root_text AS "{nameof(RootSummaryRow.RootText)}",
                replace(translate(lower(r.root_text), @foldFrom, @foldTo), ' ', '') AS "{nameof(RootSummaryRow.NormalizedRootText)}",
                r.words_count AS "{nameof(RootSummaryRow.OccurrencesCount)}",
                agg.ayahs_count AS "{nameof(RootSummaryRow.AyahsCount)}",
                agg.surahs_count AS "{nameof(RootSummaryRow.SurahsCount)}",
                agg.simple_words_count AS "{nameof(RootSummaryRow.SimpleWordsCount)}",
                agg.tashkeel_words_count AS "{nameof(RootSummaryRow.TashkeelWordsCount)}",
                COALESCE(agg.distinct_lemmas_count, r.distinct_lemmas_count) AS "{nameof(RootSummaryRow.LemmasCount)}",
                agg.stems_count AS "{nameof(RootSummaryRow.StemsCount)}",
                concat_ws(':', a_first.surah_number::text, a_first.ayah_number::text) AS "{nameof(RootSummaryRow.FirstVerseKey)}",
                r.first_word_order_in_mushaf AS "{nameof(RootSummaryRow.FirstWordOrderInMushaf)}"
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
            LEFT JOIN (
                SELECT DISTINCT ON (root_id)
                    root_id,
                    quran_word_id
                FROM quran_word_morphology
                WHERE root_id IS NOT NULL
                ORDER BY root_id, quran_word_id
            ) first_m ON first_m.root_id = r.id
            LEFT JOIN quran_words w_first ON w_first.id = first_m.quran_word_id
            LEFT JOIN quran_ayahs a_first ON a_first.id = w_first.ayah_id
            """;

        var rows = await _db.Database.SqlQueryRaw<RootSummaryRow>(
            sql,
            new NpgsqlParameter("foldFrom", RootsListDerivation.ArabicFoldFrom),
            new NpgsqlParameter("foldTo", RootsListDerivation.ArabicFoldTo))
            .ToListAsync(cancellationToken);

        return rows;
    }

    private static short ResolveAyahPageNumber(IReadOnlyList<AyahWordRow> words)
    {
        var firstReadable = words
            .Where(w => !w.IsAyahMarker)
            .OrderBy(w => w.WordNumber)
            .FirstOrDefault();

        if (firstReadable is not null)
        {
            return firstReadable.PageNumber;
        }

        return words.FirstOrDefault()?.PageNumber ?? 0;
    }

    private sealed record AyahMetaRow(
        int AyahId,
        string VerseKey,
        short SurahNumber,
        short AyahNumber,
        string SurahNameArabic);

    private sealed record AyahWordRow(
        int AyahId,
        int QuranWordId,
        short WordNumber,
        short PageNumber,
        string TextUthmani,
        bool IsAyahMarker);

    private sealed record GroupedRootWordRow(
        int UniqueWordId,
        int OccurrencesCount,
        short SurahNumber,
        short AyahNumber,
        short WordNumber,
        string TextUthmani);

    private sealed record RootWordOccurrenceRow(
        int? UniqueWordId,
        short SurahNumber,
        short AyahNumber,
        short WordNumber,
        string TextUthmani);

    private sealed record SurahOccurrenceRow(short SurahNumber, int OccurrencesInSurah);
}
