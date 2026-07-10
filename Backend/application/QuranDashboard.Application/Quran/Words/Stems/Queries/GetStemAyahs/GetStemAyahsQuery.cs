namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemAyahs;

public sealed record GetStemAyahsQuery(int Id, int Page, int PageSize, string? TypeCode);
