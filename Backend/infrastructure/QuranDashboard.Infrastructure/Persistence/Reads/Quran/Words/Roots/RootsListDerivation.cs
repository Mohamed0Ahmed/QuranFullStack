using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

internal static class RootsListDerivation
{
    internal const string ArabicFoldFrom = "أإآٱؤئةىي";

    internal const string ArabicFoldTo = "ااااواهيي";

    public static PagedResult<RootListItemDto> ToPage(
        IReadOnlyList<RootSummaryRow> all,
        RootsCountFilter filter,
        string? search,
        RootSortSpec sort,
        int page,
        int pageSize)
    {
        var rows = FilterAndSort(all, filter, search, sort);
        var materialized = rows.ToList();
        var totalCount = materialized.Count;
        var pageRows = materialized
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageRows.Select(ToListItem).ToList();
        return new PagedResult<RootListItemDto>(page, pageSize, totalCount, items);
    }

    public static RootSummaryDto? ToSummary(IReadOnlyList<RootSummaryRow> all, int id)
    {
        var row = all.FirstOrDefault(r => r.Id == id);
        return row is null ? null : ToSummaryDto(row);
    }

    public static IEnumerable<RootSummaryRow> FilterAndSort(
        IReadOnlyList<RootSummaryRow> all,
        RootsCountFilter filter,
        string? search,
        RootSortSpec sort)
    {
        var normalizedSearch = NormalizeArabicQuery(search);

        IEnumerable<RootSummaryRow> rows = all;
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            rows = rows.Where(r => r.NormalizedRootText.Contains(normalizedSearch, StringComparison.Ordinal));
        }

        if (filter.IsActive)
        {
            rows = rows.Where(r => MatchesFilter(r, filter));
        }

        return ApplySort(rows, sort);
    }

    // Count-range predicates (Feature 026, US5) compare against the same count values the list rows
    // display; every active range ANDs with search/sort. Ranges filter root dimension entries.
    private static bool MatchesFilter(RootSummaryRow row, RootsCountFilter filter) =>
        filter.Occurrences.Includes(row.OccurrencesCount)
        && filter.Ayahs.Includes(row.AyahsCount)
        && filter.Surahs.Includes(row.SurahsCount)
        && filter.SimpleWords.Includes(row.SimpleWordsCount)
        && filter.TashkeelWords.Includes(row.TashkeelWordsCount)
        && filter.Lemmas.Includes(row.LemmasCount)
        && filter.Stems.Includes(row.StemsCount);

    // Ordering is part of the read contract (see the reads README). Every allowlisted column is already
    // on the row, so no branch costs a join, and each tie-break chain is identical in BOTH directions —
    // reversing a column never reshuffles its ties, which keeps paging deterministic.
    private static IEnumerable<RootSummaryRow> ApplySort(IEnumerable<RootSummaryRow> rows, RootSortSpec sort) => sort.Column switch
    {
        RootSortColumn.Alpha => ByText(rows, r => r.NormalizedRootText, sort.Direction),
        RootSortColumn.Occurrences => ByCount(rows, r => r.OccurrencesCount, sort.Direction),
        RootSortColumn.Ayahs => ByCount(rows, r => r.AyahsCount, sort.Direction),
        RootSortColumn.Surahs => ByCount(rows, r => r.SurahsCount, sort.Direction),
        RootSortColumn.SimpleWords => ByCount(rows, r => r.SimpleWordsCount, sort.Direction),
        RootSortColumn.TashkeelWords => ByCount(rows, r => r.TashkeelWordsCount, sort.Direction),
        RootSortColumn.Lemmas => ByCount(rows, r => r.LemmasCount, sort.Direction),
        RootSortColumn.Stems => ByCount(rows, r => r.StemsCount, sort.Direction),
        RootSortColumn.MushafOrder => rows
            .OrderBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id),
        // Explicit, so a column added without an arm here fails loudly instead of silently
        // serving Mushaf order (mirrors the word-types SQL switches).
        _ => throw new InvalidOperationException($"Unhandled {nameof(RootSortColumn)} value: {sort.Column}."),
    };

    // Count columns tie-break on Mushaf order then Id.
    private static IOrderedEnumerable<RootSummaryRow> ByCount(
        IEnumerable<RootSummaryRow> rows,
        Func<RootSummaryRow, int> count,
        WordSortDirection direction) =>
        (direction == WordSortDirection.Ascending ? rows.OrderBy(count) : rows.OrderByDescending(count))
            .ThenBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id);

    // Alpha ties break on Id ALONE — deliberately no Mushaf tie-break, preserving the exact row order
    // existing sort=alpha links already return.
    private static IOrderedEnumerable<RootSummaryRow> ByText(
        IEnumerable<RootSummaryRow> rows,
        Func<RootSummaryRow, string> text,
        WordSortDirection direction) =>
        (direction == WordSortDirection.Ascending
            ? rows.OrderBy(text, StringComparer.Ordinal)
            : rows.OrderByDescending(text, StringComparer.Ordinal))
            .ThenBy(r => r.Id);

    private static RootListItemDto ToListItem(RootSummaryRow row) =>
        new(
            row.Id,
            row.RootText,
            row.OccurrencesCount,
            row.AyahsCount,
            row.SurahsCount,
            row.SimpleWordsCount,
            row.TashkeelWordsCount,
            row.LemmasCount,
            row.StemsCount);

    private static RootSummaryDto ToSummaryDto(RootSummaryRow row) =>
        new(
            row.Id,
            row.RootText,
            row.OccurrencesCount,
            row.AyahsCount,
            row.SurahsCount,
            row.SimpleWordsCount,
            row.TashkeelWordsCount,
            row.LemmasCount,
            row.StemsCount);

    private static string? NormalizeArabicQuery(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var builder = new StringBuilder(search.Length);
        foreach (var ch in search)
        {
            if (IsSkippable(ch) || char.IsWhiteSpace(ch))
            {
                continue;
            }

            builder.Append(Fold(ch));
        }

        var normalized = builder.ToString().ToLowerInvariant();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static bool IsSkippable(char ch) =>
        ch == '\u0640' ||
        ch is >= '\u0610' and <= '\u061A' ||
        ch is >= '\u064B' and <= '\u065F' ||
        ch == '\u0670' ||
        ch is >= '\u06D6' and <= '\u06ED' ||
        ch is >= '\u08D3' and <= '\u08FF';

    private static char Fold(char ch)
    {
        var index = ArabicFoldFrom.IndexOf(ch);
        return index >= 0 ? ArabicFoldTo[index] : ch;
    }
}
