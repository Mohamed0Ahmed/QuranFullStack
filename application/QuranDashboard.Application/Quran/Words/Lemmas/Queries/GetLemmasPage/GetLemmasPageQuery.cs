namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;

public sealed record GetLemmasPageQuery(
    string? Search,
    string? Sort,
    int Page,
    int PageSize);
