namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemWords;

public sealed record GetStemWordsQuery(int Id, string? Kind, int Page, int PageSize);
