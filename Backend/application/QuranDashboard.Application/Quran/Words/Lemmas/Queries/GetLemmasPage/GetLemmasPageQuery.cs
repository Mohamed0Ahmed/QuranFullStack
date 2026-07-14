using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;

// Filter carries the optional count-range filters (Feature 026, US5); null keeps pre-feature
// behavior (the handler coalesces it to LemmasCountFilter.None).
public sealed record GetLemmasPageQuery(
    string? Search,
    string? Sort,
    int Page,
    int PageSize,
    LemmasCountFilter? Filter = null);
