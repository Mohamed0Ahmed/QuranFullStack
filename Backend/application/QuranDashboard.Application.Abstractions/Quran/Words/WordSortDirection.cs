namespace QuranDashboard.Application.Abstractions.Quran.Words;

/// <summary>
/// The direction half of a Words explorer list ordering. Always paired with an allowlisted
/// per-explorer sort column inside that explorer's <c>*SortSpec</c> — it is never bound from a
/// query parameter of its own (the wire contract carries ONE opaque <c>sort</c> token).
/// </summary>
public enum WordSortDirection
{
    Ascending,
    Descending,
}
