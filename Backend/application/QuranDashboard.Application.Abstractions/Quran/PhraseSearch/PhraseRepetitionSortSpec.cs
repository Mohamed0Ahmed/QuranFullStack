namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public enum PhraseRepetitionSort
{
    OccurrencesDescending,
    OccurrencesAscending,
    MushafOrder,
}

public static class PhraseRepetitionSortKeys
{
    public const string Occurrences = "occurrences";
    public const string OccurrencesAscending = "occurrences-asc";
    public const string MushafOrder = "mushaf-order";
}

public static class PhraseRepetitionSortContract
{
    public static bool TryParse(string? value, out PhraseRepetitionSort sort)
    {
        switch (value)
        {
            case PhraseRepetitionSortKeys.Occurrences:
                sort = PhraseRepetitionSort.OccurrencesDescending;
                return true;
            case PhraseRepetitionSortKeys.OccurrencesAscending:
                sort = PhraseRepetitionSort.OccurrencesAscending;
                return true;
            case PhraseRepetitionSortKeys.MushafOrder:
                sort = PhraseRepetitionSort.MushafOrder;
                return true;
            default:
                sort = default;
                return false;
        }
    }

    public static string CanonicalKey(PhraseRepetitionSort sort) => sort switch
    {
        PhraseRepetitionSort.OccurrencesDescending => PhraseRepetitionSortKeys.Occurrences,
        PhraseRepetitionSort.OccurrencesAscending => PhraseRepetitionSortKeys.OccurrencesAscending,
        PhraseRepetitionSort.MushafOrder => PhraseRepetitionSortKeys.MushafOrder,
        _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseRepetitionSort)} value: {sort}."),
    };
}
