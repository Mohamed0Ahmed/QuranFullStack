namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootWords;

public sealed record GetRootWordsQuery(int Id, string? Kind, int Page, int PageSize);
