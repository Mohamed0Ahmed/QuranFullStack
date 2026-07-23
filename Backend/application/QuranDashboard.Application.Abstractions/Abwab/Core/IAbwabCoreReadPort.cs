namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public interface IAbwabCoreReadPort
{
    Task<AbwabTreeSnapshotDto> GetTreeSnapshotAsync(CancellationToken cancellationToken);

    Task<CategorySearchResultDto> SearchCategoriesAsync(string query, CancellationToken cancellationToken);
}
