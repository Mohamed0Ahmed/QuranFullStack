namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaWords;

public sealed record GetLemmaWordsQuery(int Id, string? Kind, int Page, int PageSize);
