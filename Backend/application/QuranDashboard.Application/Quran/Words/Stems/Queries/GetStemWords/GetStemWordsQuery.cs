namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemWords;

public sealed record GetStemWordsQuery(int Id, string? Kind, string? TypeCode, int Page, int PageSize);
