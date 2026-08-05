namespace QuranDashboard.Application.Abstractions.Security.Permissions;

public interface IPermissionCatalogueSynchronizer
{
    Task<PermissionCatalogueSyncResult> SynchronizeAsync(CancellationToken cancellationToken);
}

public sealed record PermissionCatalogueSyncResult(
    IReadOnlyList<string> AddedCodes,
    IReadOnlyList<string> UpdatedCodes,
    IReadOnlyList<string> UnknownDatabaseCodes);
