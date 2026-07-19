namespace QuranDashboard.Application.Abstractions.Quran.Words;

// Always paired with a column inside a *SortSpec; never bound from its own query parameter
// (the wire contract carries ONE opaque sort token).
public enum WordSortDirection
{
    Ascending,
    Descending,
}
