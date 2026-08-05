using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class PermissionCatalogueSynchronizer(
    QuranDashboardDbContext db) : IPermissionCatalogueSynchronizer
{
    public async Task<PermissionCatalogueSyncResult> SynchronizeAsync(CancellationToken cancellationToken)
    {
        var existing = await db.AccessPermissions.ToListAsync(cancellationToken);
        var duplicateCode = existing
            .GroupBy(permission => permission.Code, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateCode is not null)
        {
            throw new InvalidOperationException(
                $"The permissions table contains duplicate code '{duplicateCode.Key}'.");
        }

        var existingByCode = existing.ToDictionary(permission => permission.Code, StringComparer.Ordinal);
        var addedCodes = new List<string>();
        var updatedCodes = new List<string>();

        foreach (var definition in AbwabPermissionCatalogue.All)
        {
            if (!existingByCode.TryGetValue(definition.Code, out var permission))
            {
                db.AccessPermissions.Add(new Permission(
                    definition.Code,
                    definition.ArabicLabel,
                    definition.EnglishDescription,
                    definition.DisplayOrder));
                addedCodes.Add(definition.Code);
                continue;
            }

            if (permission.ArabicLabel != definition.ArabicLabel
                || permission.EnglishDescription != definition.EnglishDescription
                || permission.DisplayOrder != definition.DisplayOrder)
            {
                permission.UpdateMetadata(
                    definition.ArabicLabel,
                    definition.EnglishDescription,
                    definition.DisplayOrder);
                updatedCodes.Add(definition.Code);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var knownCodes = AbwabPermissionCatalogue.All
            .Select(permission => permission.Code)
            .ToHashSet(StringComparer.Ordinal);
        var unknownCodes = existing
            .Where(permission => !knownCodes.Contains(permission.Code))
            .Select(permission => permission.Code)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        return new PermissionCatalogueSyncResult(addedCodes, updatedCodes, unknownCodes);
    }
}
