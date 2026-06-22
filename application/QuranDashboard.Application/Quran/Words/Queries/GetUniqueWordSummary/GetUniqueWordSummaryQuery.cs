namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSummary;

/// <summary>
/// Request for the summary of a selected unique word. Used to restore modal
/// state from a shared URL before or alongside a drill-down read.
/// </summary>
/// <param name="Kind"><c>tashkeel</c> or <c>simple</c> route key.</param>
/// <param name="Id">Stable unique-word ID from the route.</param>
public sealed record GetUniqueWordSummaryQuery(string? Kind, int Id);
