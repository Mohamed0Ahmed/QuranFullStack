using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

/// <summary>
/// Applies search, sort, and paging over the compute-once whole roots summary
/// (research D2). Shared by <see cref="EfRootsReader"/> and
/// <see cref="CachedRootsReader"/>.
/// </summary>
internal static class RootsListDerivation
{
    /// <summary>
    /// Arabic symmetric fold map (mirrors Feature 014). Shared by the SQL
    /// <c>translate()</c> normalization and the C# query-side fold so search
    /// stays symmetric.
    /// </summary>
    internal const string ArabicFoldFrom = "أإآٱؤئةىي";

    internal const string ArabicFoldTo = "ااااواهيي";

    public static PagedResult<RootListItemDto> ToPage(
        IReadOnlyList<RootSummaryRow> all,
        string? search,
        RootSort sort,
        int page,
        int pageSize)
    {
        var rows = FilterAndSort(all, search, sort);
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
        string? search,
        RootSort sort)
    {
        var normalizedSearch = NormalizeArabicQuery(search);

        IEnumerable<RootSummaryRow> rows = all;
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            rows = rows.Where(r => r.NormalizedRootText.Contains(normalizedSearch, StringComparison.Ordinal));
        }

        return ApplySort(rows, sort);
    }

    private static IEnumerable<RootSummaryRow> ApplySort(IEnumerable<RootSummaryRow> rows, RootSort sort) => sort switch
    {
        RootSort.Occurrences => rows
            .OrderByDescending(r => r.OccurrencesCount)
            .ThenBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id),
        RootSort.Alpha => rows
            .OrderBy(r => r.NormalizedRootText, StringComparer.Ordinal)
            .ThenBy(r => r.Id),
        _ => rows
            .OrderBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id),
    };

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
            row.StemsCount,
            row.FirstVerseKey);

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
            row.StemsCount,
            row.FirstVerseKey);

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
