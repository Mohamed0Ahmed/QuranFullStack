using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

internal static class StemsListDerivation
{
    internal static readonly TypeSummaryDto NoType = new(string.Empty, "غير محدَّد", 0);

    public static PagedResult<StemListItemDto> ToPage(
        IReadOnlyList<StemSummaryRow> all,
        StemsCountFilter filter,
        StemsAssociationFilter association,
        string? search,
        StemSortSpec sort,
        int page,
        int pageSize)
    {
        var rows = FilterAndSort(all, filter, association, search, sort);
        var materialized = rows.ToList();
        var totalCount = materialized.Count;
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<StemListItemDto>(page, pageSize, totalCount, []);
        }

        var pageRows = materialized
            .Skip(skip.Value)
            .Take(pageSize)
            .ToList();

        var items = pageRows.Select(ToListItem).ToList();
        return new PagedResult<StemListItemDto>(page, pageSize, totalCount, items);
    }

    public static StemSummaryDto? ToSummary(IReadOnlyList<StemSummaryRow> all, int id)
    {
        var row = all.FirstOrDefault(r => r.Id == id);
        return row is null ? null : ToSummaryDto(row);
    }

    public static IEnumerable<StemSummaryRow> FilterAndSort(
        IReadOnlyList<StemSummaryRow> all,
        StemsCountFilter filter,
        StemsAssociationFilter association,
        string? search,
        StemSortSpec sort)
    {
        var normalizedSearch = ArabicSearchQueryNormalizer.Normalize(search, stripWhitespace: true);

        IEnumerable<StemSummaryRow> rows = all;
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            rows = rows.Where(r => r.NormalizedStemText.Contains(normalizedSearch, StringComparison.Ordinal));
        }

        if (filter.IsActive)
        {
            rows = rows.Where(r => MatchesFilter(r, filter));
        }

        // Primary (dominant) association filters (Feature 026, US7). DominantRootId/DominantLemmaId are the
        // same primary associations the list row displays; a stem whose primary differs is excluded even if
        // it co-occurs with the filtered id (primary-not-sole — pinned by MorphologyAssociationFilterTests).
        if (association.RootId is int rootId)
        {
            rows = rows.Where(r => r.DominantRootId == rootId);
        }

        if (association.LemmaId is int lemmaId)
        {
            rows = rows.Where(r => r.DominantLemmaId == lemmaId);
        }

        return ApplySort(rows, sort);
    }

    private static bool MatchesFilter(StemSummaryRow row, StemsCountFilter filter) =>
        filter.Occurrences.Includes(row.OccurrencesCount)
        && filter.Ayahs.Includes(row.AyahsCount)
        && filter.Surahs.Includes(row.SurahsCount)
        && filter.SimpleWords.Includes(row.SimpleWordsCount)
        && filter.TashkeelWords.Includes(row.TashkeelWordsCount);

    // Ordering is part of the read contract (see the reads README). Every allowlisted column is already
    // on the row, so no branch costs a join, and each tie-break chain is identical in BOTH directions —
    // reversing a column never reshuffles its ties, which keeps paging deterministic.
    private static IEnumerable<StemSummaryRow> ApplySort(IEnumerable<StemSummaryRow> rows, StemSortSpec sort) => sort.Column switch
    {
        StemSortColumn.Alpha => ByText(rows, r => r.NormalizedStemText, sort.Direction),
        StemSortColumn.Occurrences => ByCount(rows, r => r.OccurrencesCount, sort.Direction),
        StemSortColumn.Ayahs => ByCount(rows, r => r.AyahsCount, sort.Direction),
        StemSortColumn.Surahs => ByCount(rows, r => r.SurahsCount, sort.Direction),
        StemSortColumn.SimpleWords => ByCount(rows, r => r.SimpleWordsCount, sort.Direction),
        StemSortColumn.TashkeelWords => ByCount(rows, r => r.TashkeelWordsCount, sort.Direction),
        StemSortColumn.MushafOrder => rows
            .OrderBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id),
        // Explicit, so a column added without an arm here fails loudly instead of silently
        // serving Mushaf order (mirrors the word-types SQL switches).
        _ => throw new InvalidOperationException($"Unhandled {nameof(StemSortColumn)} value: {sort.Column}."),
    };

    private static IOrderedEnumerable<StemSummaryRow> ByCount(
        IEnumerable<StemSummaryRow> rows,
        Func<StemSummaryRow, int> count,
        WordSortDirection direction) =>
        (direction == WordSortDirection.Ascending ? rows.OrderBy(count) : rows.OrderByDescending(count))
            .ThenBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id);

    // Alpha ties break on Id ALONE — deliberately no Mushaf tie-break, preserving the exact row order
    // existing sort=alpha links already return (pinned by StemsListReadTests' alpha sequence).
    private static IOrderedEnumerable<StemSummaryRow> ByText(
        IEnumerable<StemSummaryRow> rows,
        Func<StemSummaryRow, string> text,
        WordSortDirection direction) =>
        (direction == WordSortDirection.Ascending
            ? rows.OrderBy(text, StringComparer.Ordinal)
            : rows.OrderByDescending(text, StringComparer.Ordinal))
            .ThenBy(r => r.Id);

    private static StemListItemDto ToListItem(StemSummaryRow row) =>
        new(
            row.Id,
            row.StemText,
            row.DominantLemmaId,
            row.DominantLemmaText,
            row.DominantRootId,
            row.DominantRootText,
            row.OccurrencesCount,
            row.AyahsCount,
            row.SurahsCount,
            row.SimpleWordsCount,
            row.TashkeelWordsCount);

    private static StemSummaryDto ToSummaryDto(StemSummaryRow row)
    {
        var distribution = row.TypeDistribution is { Count: > 0 }
            ? row.TypeDistribution.Select(ToTypeSummary).ToList()
            : new List<TypeSummaryDto> { NoType };

        return new StemSummaryDto(
            row.Id,
            row.StemText,
            row.DominantLemmaId,
            row.DominantLemmaText,
            row.DominantRootId,
            row.DominantRootText,
            row.OccurrencesCount,
            row.AyahsCount,
            row.SurahsCount,
            row.SimpleWordsCount,
            row.TashkeelWordsCount,
            distribution);
    }

    private static TypeSummaryDto ToTypeSummary(StemTypeDistributionRow row) =>
        new(row.Code, row.ArabicLabel, row.OccurrencesCount);
}
