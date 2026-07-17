namespace QuranDashboard.Application.Abstractions.Quran.Words;

/// <summary>
/// The allowlisted Unique Words list sort columns. Every member maps to a column the list query
/// already projects at the sort point. The type/root chips (page-only enrichment) and the
/// missing-surahs count (computed after paging, and the monotone inverse of السور) are deliberately
/// excluded — see the reads README's ordering contract.
/// </summary>
public enum UniqueWordSortColumn
{
    MushafOrder,
    Occurrences,
    Alpha,
    Ayahs,
    Surahs,
}

public static class UniqueWordSortKeys
{
    public const string MushafOrder = "mushaf-order";
    public const string Occurrences = "occurrences";
    public const string Alpha = "alpha";
    public const string Ayahs = "ayahs";
    public const string Surahs = "surahs";
}

/// <summary>
/// A parsed Unique Words ordering: an allowlisted column plus its direction. The pair travels together
/// from the parser to the reader and the cache key so the two halves can never drift apart.
/// </summary>
public readonly record struct UniqueWordSortSpec(UniqueWordSortColumn Column, WordSortDirection Direction)
{
    /// <summary>The ordering used when the request carries no sort token.</summary>
    public static UniqueWordSortSpec Default { get; } = Natural(UniqueWordSortColumn.MushafOrder);

    /// <summary>The column at its natural direction — what a bare token means.</summary>
    public static UniqueWordSortSpec Natural(UniqueWordSortColumn column) => new(column, NaturalDirectionOf(column));

    /// <summary>
    /// Counts read most-first, so their natural direction is descending; text and the Mushaf release
    /// order read forward.
    /// </summary>
    public static WordSortDirection NaturalDirectionOf(UniqueWordSortColumn column) => column switch
    {
        UniqueWordSortColumn.MushafOrder => WordSortDirection.Ascending,
        UniqueWordSortColumn.Alpha => WordSortDirection.Ascending,
        UniqueWordSortColumn.Occurrences => WordSortDirection.Descending,
        UniqueWordSortColumn.Ayahs => WordSortDirection.Descending,
        UniqueWordSortColumn.Surahs => WordSortDirection.Descending,
        _ => throw new InvalidOperationException($"Unhandled {nameof(UniqueWordSortColumn)} value."),
    };

    /// <summary>
    /// The canonical wire/cache token: bare for the column's natural direction, suffixed for the
    /// opposite one. mushaf-order is ascending-only by contract and never carries a suffix.
    /// </summary>
    public string CanonicalToken() => Column == UniqueWordSortColumn.MushafOrder
        ? UniqueWordSortKeys.MushafOrder
        : WordSortToken.Canonical(ColumnKey(Column), Direction, NaturalDirectionOf(Column));

    private static string ColumnKey(UniqueWordSortColumn column) => column switch
    {
        UniqueWordSortColumn.MushafOrder => UniqueWordSortKeys.MushafOrder,
        UniqueWordSortColumn.Occurrences => UniqueWordSortKeys.Occurrences,
        UniqueWordSortColumn.Alpha => UniqueWordSortKeys.Alpha,
        UniqueWordSortColumn.Ayahs => UniqueWordSortKeys.Ayahs,
        UniqueWordSortColumn.Surahs => UniqueWordSortKeys.Surahs,
        _ => throw new InvalidOperationException($"Unhandled {nameof(UniqueWordSortColumn)} value."),
    };
}

public static class UniqueWordSortParser
{
    /// <summary>
    /// Parses a raw request token against the Unique Words column allowlist. An unknown column, or any
    /// direction suffix on mushaf-order, fails — the caller maps that to a controlled 400.
    /// </summary>
    public static bool TryParse(string? value, out UniqueWordSortSpec spec)
    {
        spec = default;

        if (!WordSortToken.TrySplit(value, out var columnToken, out var direction)
            || !TryParseColumn(columnToken, out var column))
        {
            return false;
        }

        // mushaf-order is the release order, not a column: ascending-only, bare token only.
        if (column == UniqueWordSortColumn.MushafOrder && direction is not null)
        {
            return false;
        }

        spec = new UniqueWordSortSpec(column, direction ?? UniqueWordSortSpec.NaturalDirectionOf(column));
        return true;
    }

    private static bool TryParseColumn(string token, out UniqueWordSortColumn column)
    {
        switch (token)
        {
            case UniqueWordSortKeys.MushafOrder:
                column = UniqueWordSortColumn.MushafOrder;
                return true;
            case UniqueWordSortKeys.Occurrences:
                column = UniqueWordSortColumn.Occurrences;
                return true;
            case UniqueWordSortKeys.Alpha:
                column = UniqueWordSortColumn.Alpha;
                return true;
            case UniqueWordSortKeys.Ayahs:
                column = UniqueWordSortColumn.Ayahs;
                return true;
            case UniqueWordSortKeys.Surahs:
                column = UniqueWordSortColumn.Surahs;
                return true;
            default:
                column = default;
                return false;
        }
    }
}
