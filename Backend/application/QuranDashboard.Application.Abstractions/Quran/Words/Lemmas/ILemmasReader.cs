using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

/// <summary>
/// Read-only Lemmas Explorer (Feature 016) data contract. Implemented by EF Core
/// projections and wrapped by a bounded cache decorator. <c>null</c> from a
/// selected-resource read means the positive identity does not exist; empty
/// detail collections for an existing identity are successful non-null responses.
/// All queries are <c>AsNoTracking</c> and perform no tracked mutation.
/// </summary>
public interface ILemmasReader
{
    Task<PagedResult<LemmaListItemDto>> GetLemmasPageAsync(
        string? search,
        LemmaSort sort,
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
