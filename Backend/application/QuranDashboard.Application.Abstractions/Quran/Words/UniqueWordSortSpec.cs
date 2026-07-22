namespace QuranDashboard.Application.Abstractions.Quran.Words;

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

public readonly record struct UniqueWordSortSpec(UniqueWordSortColumn Column, WordSortDirection Direction)
{
    public static UniqueWordSortSpec Default { get; } = Natural(UniqueWordSortColumn.MushafOrder);

    public static UniqueWordSortSpec Natural(UniqueWordSortColumn column) => new(column, NaturalDirectionOf(column));

    public static WordSortDirection NaturalDirectionOf(UniqueWordSortColumn column) => column switch
    {
        UniqueWordSortColumn.MushafOrder => WordSortDirection.Ascending,
        UniqueWordSortColumn.Alpha => WordSortDirection.Ascending,
        UniqueWordSortColumn.Occurrences => WordSortDirection.Descending,
        UniqueWordSortColumn.Ayahs => WordSortDirection.Descending,
        UniqueWordSortColumn.Surahs => WordSortDirection.Descending,
        _ => throw new InvalidOperationException($"Unhandled {nameof(UniqueWordSortColumn)} value."),
    };

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
    public static bool TryParse(string? value, out UniqueWordSortSpec spec)
    {
        spec = default;

        if (!WordSortToken.TrySplit(value, out var columnToken, out var direction)
            || !TryParseColumn(columnToken, out var column))
        {
            return false;
        }

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
