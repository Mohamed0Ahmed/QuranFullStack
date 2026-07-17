namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems;

/// <summary>
/// The allowlisted Stems list sort columns. Every member maps to a value already present on the
/// summary row at the sort point. The dominant root/lemma TEXT columns are deliberately excluded
/// (see the reads README's ordering contract).
/// </summary>
public enum StemSortColumn
{
    MushafOrder,
    Occurrences,
    Alpha,
    Ayahs,
    Surahs,
    SimpleWords,
    TashkeelWords,
}

public static class StemSortKeys
{
    public const string MushafOrder = "mushaf-order";
    public const string Occurrences = "occurrences";
    public const string Alpha = "alpha";
    public const string Ayahs = "ayahs";
    public const string Surahs = "surahs";
    public const string SimpleWords = "simple";
    public const string TashkeelWords = "tashkeel";
}

/// <summary>
/// A parsed Stems ordering: an allowlisted column plus its direction. The pair travels together from
/// the parser to the reader and the cache key so the two halves can never drift apart.
/// </summary>
public readonly record struct StemSortSpec(StemSortColumn Column, WordSortDirection Direction)
{
    /// <summary>The ordering used when the request carries no sort token.</summary>
    public static StemSortSpec Default { get; } = Natural(StemSortColumn.MushafOrder);

    /// <summary>The column at its natural direction — what a bare token means.</summary>
    public static StemSortSpec Natural(StemSortColumn column) => new(column, NaturalDirectionOf(column));

    /// <summary>
    /// Counts read most-first, so their natural direction is descending; text and the Mushaf release
    /// order read forward.
    /// </summary>
    public static WordSortDirection NaturalDirectionOf(StemSortColumn column) => column switch
    {
        StemSortColumn.MushafOrder => WordSortDirection.Ascending,
        StemSortColumn.Alpha => WordSortDirection.Ascending,
        StemSortColumn.Occurrences => WordSortDirection.Descending,
        StemSortColumn.Ayahs => WordSortDirection.Descending,
        StemSortColumn.Surahs => WordSortDirection.Descending,
        StemSortColumn.SimpleWords => WordSortDirection.Descending,
        StemSortColumn.TashkeelWords => WordSortDirection.Descending,
        _ => throw new InvalidOperationException($"Unhandled {nameof(StemSortColumn)} value."),
    };

    /// <summary>
    /// The canonical wire/cache token: bare for the column's natural direction, suffixed for the
    /// opposite one. mushaf-order is ascending-only by contract and never carries a suffix.
    /// </summary>
    public string CanonicalToken() => Column == StemSortColumn.MushafOrder
        ? StemSortKeys.MushafOrder
        : WordSortToken.Canonical(ColumnKey(Column), Direction, NaturalDirectionOf(Column));

    private static string ColumnKey(StemSortColumn column) => column switch
    {
        StemSortColumn.MushafOrder => StemSortKeys.MushafOrder,
        StemSortColumn.Occurrences => StemSortKeys.Occurrences,
        StemSortColumn.Alpha => StemSortKeys.Alpha,
        StemSortColumn.Ayahs => StemSortKeys.Ayahs,
        StemSortColumn.Surahs => StemSortKeys.Surahs,
        StemSortColumn.SimpleWords => StemSortKeys.SimpleWords,
        StemSortColumn.TashkeelWords => StemSortKeys.TashkeelWords,
        _ => throw new InvalidOperationException($"Unhandled {nameof(StemSortColumn)} value."),
    };
}

public static class StemSortParser
{
    /// <summary>
    /// Parses a raw request token against the Stems column allowlist. An unknown column, or any
    /// direction suffix on mushaf-order, fails — the caller maps that to a controlled 400.
    /// </summary>
    public static bool TryParse(string? value, out StemSortSpec spec)
    {
        spec = default;

        if (!WordSortToken.TrySplit(value, out var columnToken, out var direction)
            || !TryParseColumn(columnToken, out var column))
        {
            return false;
        }

        // mushaf-order is the release order, not a column: ascending-only, bare token only.
        if (column == StemSortColumn.MushafOrder && direction is not null)
        {
            return false;
        }

        spec = new StemSortSpec(column, direction ?? StemSortSpec.NaturalDirectionOf(column));
        return true;
    }

    private static bool TryParseColumn(string token, out StemSortColumn column)
    {
        switch (token)
        {
            case StemSortKeys.MushafOrder:
                column = StemSortColumn.MushafOrder;
                return true;
            case StemSortKeys.Occurrences:
                column = StemSortColumn.Occurrences;
                return true;
            case StemSortKeys.Alpha:
                column = StemSortColumn.Alpha;
                return true;
            case StemSortKeys.Ayahs:
                column = StemSortColumn.Ayahs;
                return true;
            case StemSortKeys.Surahs:
                column = StemSortColumn.Surahs;
                return true;
            case StemSortKeys.SimpleWords:
                column = StemSortColumn.SimpleWords;
                return true;
            case StemSortKeys.TashkeelWords:
                column = StemSortColumn.TashkeelWords;
                return true;
            default:
                column = default;
                return false;
        }
    }
}
