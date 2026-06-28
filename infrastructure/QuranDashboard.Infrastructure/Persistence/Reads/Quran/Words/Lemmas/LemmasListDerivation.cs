using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;

/// <summary>
/// In-memory catalogue derivation for the Lemmas Explorer (Feature 016). Mirrors
/// <c>RootsListDerivation</c>: normalized Arabic contains search, deterministic
/// sort, and paging over the cached whole-summary list. The type distribution
/// loaded per lemma is already ordered count descending then earliest Mushaf
/// occurrence ascending.
/// </summary>
internal static class LemmasListDerivation
{
    internal const string ArabicFoldFrom = "أإآٱؤئةىي";

    internal const string ArabicFoldTo = "ااااواهيي";

    /// <summary>
    /// Placeholder type summary when a lemma has no morphology rows. Labels are
    /// controlled values; counts are zero so the distribution total is zero.
    /// </summary>
    internal static readonly TypeSummaryDto NoType = new(string.Empty, "غير محدَّد", "Unspecified", 0, 0, 0, 0);

    public static PagedResult<LemmaListItemDto> ToPage(
        IReadOnlyList<LemmaSummaryRow> all,
        string? search,
        LemmaSort sort,
        int page,
        int pageSize)
    {
        var rows = FilterAndSort(all, search, sort);
        var materialized = rows.ToList();
        var totalCount = materialized.Count;
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<LemmaListItemDto>(page, pageSize, totalCount, []);
        }

        var pageRows = materialized
            .Skip(skip.Value)
            .Take(pageSize)
            .ToList();

        var items = pageRows.Select(ToListItem).ToList();
        return new PagedResult<LemmaListItemDto>(page, pageSize, totalCount, items);
    }

    public static LemmaSummaryDto? ToSummary(IReadOnlyList<LemmaSummaryRow> all, int id)
    {
        var row = all.FirstOrDefault(r => r.Id == id);
        return row is null ? null : ToSummaryDto(row);
    }

    public static IEnumerable<LemmaSummaryRow> FilterAndSort(
        IReadOnlyList<LemmaSummaryRow> all,
        string? search,
        LemmaSort sort)
    {
        var normalizedSearch = NormalizeArabicQuery(search);

        IEnumerable<LemmaSummaryRow> rows = all;
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            rows = rows.Where(r => r.NormalizedLemmaText.Contains(normalizedSearch, StringComparison.Ordinal));
        }

        return ApplySort(rows, sort);
    }

    private static IEnumerable<LemmaSummaryRow> ApplySort(IEnumerable<LemmaSummaryRow> rows, LemmaSort sort) => sort switch
    {
        LemmaSort.Occurrences => rows
            .OrderByDescending(r => r.OccurrencesCount)
            .ThenBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id),
        LemmaSort.Alpha => rows
            .OrderBy(r => r.NormalizedLemmaText, StringComparer.Ordinal)
            .ThenBy(r => r.Id),
        _ => rows
            .OrderBy(r => r.FirstWordOrderInMushaf)
            .ThenBy(r => r.Id),
    };

    private static LemmaListItemDto ToListItem(LemmaSummaryRow row) =>
        new(
            row.Id,
            row.LemmaText,
            row.RootId,
            row.RootText,
            row.OccurrencesCount,
            row.AyahsCount,
            row.SurahsCount,
            row.SimpleWordsCount,
            row.TashkeelWordsCount,
            row.StemsCount);

    private static LemmaSummaryDto ToSummaryDto(LemmaSummaryRow row)
    {
        var distribution = row.TypeDistribution is { Count: > 0 }
            ? row.TypeDistribution.Select(ToTypeSummary).ToList()
            : new List<TypeSummaryDto> { NoType };

        return new LemmaSummaryDto(
            row.Id,
            row.LemmaText,
            row.RootId,
            row.RootText,
            row.OccurrencesCount,
            row.AyahsCount,
            row.SurahsCount,
            row.SimpleWordsCount,
            row.TashkeelWordsCount,
            row.StemsCount,
            distribution);
    }

    private static TypeSummaryDto ToTypeSummary(LemmaTypeDistributionRow row) =>
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
