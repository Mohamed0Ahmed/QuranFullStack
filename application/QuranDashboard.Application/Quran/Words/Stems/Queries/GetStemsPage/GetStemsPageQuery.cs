namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;

public sealed record GetStemsPageQuery(
    string? Search,
    string? Sort,
    int Page,
    int PageSize);
