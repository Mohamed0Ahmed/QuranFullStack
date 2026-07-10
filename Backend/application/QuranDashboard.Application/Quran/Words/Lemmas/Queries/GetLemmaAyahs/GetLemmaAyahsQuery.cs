namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaAyahs;

public sealed record GetLemmaAyahsQuery(int Id, int Page, int PageSize, string? TypeCode = null);
