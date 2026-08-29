namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public enum PhraseSimilaritySort
{
    Strength,
    Connections,
    MushafOrder,
}

public static class PhraseSimilaritySortKeys
{
    public const string Strength = "strength";
    public const string Connections = "connections";
    public const string MushafOrder = "mushaf-order";
}

public static class PhraseSimilaritySortContract
{
    public static bool TryParse(string? value, out PhraseSimilaritySort sort)
    {
        switch (value)
        {
            case PhraseSimilaritySortKeys.Strength:
                sort = PhraseSimilaritySort.Strength;
                return true;
            case PhraseSimilaritySortKeys.Connections:
                sort = PhraseSimilaritySort.Connections;
                return true;
            case PhraseSimilaritySortKeys.MushafOrder:
                sort = PhraseSimilaritySort.MushafOrder;
                return true;
            default:
                sort = default;
                return false;
        }
    }

    public static string CanonicalKey(PhraseSimilaritySort sort) => sort switch
    {
        PhraseSimilaritySort.Strength => PhraseSimilaritySortKeys.Strength,
        PhraseSimilaritySort.Connections => PhraseSimilaritySortKeys.Connections,
        PhraseSimilaritySort.MushafOrder => PhraseSimilaritySortKeys.MushafOrder,
        _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseSimilaritySort)} value: {sort}."),
    };
}
