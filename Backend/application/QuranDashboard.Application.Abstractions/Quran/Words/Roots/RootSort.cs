namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots;

public enum RootSort
{
    MushafOrder,
    Occurrences,
    Alpha,
}

public static class RootSortKeys
{
    public const string MushafOrder = "mushaf-order";
    public const string Occurrences = "occurrences";
    public const string Alpha = "alpha";
}

public static class RootSortParser
{
    public static bool TryParse(string? value, out RootSort sort)
    {
        sort = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case RootSortKeys.MushafOrder:
                sort = RootSort.MushafOrder;
                return true;
            case RootSortKeys.Occurrences:
                sort = RootSort.Occurrences;
                return true;
            case RootSortKeys.Alpha:
                sort = RootSort.Alpha;
                return true;
            default:
                return false;
        }
    }
}
