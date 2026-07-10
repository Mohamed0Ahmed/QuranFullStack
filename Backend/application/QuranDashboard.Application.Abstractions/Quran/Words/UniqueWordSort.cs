namespace QuranDashboard.Application.Abstractions.Quran.Words;

public enum UniqueWordSort
{
    MushafOrder,
    Occurrences,
    Alpha,
}

public static class UniqueWordSortKeys
{
    public const string MushafOrder = "mushaf-order";
    public const string Occurrences = "occurrences";
    public const string Alpha = "alpha";
}

public static class UniqueWordSortParser
{

    public static bool TryParse(string? value, out UniqueWordSort sort)
    {
        sort = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case UniqueWordSortKeys.MushafOrder:
                sort = UniqueWordSort.MushafOrder;
                return true;
            case UniqueWordSortKeys.Occurrences:
                sort = UniqueWordSort.Occurrences;
                return true;
            case UniqueWordSortKeys.Alpha:
                sort = UniqueWordSort.Alpha;
                return true;
            default:
                return false;
        }
    }
}
