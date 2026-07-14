using QuranDashboard.Application.Abstractions.Quran.Words.Stems;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;

// Filter carries the optional count-range filters (Feature 026, US5) and Association the optional
// primary-root/primary-lemma filters (Feature 026, US7); null keeps pre-feature behavior (the handler
// coalesces them to the respective None value).
public sealed record GetStemsPageQuery(
    string? Search,
    string? Sort,
    int Page,
    int PageSize,
    StemsCountFilter? Filter = null,
    StemsAssociationFilter? Association = null);
