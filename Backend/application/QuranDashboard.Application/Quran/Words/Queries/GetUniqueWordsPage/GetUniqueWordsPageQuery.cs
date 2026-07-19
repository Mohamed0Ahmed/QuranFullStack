using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

// Filter/Association are optional and default to null so pre-feature callers (and their URLs) behave
// byte-identically; the handler coalesces null to the respective None value.
public sealed record GetUniqueWordsPageQuery(
    string? Kind,
    string? Search,
    string? Sort,
    int Page,
    int PageSize,
    UniqueWordsCountFilter? Filter = null,
    UniqueWordsAssociationFilter? Association = null);
