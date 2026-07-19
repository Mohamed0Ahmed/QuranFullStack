using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;

// Filter/Association are optional; null keeps pre-feature behavior (the handler coalesces each to its
// None value).
public sealed record GetLemmasPageQuery(
    string? Search,
    string? Sort,
    int Page,
    int PageSize,
    LemmasCountFilter? Filter = null,
    LemmasAssociationFilter? Association = null);
