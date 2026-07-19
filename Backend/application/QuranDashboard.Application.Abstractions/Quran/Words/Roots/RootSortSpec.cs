namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots;

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

public readonly record struct RootSortSpec(RootSortColumn Column, WordSortDirection Direction)
{
    public static RootSortSpec Default { get; } = Natural(RootSortColumn.MushafOrder);

    public static RootSortSpec Natural(RootSortColumn column) => new(column, NaturalDirectionOf(column));

    // Counts read most-first, so their natural direction is descending; text and the Mushaf
    // release order read forward.
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
