namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSurahs;

/// <summary>
/// Request for mentioned-surahs drill-down of a selected unique word.
/// </summary>
/// <param name="Kind"><c>tashkeel</c> or <c>simple</c> route key.</param>
/// <param name="Id">Stable unique-word ID from the route.</param>
public sealed record GetUniqueWordSurahsQuery(string? Kind, int Id);
