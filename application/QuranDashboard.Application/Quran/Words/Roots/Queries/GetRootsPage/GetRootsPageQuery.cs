namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;

public sealed record GetRootsPageQuery(
    string? Search,
    string? Sort,
    int Page,
    int PageSize);
