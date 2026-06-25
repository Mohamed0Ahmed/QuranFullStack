namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

public enum LemmaSort
{
    MushafOrder,
    Occurrences,
    Alpha,
}

public static class LemmaSortKeys
{
    public const string MushafOrder = "mushaf-order";
    public const string Occurrences = "occurrences";
    public const string Alpha = "alpha";
}

public static class LemmaSortParser
{
    public static bool TryParse(string? value, out LemmaSort sort)
    {
        sort = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case LemmaSortKeys.MushafOrder:
                sort = LemmaSort.MushafOrder;
                return true;
            case LemmaSortKeys.Occurrences:
                sort = LemmaSort.Occurrences;
                return true;
            case LemmaSortKeys.Alpha:
                sort = LemmaSort.Alpha;
                return true;
            default:
                return false;
        }
    }
}
