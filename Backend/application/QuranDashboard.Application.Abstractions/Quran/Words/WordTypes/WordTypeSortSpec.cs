namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed class WordTypeSortSpec
{
    private const string Occurrences = "occurrences";
    private const string Ayahs = "ayahs";
    private const string Surahs = "surahs";
    private const string MushafOrder = "mushaf-order";
    private const string Alpha = "alpha";

    private enum ColumnKind
    {
        Occurrences,
        Ayahs,
        Surahs,
        MushafOrder,
        Alpha,
    }

    private WordTypeSortSpec(ColumnKind column, WordSortDirection direction)
    {
        Column = column;
        Direction = direction;
    }

    private ColumnKind Column { get; }
    private WordSortDirection Direction { get; }

    public static WordTypeSortSpec? Create(string? value)
    {
        var token = string.IsNullOrWhiteSpace(value) ? Occurrences : value;
        if (!WordSortToken.TrySplit(token, out var columnToken, out var direction)
            || !TryParseColumn(columnToken, out var column)
            || (column == ColumnKind.MushafOrder && direction is not null))
        {
            return null;
        }

        return new WordTypeSortSpec(column, direction ?? NaturalDirectionOf(column));
    }

    // Counts read most-first (descending natural); text and the Mushaf release order read forward.
    private static WordSortDirection NaturalDirectionOf(ColumnKind column) => column switch
    {
        ColumnKind.MushafOrder => WordSortDirection.Ascending,
        ColumnKind.Alpha => WordSortDirection.Ascending,
        ColumnKind.Occurrences => WordSortDirection.Descending,
        ColumnKind.Ayahs => WordSortDirection.Descending,
        ColumnKind.Surahs => WordSortDirection.Descending,
        _ => throw new InvalidOperationException($"Unhandled {nameof(ColumnKind)} value."),
    };

    // Canonical wire/cache token: bare for the natural direction, suffixed for the opposite one.
    // mushaf-order is ascending-only by contract and never carries a suffix.
    public string CanonicalToken() => Column == ColumnKind.MushafOrder
        ? MushafOrder
        : WordSortToken.Canonical(ColumnKey(Column), Direction, NaturalDirectionOf(Column));

    private static string ColumnKey(ColumnKind column) => column switch
    {
        ColumnKind.Occurrences => Occurrences,
        ColumnKind.Ayahs => Ayahs,
        ColumnKind.Surahs => Surahs,
        ColumnKind.MushafOrder => MushafOrder,
        ColumnKind.Alpha => Alpha,
        _ => throw new InvalidOperationException($"Unhandled {nameof(ColumnKind)} value."),
    };

    private static bool TryParseColumn(string token, out ColumnKind column)
    {
        switch (token)
        {
            case Occurrences:
                column = ColumnKind.Occurrences;
                return true;
            case Ayahs:
                column = ColumnKind.Ayahs;
                return true;
            case Surahs:
                column = ColumnKind.Surahs;
                return true;
            case MushafOrder:
                column = ColumnKind.MushafOrder;
                return true;
            case Alpha:
                column = ColumnKind.Alpha;
                return true;
            default:
                column = default;
                return false;
        }
    }
}
