using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abwab.Categories;

// Applies the §9 protection gates for category writers: manual protection always blocks; the ordinary
// 24-hour window additionally gates ONLY direct-content edits and per-selected-category moves (never
// pure reorder, subtree delete, or restore) per the US2 carry-forward — callers choose which check fits
// their action kind.
internal static class CategoryProtectionGate
{
    public static async Task EnsureNotManuallyProtectedAsync(
        ProtectionResolver resolver,
        Guid categoryId,
        ManualProtectionType protectionType,
        CancellationToken cancellationToken)
    {
        var resolution = await resolver.ResolveTypeAsync(categoryId, protectionType, cancellationToken)
            ?? throw new AbwabWriteConflictException(AbwabConflictCodes.CategoryUnavailable, "Category is unavailable.");

        if (resolution.IsProtected)
        {
            throw new AbwabWriteConflictException(AbwabConflictCodes.ManualProtection, "The category is manually protected.");
        }
    }

    public static async Task EnsureOrdinaryEditAllowedAsync(
        ProtectionResolver resolver,
        ISystemOwnerStore owners,
        Guid categoryId,
        ManualProtectionType protectionType,
        string actorSubject,
        CancellationToken cancellationToken)
    {
        var profile = await resolver.ResolveProfileAsync(categoryId, cancellationToken)
            ?? throw new AbwabWriteConflictException(AbwabConflictCodes.CategoryUnavailable, "Category is unavailable.");

        var manual = profile.ManualProtections.Single(m => m.ProtectionType == protectionType);
        if (manual.IsProtected)
        {
            throw new AbwabWriteConflictException(AbwabConflictCodes.ManualProtection, "The category is manually protected.");
        }

        if (!profile.OrdinaryProtection.IsActive)
        {
            return;
        }

        var isLastEditor = string.Equals(profile.OrdinaryProtection.ActorSubject, actorSubject, StringComparison.Ordinal);
        var isSystemOwner = await owners.IsActiveSystemOwnerAsync(actorSubject, cancellationToken);
        if (!isLastEditor && !isSystemOwner)
        {
            throw new AbwabWriteConflictException(AbwabConflictCodes.OrdinaryProtection, "The category is inside its ordinary protection window.");
        }
    }

    public static void StartWindow(Category category, string actorSubject, DateTimeOffset nowUtc)
    {
        category.OrdinaryProtectionActorSubject = actorSubject;
        category.OrdinaryProtectionLastEditedAtUtc = nowUtc;
    }
}
