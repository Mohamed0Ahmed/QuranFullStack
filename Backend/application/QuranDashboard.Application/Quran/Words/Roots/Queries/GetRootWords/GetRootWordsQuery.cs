namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootWords;

public sealed record GetRootWordsQuery(int Id, string? Kind, string? TypeCode, int Page, int PageSize);
