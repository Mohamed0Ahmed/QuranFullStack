using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abwab.Relationships;

// §7.3 endpoint-protection gate. The caller supplies the whole resolved target set — proposed
// endpoints on add, current ∪ proposed on edit, stored endpoints on delete/restore — and applicable
// direct OR inherited Relationship protection on ANY target blocks the ENTIRE mutation. It runs
// before any row is touched, which is what stops an edit escaping protection by replacing a
// protected endpoint with an unprotected one.
internal static class RelationshipProtectionGate
{
    public static async Task EnsureNoProtectedTargetAsync(
        ProtectionResolver resolver,
        IEnumerable<Guid> targetCategoryIds,
        CancellationToken cancellationToken)
    {
        foreach (var categoryId in targetCategoryIds.Distinct().OrderBy(id => id))
        {
            var resolution = await resolver.ResolveTypeAsync(categoryId, ManualProtectionType.Relationship, cancellationToken)
                ?? throw new AbwabWriteConflictException(AbwabConflictCodes.CategoryUnavailable, "Category is unavailable.");

            if (resolution.IsProtected)
            {
                throw new AbwabWriteConflictException(
                    AbwabConflictCodes.ManualProtection, "An endpoint category is manually protected against relationship changes.");
            }
        }
    }
}
