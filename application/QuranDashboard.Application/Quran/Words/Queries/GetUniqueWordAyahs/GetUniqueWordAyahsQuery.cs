namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordAyahs;

/// <summary>
/// Request for paged ayah-match drill-down of a selected unique word.
/// </summary>
/// <param name="Kind"><c>tashkeel</c> or <c>simple</c> route key.</param>
/// <param name="Id">Stable unique-word ID from the route.</param>
/// <param name="Page">1-based ayah page number.</param>
/// <param name="PageSize">Bounded ayah page size (default 20).</param>
public sealed record GetUniqueWordAyahsQuery(string? Kind, int Id, int Page, int PageSize);
