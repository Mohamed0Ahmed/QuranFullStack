using System.Text;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

/// <summary>
/// In-memory catalogue derivation for the Stems Explorer (Feature 016). Mirrors
/// the Lemmas pattern: normalized Arabic contains search, deterministic sort,
/// and paging over the cached whole-summary list.
/// </summary>
internal static class StemsListDerivation
{
    internal const string ArabicFoldFrom = "أإآٱؤئةىي";

    internal const string ArabicFoldTo = "ااااواهيي";

    internal static readonly TypeSummaryDto NoType = new(string.Empty, "غير محدَّد", 0);

    public static PagedResult<StemListItemDto> ToPage(
        IReadOnlyList<StemSummaryRow> all,
        StemsCountFilter filter,
        string? search,
        StemSort sort,
        int page,
        int pageSize)
    {
        var rows = FilterAndSort(all, filter, search, sort);
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
        string? search,
        StemSort sort)
    {
        var normalizedSearch = NormalizeArabicQuery(search);

        IEnumerable<StemSummaryRow> rows = all;
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            rows = rows.Where(r => r.NormalizedStemText.Contains(normalizedSearch, StringComparison.Ordinal));
        }

        if (filter.IsActive)
        {
            rows = rows.Where(r => MatchesFilter(r, filter));
        }

        return ApplySort(rows, sort);
    }

    // Count-range predicates (Feature 026, US5) compare against the same count values the list rows
    // display; every active range ANDs with search/sort. Ranges filter stem dimension entries.
    private static bool MatchesFilter(StemSummaryRow row, StemsCountFilter filter) =>
        filter.Occurrences.Includes(row.OccurrencesCount)
        && filter.Ayahs.Includes(row.AyahsCount)
        && filter.Surahs.Includes(row.SurahsCount)
        && filter.SimpleWords.Includes(row.SimpleWordsCount)
        && filter.TashkeelWords.Includes(row.TashkeelWordsCount);

    private static IEnumerable<StemSummaryRow> ApplySort(IEnumerable<StemSummaryRow> rows, StemSort sort) => sort switch
    {
        StemSort.Occurrences => rows
            .OrderByDescending(r => r.OccurrencesCount)
            .ThenBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id),
        StemSort.Alpha => rows
            .OrderBy(r => r.NormalizedStemText, StringComparer.Ordinal)
            .ThenBy(r => r.Id),
        _ => rows
            .OrderBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id),
    };

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

    internal static string? NormalizeArabicQuery(string? search)
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
