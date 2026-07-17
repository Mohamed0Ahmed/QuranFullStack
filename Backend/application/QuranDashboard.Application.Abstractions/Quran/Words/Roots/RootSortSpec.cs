namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots;

/// <summary>
/// The allowlisted Roots list sort columns. Every member maps to a value already present on the
/// summary row at the sort point, so no column costs a join.
/// </summary>
public enum RootSortColumn
{
    MushafOrder,
    Occurrences,
    Alpha,
    Ayahs,
    Surahs,
    SimpleWords,
    TashkeelWords,
    Lemmas,
    Stems,
}

public static class RootSortKeys
{
    public const string MushafOrder = "mushaf-order";
    public const string Occurrences = "occurrences";
    public const string Alpha = "alpha";
    public const string Ayahs = "ayahs";
    public const string Surahs = "surahs";
    public const string SimpleWords = "simple";
    public const string TashkeelWords = "tashkeel";
    public const string Lemmas = "lemmas";
    public const string Stems = "stems";
}

/// <summary>
/// A parsed Roots ordering: an allowlisted column plus its direction. The pair travels together from
/// the parser to the reader and the cache key so the two halves can never drift apart.
/// </summary>
public readonly record struct RootSortSpec(RootSortColumn Column, WordSortDirection Direction)
{
    /// <summary>The ordering used when the request carries no sort token.</summary>
    public static RootSortSpec Default { get; } = Natural(RootSortColumn.MushafOrder);

    /// <summary>The column at its natural direction — what a bare token means.</summary>
    public static RootSortSpec Natural(RootSortColumn column) => new(column, NaturalDirectionOf(column));

    /// <summary>
    /// Counts read most-first, so their natural direction is descending; text and the Mushaf release
    /// order read forward.
    /// </summary>
    public static WordSortDirection NaturalDirectionOf(RootSortColumn column) => column switch
    {
        RootSortColumn.MushafOrder => WordSortDirection.Ascending,
        RootSortColumn.Alpha => WordSortDirection.Ascending,
        RootSortColumn.Occurrences => WordSortDirection.Descending,
        RootSortColumn.Ayahs => WordSortDirection.Descending,
        RootSortColumn.Surahs => WordSortDirection.Descending,
        RootSortColumn.SimpleWords => WordSortDirection.Descending,
        RootSortColumn.TashkeelWords => WordSortDirection.Descending,
        RootSortColumn.Lemmas => WordSortDirection.Descending,
        RootSortColumn.Stems => WordSortDirection.Descending,
        _ => throw new InvalidOperationException($"Unhandled {nameof(RootSortColumn)} value."),
    };

    /// <summary>
    /// The canonical wire/cache token: bare for the column's natural direction, suffixed for the
    /// opposite one. mushaf-order is ascending-only by contract and never carries a suffix.
    /// </summary>
    public string CanonicalToken() => Column == RootSortColumn.MushafOrder
        ? RootSortKeys.MushafOrder
        : WordSortToken.Canonical(ColumnKey(Column), Direction, NaturalDirectionOf(Column));

    private static string ColumnKey(RootSortColumn column) => column switch
    {
        RootSortColumn.MushafOrder => RootSortKeys.MushafOrder,
        RootSortColumn.Occurrences => RootSortKeys.Occurrences,
        RootSortColumn.Alpha => RootSortKeys.Alpha,
        RootSortColumn.Ayahs => RootSortKeys.Ayahs,
        RootSortColumn.Surahs => RootSortKeys.Surahs,
        RootSortColumn.SimpleWords => RootSortKeys.SimpleWords,
        RootSortColumn.TashkeelWords => RootSortKeys.TashkeelWords,
        RootSortColumn.Lemmas => RootSortKeys.Lemmas,
        RootSortColumn.Stems => RootSortKeys.Stems,
        _ => throw new InvalidOperationException($"Unhandled {nameof(RootSortColumn)} value."),
    };
}

public static class RootSortParser
{
    /// <summary>
    /// Parses a raw request token against the Roots column allowlist. An unknown column, or any
    /// direction suffix on mushaf-order, fails — the caller maps that to a controlled 400.
    /// </summary>
    public static bool TryParse(string? value, out RootSortSpec spec)
    {
        spec = default;

        if (!WordSortToken.TrySplit(value, out var columnToken, out var direction)
            || !TryParseColumn(columnToken, out var column))
        {
            return false;
        }

        // mushaf-order is the release order, not a column: ascending-only, bare token only.
        if (column == RootSortColumn.MushafOrder && direction is not null)
        {
            return false;
        }

        spec = new RootSortSpec(column, direction ?? RootSortSpec.NaturalDirectionOf(column));
        return true;
    }

    private static bool TryParseColumn(string token, out RootSortColumn column)
    {
        switch (token)
        {
            case RootSortKeys.MushafOrder:
                column = RootSortColumn.MushafOrder;
                return true;
            case RootSortKeys.Occurrences:
                column = RootSortColumn.Occurrences;
                return true;
            case RootSortKeys.Alpha:
                column = RootSortColumn.Alpha;
                return true;
            case RootSortKeys.Ayahs:
                column = RootSortColumn.Ayahs;
                return true;
            case RootSortKeys.Surahs:
                column = RootSortColumn.Surahs;
                return true;
            case RootSortKeys.SimpleWords:
                column = RootSortColumn.SimpleWords;
                return true;
            case RootSortKeys.TashkeelWords:
                column = RootSortColumn.TashkeelWords;
                return true;
            case RootSortKeys.Lemmas:
                column = RootSortColumn.Lemmas;
                return true;
            case RootSortKeys.Stems:
                column = RootSortColumn.Stems;
                return true;
            default:
                column = default;
                return false;
        }
    }
}
