using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

public sealed partial class EfStemsReader(QuranDashboardDbContext db) : IStemsReader
{
    private readonly QuranDashboardDbContext _db = db;

    public async Task<PagedResult<StemListItemDto>> GetStemsPageAsync(
        string? search,
        StemSortSpec sort,
        StemsCountFilter filter,
        StemsAssociationFilter association,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        return StemsListDerivation.ToPage(all, filter, association, search, sort, page, pageSize);
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
        string? typeCode,
        CancellationToken cancellationToken)
    {
        var normalizedTypeCode = StemsAyahTypeCode.Normalize(typeCode);

        var stemExists = await _db.QuranStems
            .AsNoTracking()
            .AnyAsync(s => s.Id == id, cancellationToken);
        if (!stemExists)
        {
            return null;
        }

        var matchedAyahIds = _db.WordMorphologySegments
            .AsNoTracking()
            .Where(s => s.Kind == "STEM"
                && s.StemId == id
                && (normalizedTypeCode == null || s.Pos == normalizedTypeCode))
            .Join(
                _db.QuranWords.AsNoTracking(),
                s => s.QuranWordId,
                w => w.Id,
                (_, w) => new { w.AyahId, w.IsAyahMarker })
            .Where(x => !x.IsAyahMarker)
            .Select(x => x.AyahId)
            .Distinct();

        var totalCount = await matchedAyahIds.CountAsync(cancellationToken);
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<StemAyahMatchDto>(page, pageSize, totalCount, []);
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
            return new PagedResult<StemAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var ayahIds = pageAyahs.Select(a => a.AyahId).ToList();

        var matchedRows = await (
            from s in _db.WordMorphologySegments.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
            where s.Kind == "STEM"
                && s.StemId == id
                && ayahIds.Contains(w.AyahId)
                && (normalizedTypeCode == null || s.Pos == normalizedTypeCode)
                && !w.IsAyahMarker
            orderby w.SurahNumber, w.AyahNumber, w.WordNumber, w.Id
            select new { w.AyahId, w.Id })
            .ToListAsync(cancellationToken);

        var matchedIdsByAyah = matchedRows
            .GroupBy(r => r.AyahId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToHashSet());

        var items = await AyahWordHydration.ProjectAyahMatchesAsync(
            _db,
            pageAyahs,
            ayah => ayah.AyahId,
            (ayah, words, pageNumber) =>
            {
                var matchedSet = matchedIdsByAyah.GetValueOrDefault(ayah.AyahId) ?? [];
                return new StemAyahMatchDto(
                    ayah.AyahId,
                    ayah.VerseKey,
                    ayah.SurahNameArabic,
                    pageNumber,
                    words.Select(w => new StemAyahWordDto(
                        w.TextUthmani,
                        matchedSet.Contains(w.QuranWordId))).ToList());
            },
            cancellationToken);

        return new PagedResult<StemAyahMatchDto>(page, pageSize, totalCount, items);
    }

    public async Task<StemSurahsResponse?> GetStemMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var stemExists = await _db.QuranStems
            .AsNoTracking()
            .AnyAsync(s => s.Id == id, cancellationToken);
        if (!stemExists)
        {
            return null;
        }

        var surahGroups = await (
            from s in _db.WordMorphologySegments.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
            where s.Kind == "STEM" && s.StemId == id && !w.IsAyahMarker
            group w by w.SurahNumber into g
            orderby g.Key
            select new SurahOccurrenceRow(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        if (surahGroups.Count == 0)
        {
            return new StemSurahsResponse([]);
        }

        var surahNumbers = surahGroups.Select(r => r.SurahNumber).ToList();
        var surahNames = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => surahNumbers.Contains(s.SurahNumber))
            .ToDictionaryAsync(s => s.SurahNumber, s => s.NameArabic, cancellationToken);

        var surahs = surahGroups
            .Select(r => new StemSurahItemDto(r.SurahNumber, surahNames[r.SurahNumber], r.OccurrencesInSurah))
            .ToList();

        return new StemSurahsResponse(surahs);
    }

    public async Task<StemMissingSurahsResponse?> GetStemMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var stemExists = await _db.QuranStems
            .AsNoTracking()
            .AnyAsync(s => s.Id == id, cancellationToken);
        if (!stemExists)
        {
            return null;
        }

        var mentionedSurahNumbers = await (
            from s in _db.WordMorphologySegments.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
            where s.Kind == "STEM" && s.StemId == id && !w.IsAyahMarker
            select w.SurahNumber)
            .Distinct()
            .ToListAsync(cancellationToken);

        var missingSurahs = await _db.QuranSurahs
            .AsNoTracking()
            .Where(s => !mentionedSurahNumbers.Contains(s.SurahNumber))
            .OrderBy(s => s.SurahNumber)
            .Select(s => new MissingSurahItemDto(s.SurahNumber, s.NameArabic))
            .ToListAsync(cancellationToken);

