namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordMissingSurahs;

/// <summary>
/// Request for missing-surahs drill-down of a selected unique word.
/// </summary>
/// <param name="Kind"><c>tashkeel</c> or <c>simple</c> route key.</param>
/// <param name="Id">Stable unique-word ID from the route.</param>
public sealed record GetUniqueWordMissingSurahsQuery(string? Kind, int Id);
