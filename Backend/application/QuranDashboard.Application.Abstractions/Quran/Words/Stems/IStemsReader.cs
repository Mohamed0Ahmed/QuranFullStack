using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems;

// null from a selected-resource read means the identity does not exist; an empty detail collection
// for an existing identity is a successful non-null response.
public interface IStemsReader
{
    Task<PagedResult<StemListItemDto>> GetStemsPageAsync(
        string? search,
        StemSortSpec sort,
        StemsCountFilter filter,
        StemsAssociationFilter association,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<StemSummaryDto?> GetStemSummaryAsync(
        int id,
        CancellationToken cancellationToken);

    Task<PagedResult<StemWordItemDto>?> GetStemWordsAsync(
        int id,
        StemWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<StemAyahMatchDto>?> GetStemAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        string? typeCode,
        CancellationToken cancellationToken);

    Task<StemSurahsResponse?> GetStemMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken);

    Task<StemMissingSurahsResponse?> GetStemMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken);

    Task<StemLemmasResponse?> GetStemLemmasAsync(
        int id,
        CancellationToken cancellationToken);
}