        return new StemMissingSurahsResponse(missingSurahs);
    }

    public async Task<StemLemmasResponse?> GetStemLemmasAsync(int id, CancellationToken cancellationToken)
    {
        var stemExists = await _db.QuranStems
            .AsNoTracking()
            .AnyAsync(s => s.Id == id, cancellationToken);
        if (!stemExists)
        {
            return null;
        }

        var rows = await (
            from s in _db.WordMorphologySegments.AsNoTracking()
            join w in _db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
            join l in _db.QuranLemmas.AsNoTracking() on s.LemmaId equals l.Id
            where s.Kind == "STEM" && s.StemId == id && s.LemmaId != null && !w.IsAyahMarker
            select new { s.LemmaId, l.LemmaText, s.QuranWordId })
            .ToListAsync(cancellationToken);

        var lemmas = MorphologyRelatedItemsOrdering.OrderStemLemmas(
            rows.Select(r => (r.LemmaId!.Value, r.LemmaText, r.QuranWordId)));

        return new StemLemmasResponse(lemmas);
    }

    private static StemRelationRow? PickDominantRelation(IEnumerable<StemRelationGroupSqlRow> groups) =>
        groups
            .Select(r => new StemRelationRow(
                r.RelationId,
                r.Text ?? string.Empty,
                r.Buckwalter,
                r.OccurrencesCount,
                r.FirstSurahNumber,
                r.FirstAyahNumber,
                r.FirstWordNumber))
            .OrderByDescending(r => r.OccurrencesCount)
            .ThenBy(r => r.FirstSurahNumber)
            .ThenBy(r => r.FirstAyahNumber)
            .ThenBy(r => r.FirstWordNumber)
            .ThenBy(r => r.Id)
            .FirstOrDefault();

    private static IReadOnlyList<StemTypeDistributionRow> OrderTypeDistribution(IEnumerable<StemTypeDistributionSqlRow> rows) =>
        rows
            .Select(r => new StemTypeDistributionRow(
                r.Code,
                r.ArabicLabel,
                r.EnglishLabel,
                r.OccurrencesCount,
                r.FirstSurahNumber,
                r.FirstAyahNumber,
                r.FirstWordNumber))
            .OrderByDescending(r => r.OccurrencesCount)
            .ThenBy(r => r.FirstSurahNumber)
            .ThenBy(r => r.FirstAyahNumber)
            .ThenBy(r => r.FirstWordNumber)
            .ThenBy(r => r.Code, StringComparer.Ordinal)
            .ToList();

    private static string BuildFirstVerseKey(int? firstSurahNumber, int? firstAyahNumber) =>
        firstSurahNumber is > 0 && firstAyahNumber is > 0
            ? $"{firstSurahNumber}:{firstAyahNumber}"
            : string.Empty;

    // Loads the whole grouped/ordered word list in one pass (null when the stem is absent). This is the
    // identity-grain unit CachedStemsReader caches once, so paging slices the cached list instead of
    // re-issuing the full occurrence query (perf finding B6).
    internal async Task<IReadOnlyList<StemWordItemDto>?> LoadStemWordGroupsAsync(
        int id,
        StemWordKind wordKind,
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

        return rows
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
                    first.DisplayText,
                    g.Count(),
                    first.SurahNumber,
                    first.AyahNumber,
                    first.WordNumber);
            })
            .OrderBy(x => x.FirstSurahNumber)
            .ThenBy(x => x.FirstAyahNumber)
            .ThenBy(x => x.FirstWordNumber)
            .ThenBy(x => x.UniqueWordId)
            .Select(row => new StemWordItemDto(
                row.UniqueWordId,
                row.DisplayText,
                row.OccurrencesCount))
            .ToList();
    }

    internal static PagedResult<StemWordItemDto> SliceStemWordsPage(
        IReadOnlyList<StemWordItemDto> all,
        int page,
        int pageSize)
    {
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, all.Count);
        if (skip is null)
        {
            return new PagedResult<StemWordItemDto>(page, pageSize, all.Count, []);
        }

        var items = all.Skip(skip.Value).Take(pageSize).ToList();
        return new PagedResult<StemWordItemDto>(page, pageSize, all.Count, items);
    }

    private async Task<PagedResult<StemWordItemDto>?> GetStemWordsPageAsync(
        int id,
        StemWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var grouped = await LoadStemWordGroupsAsync(id, wordKind, cancellationToken);
        return grouped is null ? null : SliceStemWordsPage(grouped, page, pageSize);
    }

    private async Task<IReadOnlyList<StemWordOccurrenceRow>> LoadStemWordRowsAsync(
        int id,
        bool useSimpleWordIds,
        CancellationToken cancellationToken)
    {
        return useSimpleWordIds
            ? await (
                from s in _db.WordMorphologySegments.AsNoTracking()
                join w in _db.QuranWords.AsNoTracking() on s.QuranWordId equals w.Id
                where s.Kind == "STEM" && s.StemId == id && !w.IsAyahMarker
                select new StemWordOccurrenceRow(
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
                where s.Kind == "STEM" && s.StemId == id && !w.IsAyahMarker
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

    private sealed record StemWordOccurrenceRow(
        int? UniqueWordId,
        string DisplayText,
        int SurahNumber,
        int AyahNumber,
        int WordNumber,
        int QuranWordId);

    private sealed record StemWordGroupRow(
        int UniqueWordId,
        string DisplayText,
        int OccurrencesCount,
        int FirstSurahNumber,
        int FirstAyahNumber,
        int FirstWordNumber);

    private sealed record SurahOccurrenceRow(short SurahNumber, int OccurrencesInSurah);
}
