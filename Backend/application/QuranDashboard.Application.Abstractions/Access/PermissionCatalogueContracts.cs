namespace QuranDashboard.Application.Abstractions.Access;

public sealed record PermissionCatalogueItem(
    string Code,
    string ArabicLabel,
    string EnglishDescription,
    string GroupKey,
    string GroupLabel,
    int GroupDisplayOrder,
    int DisplayOrder);

public sealed record PermissionCatalogueResponse(
    IReadOnlyList<PermissionCatalogueItem> Items,
    bool AssignmentReady);

public interface IPermissionCatalogueReader
{
    Task<PermissionCatalogueResponse> GetActiveAsync(CancellationToken cancellationToken);
}
