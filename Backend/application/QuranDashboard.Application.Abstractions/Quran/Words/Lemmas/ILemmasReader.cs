using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

// null from a selected-resource read means the identity does not exist; an empty detail collection
// for an existing identity is a successful non-null response.
public interface ILemmasReader
{
    Task<PagedResult<LemmaListItemDto>> GetLemmasPageAsync(
        string? search,
        LemmaSortSpec sort,
        LemmasCountFilter filter,
        LemmasAssociationFilter association,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<LemmaSummaryDto?> GetLemmaSummaryAsync(
        int id,
        CancellationToken cancellationToken);

    Task<PagedResult<LemmaWordItemDto>?> GetLemmaWordsAsync(
        int id,
        LemmaWordKind wordKind,
        string? typeCode,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<LemmaAyahMatchDto>?> GetLemmaAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        string? typeCode,
        CancellationToken cancellationToken);

    Task<LemmaSurahsResponse?> GetLemmaMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken);

    Task<LemmaMissingSurahsResponse?> GetLemmaMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken);

    Task<LemmaStemsResponse?> GetLemmaStemsAsync(
        int id,
        CancellationToken cancellationToken);
}
