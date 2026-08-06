using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Access;

public sealed class EfPermissionCatalogueReader(QuranDashboardDbContext db) : IPermissionCatalogueReader
{
    public async Task<IReadOnlyList<PermissionCatalogueItem>> GetActiveAsync(CancellationToken cancellationToken)
    {
        var persisted = await db.AccessPermissions
            .AsNoTracking()
            .Where(permission => permission.RetiredAtUtc == null)
            .ToDictionaryAsync(permission => permission.Code, StringComparer.Ordinal, cancellationToken);
        var definitions = AbwabPermissionCatalogue.All;
        if (persisted.Count != definitions.Count || definitions.Any(definition =>
                !persisted.TryGetValue(definition.Code, out var permission)
                || permission.ArabicLabel != definition.ArabicLabel
                || permission.EnglishDescription != definition.EnglishDescription
                || permission.DisplayOrder != definition.DisplayOrder))
        {
            throw new InvalidOperationException("The active permission catalogue does not match the Backend definition.");
        }

        return definitions
            .Select(definition => new PermissionCatalogueItem(
                definition.Code,
                definition.ArabicLabel,
                definition.EnglishDescription,
                GroupKey(definition.Code),
                definition.Group,
                definition.GroupDisplayOrder,
                definition.DisplayOrder))
            .ToArray();
    }

    private static string GroupKey(string permissionCode) => permissionCode.Split('.')[1];
}
