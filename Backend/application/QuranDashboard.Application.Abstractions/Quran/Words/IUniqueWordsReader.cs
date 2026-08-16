using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words;

public interface IUniqueWordsReader
{
    Task<PagedResult<UniqueWordListItemDto>> GetUniqueWordsPageAsync(
        UniqueWordKind kind,
        string? search,
        UniqueWordSortSpec sort,
        UniqueWordsCountFilter filter,
        UniqueWordsAssociationFilter association,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<UniqueWordSummaryDto?> GetUniqueWordSummaryAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken);

    Task<UniqueWordSurahsResponse?> GetMentionedSurahsAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken);

    Task<UniqueWordMissingSurahsResponse?> GetMissingSurahsAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken);

    Task<PagedResult<UniqueWordAyahMatchDto>?> GetAyahMatchesAsync(
        UniqueWordKind kind,
        int id,
        int page,
        int pageSize,
        string? typeCode,
        CancellationToken cancellationToken);
}
