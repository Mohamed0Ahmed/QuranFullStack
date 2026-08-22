namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaWords;

public sealed record GetLemmaWordsQuery(int Id, string? Kind, string? TypeCode, int Page, int PageSize);
