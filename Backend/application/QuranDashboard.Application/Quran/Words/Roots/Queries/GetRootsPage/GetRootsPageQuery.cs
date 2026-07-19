using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;

// Filter is optional; null keeps pre-feature behavior (the handler coalesces it to RootsCountFilter.None).
public sealed record GetRootsPageQuery(
    string? Search,
    string? Sort,
    int Page,
    int PageSize,
    RootsCountFilter? Filter = null);
