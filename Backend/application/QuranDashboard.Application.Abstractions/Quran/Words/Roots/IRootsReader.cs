using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots;

public interface IRootsReader
{
    Task<PagedResult<RootListItemDto>> GetRootsPageAsync(
        string? search,
        RootSortSpec sort,
        RootsCountFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<RootSummaryDto?> GetRootSummaryAsync(
        int id,
        CancellationToken cancellationToken);

    Task<PagedResult<RootWordItemDto>?> GetRootWordsAsync(
        int id,
        RootWordKind wordKind,
        string? typeCode,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<RootAyahMatchDto>?> GetRootAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        string? typeCode,
        CancellationToken cancellationToken);

    Task<RootSurahsResponse?> GetRootMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken);

    Task<RootMissingSurahsResponse?> GetRootMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken);

    Task<RootLemmasResponse?> GetRootLemmasAsync(
        int id,
        CancellationToken cancellationToken);

    Task<RootStemsResponse?> GetRootStemsAsync(
        int id,
        CancellationToken cancellationToken);
}
