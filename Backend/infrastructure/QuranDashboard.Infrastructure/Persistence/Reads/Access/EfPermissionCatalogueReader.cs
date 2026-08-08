using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Access;

public sealed class EfPermissionCatalogueReader(QuranDashboardDbContext db) : IPermissionCatalogueReader
{
    public async Task<PermissionCatalogueResponse> GetActiveAsync(CancellationToken cancellationToken)
    {
        var persisted = await db.AccessPermissions
            .AsNoTracking()
            .Select(permission => new { permission.Code, permission.RetiredAtUtc })
            .ToArrayAsync(cancellationToken);
        var retiredCodes = persisted
            .Where(permission => permission.RetiredAtUtc is not null)
            .Select(permission => permission.Code)
            .ToHashSet(StringComparer.Ordinal);
        var assignableCodes = persisted
            .Where(permission => permission.RetiredAtUtc is null)
            .Select(permission => permission.Code)
            .ToHashSet(StringComparer.Ordinal);
        var offered = AbwabPermissionCatalogue.All
            .Where(definition => !retiredCodes.Contains(definition.Code))
            .ToArray();

        return new PermissionCatalogueResponse(
            offered
                .Select(definition => new PermissionCatalogueItem(
                    definition.Code,
                    definition.ArabicLabel,
                    definition.EnglishDescription,
                    GroupKey(definition.Code),
                    definition.Group,
                    definition.GroupDisplayOrder,
                    definition.DisplayOrder))
                .ToArray(),
            offered.All(definition => assignableCodes.Contains(definition.Code)));
    }

    private static string GroupKey(string permissionCode) => permissionCode.Split('.')[1];
}
