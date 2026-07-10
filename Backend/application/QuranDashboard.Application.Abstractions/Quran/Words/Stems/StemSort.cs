namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems;

public enum StemSort
{
    MushafOrder,
    Occurrences,
    Alpha,
}

public static class StemSortKeys
{
    public const string MushafOrder = "mushaf-order";
    public const string Occurrences = "occurrences";
    public const string Alpha = "alpha";
}

public static class StemSortParser
{
    public static bool TryParse(string? value, out StemSort sort)
    {
        sort = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case StemSortKeys.MushafOrder:
                sort = StemSort.MushafOrder;
                return true;
            case StemSortKeys.Occurrences:
                sort = StemSort.Occurrences;
                return true;
            case StemSortKeys.Alpha:
                sort = StemSort.Alpha;
                return true;
            default:
                return false;
        }
    }
}
