using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public interface IWordTypesReader
{
    Task<WordTypeTreeDto> GetTreeAsync(CancellationToken cancellationToken);

    Task<PagedResult<WordTypeRowDto>> GetRowsAsync(
        WordTypeFilter filter,
        WordTypeSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<WordTypeTableRowDto>> GetTableRowsAsync(
        WordTypeFilter filter,
        WordTypeTableView tableView,
        WordTypeSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<WordTypeSummaryDto?> GetSummaryAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken);

    Task<PagedResult<WordTypeAyahMatchDto>?> GetAyahMatchesAsync(
        WordTypeRowIdentity identity,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<WordTypeSurahsResponse?> GetSurahsAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken);
}
