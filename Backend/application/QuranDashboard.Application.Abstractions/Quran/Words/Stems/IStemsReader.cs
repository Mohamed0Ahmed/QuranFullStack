using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems;

/// <summary>
/// Read-only Stems Explorer (Feature 016) data contract. Implemented by EF Core
/// projections and wrapped by a bounded cache decorator. <c>null</c> from a
/// selected-resource read means the positive identity does not exist; empty
/// detail collections for an existing identity are successful non-null responses.
/// All queries are <c>AsNoTracking</c> and perform no tracked mutation.
/// </summary>
public interface IStemsReader
{
    Task<PagedResult<StemListItemDto>> GetStemsPageAsync(
        string? search,
        StemSort sort,
        StemsCountFilter filter,
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
