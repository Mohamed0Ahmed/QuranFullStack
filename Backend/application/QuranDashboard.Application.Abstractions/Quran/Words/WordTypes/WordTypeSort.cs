namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public enum WordTypeSort
{
    Occurrences,
    Ayahs,
    Surahs,
    MushafOrder,
    Alpha,
}

public static class WordTypeSortKeys
{
    public const string Occurrences = "occurrences";
    public const string Ayahs = "ayahs";
    public const string Surahs = "surahs";
    public const string MushafOrder = "mushaf-order";
    public const string Alpha = "alpha";
}

public static class WordTypeSortParser
{
    public static bool TryParse(string? value, out WordTypeSort sort)
    {
        sort = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case WordTypeSortKeys.Occurrences:
                sort = WordTypeSort.Occurrences;
                return true;
            case WordTypeSortKeys.Ayahs:
                sort = WordTypeSort.Ayahs;
                return true;
            case WordTypeSortKeys.Surahs:
                sort = WordTypeSort.Surahs;
                return true;
            case WordTypeSortKeys.MushafOrder:
                sort = WordTypeSort.MushafOrder;
                return true;
            case WordTypeSortKeys.Alpha:
                sort = WordTypeSort.Alpha;
                return true;
            default:
                return false;
        }
    }
}
