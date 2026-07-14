using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

// Filter carries the optional count-range filters (Feature 026, US5). It defaults to null so
// pre-feature callers (and their URLs) behave byte-identically; the handler coalesces null to
// UniqueWordsCountFilter.None.
public sealed record GetUniqueWordsPageQuery(
    string? Kind,
    string? Search,
    string? Sort,
    int Page,
    int PageSize,
    UniqueWordsCountFilter? Filter = null);
