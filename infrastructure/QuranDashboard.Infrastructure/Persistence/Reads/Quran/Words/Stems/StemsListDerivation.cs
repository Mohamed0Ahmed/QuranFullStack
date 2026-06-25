using System.Text;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

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

    internal static readonly TypeSummaryDto NoType = new(string.Empty, "غير محدَّد", "Unspecified", 0, 0, 0, 0);

    public static PagedResult<StemListItemDto> ToPage(
        IReadOnlyList<StemSummaryRow> all,
        string? search,
        StemSort sort,
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
        return new PagedResult<StemListItemDto>(page, pageSize, totalCount, items);
    }

    public static StemSummaryDto? ToSummary(IReadOnlyList<StemSummaryRow> all, int id)
    {
        var row = all.FirstOrDefault(r => r.Id == id);
        return row is null ? null : ToSummaryDto(row);
    }

    public static IEnumerable<StemSummaryRow> FilterAndSort(
        IReadOnlyList<StemSummaryRow> all,
        string? search,
        StemSort sort)
    {
        var normalizedSearch = NormalizeArabicQuery(search);

        IEnumerable<StemSummaryRow> rows = all;
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            rows = rows.Where(r => r.NormalizedStemText.Contains(normalizedSearch, StringComparison.Ordinal));
        }

        return ApplySort(rows, sort);
    }

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
            row.DominantLemmaBuckwalter,
            row.DominantRootId,
            row.DominantRootText,
            row.DominantRootBuckwalter,
            DominantType(row.TypeDistribution),
            OtherTypesCount(row.TypeDistribution),
            row.OccurrencesCount,
            row.AyahsCount,
            row.SurahsCount,
            row.SimpleWordsCount,
            row.TashkeelWordsCount,
            row.FirstVerseKey);

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
            row.DominantLemmaBuckwalter,
            row.DominantRootId,
            row.DominantRootText,
            row.DominantRootBuckwalter,
            DominantType(row.TypeDistribution),
            OtherTypesCount(row.TypeDistribution),
            row.OccurrencesCount,
            row.AyahsCount,
            row.SurahsCount,
            row.SimpleWordsCount,
            row.TashkeelWordsCount,
            row.FirstVerseKey,
            distribution);
    }

    private static TypeSummaryDto DominantType(IReadOnlyList<StemTypeDistributionRow> distribution) =>
        distribution is { Count: > 0 } ? ToTypeSummary(distribution[0]) : NoType;

    private static int OtherTypesCount(IReadOnlyList<StemTypeDistributionRow> distribution) =>
        distribution is null || distribution.Count <= 1 ? 0 : distribution.Count - 1;

    private static TypeSummaryDto ToTypeSummary(StemTypeDistributionRow row) =>
        new(
            row.Code,
            row.ArabicLabel,
            row.EnglishLabel,
            row.OccurrencesCount,
            row.FirstSurahNumber,
            row.FirstAyahNumber,
            row.FirstWordNumber);

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
