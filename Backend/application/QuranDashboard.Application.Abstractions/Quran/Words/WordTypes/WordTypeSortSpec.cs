namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

/// <summary>
/// The allowlisted Word Types sort columns, shared by the words view and the three grouped
/// (roots/stems/lemmas) views. Every member maps to a column both CTEs already project. The
/// type/root/stem/lemma label columns are deliberately excluded (see the reads README's ordering
/// contract), and the grouped member-word detail read takes no sort at all.
/// </summary>
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

/// <summary>
/// A parsed Word Types ordering: an allowlisted column plus its direction. The pair travels together
/// from the parser to the reader and the cache key so the two halves can never drift apart.
/// <para>
/// Unlike the other four explorers, Word Types defaults to <c>occurrences</c> (descending) rather than
/// Mushaf order.
/// </para>
/// </summary>
public readonly record struct WordTypeSortSpec(WordTypeSortColumn Column, WordSortDirection Direction)
{
    /// <summary>The ordering used when the request carries no sort token.</summary>
    public static WordTypeSortSpec Default { get; } = Natural(WordTypeSortColumn.Occurrences);

    /// <summary>The column at its natural direction — what a bare token means.</summary>
    public static WordTypeSortSpec Natural(WordTypeSortColumn column) => new(column, NaturalDirectionOf(column));

    /// <summary>
    /// Counts read most-first, so their natural direction is descending; text and the Mushaf release
    /// order read forward.
    /// </summary>
    public static WordSortDirection NaturalDirectionOf(WordTypeSortColumn column) => column switch
    {
        WordTypeSortColumn.MushafOrder => WordSortDirection.Ascending,
        WordTypeSortColumn.Alpha => WordSortDirection.Ascending,
        WordTypeSortColumn.Occurrences => WordSortDirection.Descending,
        WordTypeSortColumn.Ayahs => WordSortDirection.Descending,
        WordTypeSortColumn.Surahs => WordSortDirection.Descending,
        _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeSortColumn)} value."),
    };

    /// <summary>
    /// The canonical wire/cache token: bare for the column's natural direction, suffixed for the
    /// opposite one. mushaf-order is ascending-only by contract and never carries a suffix.
    /// </summary>
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
    /// <summary>
    /// Parses a raw request token against the Word Types column allowlist. An unknown column, or any
    /// direction suffix on mushaf-order, fails — the caller maps that to a controlled 400.
    /// </summary>
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
