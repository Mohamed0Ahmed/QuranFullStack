using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Application.Abwab.Tree;

public enum AbwabCompositeReadAccess
{
    Denied = 0,
    CategoryAndSection = 1,
    Full = 2,
}

public static class AbwabCompositeReadRedactor
{
    public static AbwabCompositeReadAccess Classify(IReadOnlySet<string> permissions)
    {
        if (!permissions.Contains(PermissionCatalogue.CategoryView) || !permissions.Contains(PermissionCatalogue.SectionView))
        {
            return AbwabCompositeReadAccess.Denied;
        }

        return permissions.Contains(PermissionCatalogue.ProtectionView)
            ? AbwabCompositeReadAccess.Full
            : AbwabCompositeReadAccess.CategoryAndSection;
    }

    public static AbwabTreeSnapshotDto? Redact(AbwabTreeSnapshotDto snapshot, IReadOnlySet<string> permissions)
    {
        var access = Classify(permissions);
        if (access == AbwabCompositeReadAccess.Denied)
        {
            return null;
        }

        if (access == AbwabCompositeReadAccess.Full)
        {
            return snapshot;
        }

        var redactedCategories = snapshot.Categories.Select(RedactCategory).ToList();
        var redactedById = redactedCategories.ToDictionary(c => c.CategoryId);
        var redactedProjection = snapshot.AllCategoriesProjection.Select(c => redactedById[c.CategoryId]).ToList();

        return snapshot with { Categories = redactedCategories, AllCategoriesProjection = redactedProjection };
    }

    public static CategorySearchResultDto? Redact(CategorySearchResultDto result, IReadOnlySet<string> permissions)
    {
        var access = Classify(permissions);
        if (access == AbwabCompositeReadAccess.Denied)
        {
            return null;
        }

        if (access == AbwabCompositeReadAccess.Full)
        {
            return result;
        }

        return result with { Matches = result.Matches.Select(RedactCategory).ToList() };
    }

    private static CategorySnapshotDto RedactCategory(CategorySnapshotDto category) =>
        category with
        {
            Protection = new CategoryProtectionSummaryDto(
                category.Protection.IsActionBlocked,
                category.Protection.HasEffectiveManualProtection,
                ManualProtections: null),
        };
}
