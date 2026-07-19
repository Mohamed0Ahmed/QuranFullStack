namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

// Every member maps to a column both CTEs already project. The label columns are deliberately excluded
// (see the reads README's ordering contract), and the grouped member-word detail read takes no sort.
public enum WordTypeSortColumn
{
    Occurrences,
    Ayahs,
    Surahs,
    MushafOrder,
    Alpha,
}

public static class WordTypeSortKeys
{
    public const string Occurrences = "occurrences";
    public const string Ayahs = "ayahs";
    public const string Surahs = "surahs";
    public const string MushafOrder = "mushaf-order";
    public const string Alpha = "alpha";
}

// Unlike the other four explorers, Word Types defaults to occurrences (descending) rather than Mushaf order.
public readonly record struct WordTypeSortSpec(WordTypeSortColumn Column, WordSortDirection Direction)
{
    public static WordTypeSortSpec Default { get; } = Natural(WordTypeSortColumn.Occurrences);

    public static WordTypeSortSpec Natural(WordTypeSortColumn column) => new(column, NaturalDirectionOf(column));

    // Counts read most-first (descending natural); text and the Mushaf release order read forward.
    public static WordSortDirection NaturalDirectionOf(WordTypeSortColumn column) => column switch
    {
        WordTypeSortColumn.MushafOrder => WordSortDirection.Ascending,
        WordTypeSortColumn.Alpha => WordSortDirection.Ascending,
        WordTypeSortColumn.Occurrences => WordSortDirection.Descending,
        WordTypeSortColumn.Ayahs => WordSortDirection.Descending,
        WordTypeSortColumn.Surahs => WordSortDirection.Descending,
        _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeSortColumn)} value."),
    };

    // Canonical wire/cache token: bare for the natural direction, suffixed for the opposite one.
    // mushaf-order is ascending-only by contract and never carries a suffix.
    public string CanonicalToken() => Column == WordTypeSortColumn.MushafOrder
        ? WordTypeSortKeys.MushafOrder
        : WordSortToken.Canonical(ColumnKey(Column), Direction, NaturalDirectionOf(Column));

    private static string ColumnKey(WordTypeSortColumn column) => column switch
    {
        WordTypeSortColumn.Occurrences => WordTypeSortKeys.Occurrences,
        WordTypeSortColumn.Ayahs => WordTypeSortKeys.Ayahs,
        WordTypeSortColumn.Surahs => WordTypeSortKeys.Surahs,
        WordTypeSortColumn.MushafOrder => WordTypeSortKeys.MushafOrder,
        WordTypeSortColumn.Alpha => WordTypeSortKeys.Alpha,
        _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeSortColumn)} value."),
    };
}

public static class WordTypeSortParser
{
    public static bool TryParse(string? value, out WordTypeSortSpec spec)
    {
        spec = default;

        if (!WordSortToken.TrySplit(value, out var columnToken, out var direction)
            || !TryParseColumn(columnToken, out var column))
        {
            return false;
        }

        // mushaf-order is the release order, not a column: ascending-only, bare token only.
        if (column == WordTypeSortColumn.MushafOrder && direction is not null)
        {
            return false;
        }

        spec = new WordTypeSortSpec(column, direction ?? WordTypeSortSpec.NaturalDirectionOf(column));
        return true;
    }

    private static bool TryParseColumn(string token, out WordTypeSortColumn column)
    {
        switch (token)
        {
            case WordTypeSortKeys.Occurrences:
                column = WordTypeSortColumn.Occurrences;
                return true;
            case WordTypeSortKeys.Ayahs:
                column = WordTypeSortColumn.Ayahs;
                return true;
            case WordTypeSortKeys.Surahs:
                column = WordTypeSortColumn.Surahs;
                return true;
            case WordTypeSortKeys.MushafOrder:
                column = WordTypeSortColumn.MushafOrder;
                return true;
            case WordTypeSortKeys.Alpha:
                column = WordTypeSortColumn.Alpha;
                return true;
            default:
                column = default;
                return false;
        }
    }
}
