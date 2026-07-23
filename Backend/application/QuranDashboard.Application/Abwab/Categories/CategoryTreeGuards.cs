using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;

namespace QuranDashboard.Application.Abwab.Categories;

// Revision/version guards live in the domain-neutral AbwabRevisionGuards (shared with Sections);
// only the category-specific name-uniqueness guard belongs here.
internal static class CategoryTreeGuards
{
    public static async Task GuardNameConflictAsync(
        ICategoryTreeStore categories,
        string normalizedName,
        Guid? parentCategoryId,
        Guid? excludeCategoryId,
        CancellationToken cancellationToken)
    {
        if (await categories.ActiveNormalizedNameExistsAsync(normalizedName, parentCategoryId, excludeCategoryId, cancellationToken))
        {
            throw new AbwabWriteConflictException(AbwabConflictCodes.CategoryNameConflict, "A category with this name already exists in this scope.");
        }
    }
}
