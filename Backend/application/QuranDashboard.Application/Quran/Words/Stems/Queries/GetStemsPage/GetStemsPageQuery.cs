using QuranDashboard.Application.Abstractions.Quran.Words.Stems;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;

// Filter carries the optional count-range filters (Feature 026, US5); null keeps pre-feature
// behavior (the handler coalesces it to StemsCountFilter.None).
public sealed record GetStemsPageQuery(
    string? Search,
    string? Sort,
    int Page,
    int PageSize,
    StemsCountFilter? Filter = null);
