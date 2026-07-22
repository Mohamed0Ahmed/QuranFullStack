using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

public sealed record GetUniqueWordsPageQuery(
    string? Kind,
    string? Search,
    string? Sort,
    int Page,
    int PageSize,
    UniqueWordsCountFilter? Filter = null,
    UniqueWordsAssociationFilter? Association = null);
