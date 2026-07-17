namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

/// <summary>
/// The allowlisted Lemmas list sort columns. Every member maps to a value already present on the
/// summary row at the sort point. The related root TEXT is deliberately excluded (see the reads
/// README's ordering contract).
/// </summary>
public enum LemmaSortColumn
{
    MushafOrder,
    Occurrences,
    Alpha,
    Ayahs,
    Surahs,
    SimpleWords,
    TashkeelWords,
    Stems,
}

public static class LemmaSortKeys
{
    public const string MushafOrder = "mushaf-order";
    public const string Occurrences = "occurrences";
    public const string Alpha = "alpha";
    public const string Ayahs = "ayahs";
    public const string Surahs = "surahs";
    public const string SimpleWords = "simple";
    public const string TashkeelWords = "tashkeel";
    public const string Stems = "stems";
}

/// <summary>
/// A parsed Lemmas ordering: an allowlisted column plus its direction. The pair travels together from
/// the parser to the reader and the cache key so the two halves can never drift apart.
/// </summary>
public readonly record struct LemmaSortSpec(LemmaSortColumn Column, WordSortDirection Direction)
{
    /// <summary>The ordering used when the request carries no sort token.</summary>
    public static LemmaSortSpec Default { get; } = Natural(LemmaSortColumn.MushafOrder);

    /// <summary>The column at its natural direction — what a bare token means.</summary>
    public static LemmaSortSpec Natural(LemmaSortColumn column) => new(column, NaturalDirectionOf(column));

    /// <summary>
    /// Counts read most-first, so their natural direction is descending; text and the Mushaf release
    /// order read forward.
    /// </summary>
    public static WordSortDirection NaturalDirectionOf(LemmaSortColumn column) => column switch
    {
        LemmaSortColumn.MushafOrder => WordSortDirection.Ascending,
        LemmaSortColumn.Alpha => WordSortDirection.Ascending,
        LemmaSortColumn.Occurrences => WordSortDirection.Descending,
        LemmaSortColumn.Ayahs => WordSortDirection.Descending,
        LemmaSortColumn.Surahs => WordSortDirection.Descending,
        LemmaSortColumn.SimpleWords => WordSortDirection.Descending,
        LemmaSortColumn.TashkeelWords => WordSortDirection.Descending,
        LemmaSortColumn.Stems => WordSortDirection.Descending,
        _ => throw new InvalidOperationException($"Unhandled {nameof(LemmaSortColumn)} value."),
    };

    /// <summary>
    /// The canonical wire/cache token: bare for the column's natural direction, suffixed for the
    /// opposite one. mushaf-order is ascending-only by contract and never carries a suffix.
    /// </summary>
    public string CanonicalToken() => Column == LemmaSortColumn.MushafOrder
        ? LemmaSortKeys.MushafOrder
        : WordSortToken.Canonical(ColumnKey(Column), Direction, NaturalDirectionOf(Column));

    private static string ColumnKey(LemmaSortColumn column) => column switch
    {
        LemmaSortColumn.MushafOrder => LemmaSortKeys.MushafOrder,
        LemmaSortColumn.Occurrences => LemmaSortKeys.Occurrences,
        LemmaSortColumn.Alpha => LemmaSortKeys.Alpha,
        LemmaSortColumn.Ayahs => LemmaSortKeys.Ayahs,
        LemmaSortColumn.Surahs => LemmaSortKeys.Surahs,
        LemmaSortColumn.SimpleWords => LemmaSortKeys.SimpleWords,
        LemmaSortColumn.TashkeelWords => LemmaSortKeys.TashkeelWords,
        LemmaSortColumn.Stems => LemmaSortKeys.Stems,
        _ => throw new InvalidOperationException($"Unhandled {nameof(LemmaSortColumn)} value."),
    };
}

public static class LemmaSortParser
{
    /// <summary>
    /// Parses a raw request token against the Lemmas column allowlist. An unknown column, or any
    /// direction suffix on mushaf-order, fails — the caller maps that to a controlled 400.
    /// </summary>
    public static bool TryParse(string? value, out LemmaSortSpec spec)
    {
        spec = default;

        if (!WordSortToken.TrySplit(value, out var columnToken, out var direction)
            || !TryParseColumn(columnToken, out var column))
        {
            return false;
        }

        // mushaf-order is the release order, not a column: ascending-only, bare token only.
        if (column == LemmaSortColumn.MushafOrder && direction is not null)
        {
            return false;
        }

        spec = new LemmaSortSpec(column, direction ?? LemmaSortSpec.NaturalDirectionOf(column));
        return true;
    }

    private static bool TryParseColumn(string token, out LemmaSortColumn column)
    {
        switch (token)
        {
            case LemmaSortKeys.MushafOrder:
                column = LemmaSortColumn.MushafOrder;
                return true;
            case LemmaSortKeys.Occurrences:
                column = LemmaSortColumn.Occurrences;
                return true;
            case LemmaSortKeys.Alpha:
                column = LemmaSortColumn.Alpha;
                return true;
            case LemmaSortKeys.Ayahs:
                column = LemmaSortColumn.Ayahs;
                return true;
            case LemmaSortKeys.Surahs:
                column = LemmaSortColumn.Surahs;
                return true;
            case LemmaSortKeys.SimpleWords:
                column = LemmaSortColumn.SimpleWords;
                return true;
            case LemmaSortKeys.TashkeelWords:
                column = LemmaSortColumn.TashkeelWords;
                return true;
            case LemmaSortKeys.Stems:
                column = LemmaSortColumn.Stems;
                return true;
            default:
                column = default;
                return false;
        }
    }
}
