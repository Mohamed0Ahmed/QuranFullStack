using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;

public sealed record GetRootsPageQuery(
    string? Search,
    string? Sort,
    int Page,
    int PageSize,
    RootsCountFilter? Filter = null);
