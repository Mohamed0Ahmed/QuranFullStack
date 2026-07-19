using QuranDashboard.Application.Abstractions.Quran.Words.Stems;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;

// Filter/Association are optional; null keeps pre-feature behavior (the handler coalesces each to its
// None value).
public sealed record GetStemsPageQuery(
    string? Search,
    string? Sort,
    int Page,
    int PageSize,
    StemsCountFilter? Filter = null,
    StemsAssociationFilter? Association = null);
