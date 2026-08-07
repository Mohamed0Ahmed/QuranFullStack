namespace QuranDashboard.Application.Abstractions.Access;

public sealed record PermissionCatalogueItem(
    string Code,
    string ArabicLabel,
    string EnglishDescription,
    string GroupKey,
    string GroupLabel,
    int GroupDisplayOrder,
    int DisplayOrder);

public interface IPermissionCatalogueReader
{
    Task<IReadOnlyList<PermissionCatalogueItem>> GetActiveAsync(CancellationToken cancellationToken);
}
