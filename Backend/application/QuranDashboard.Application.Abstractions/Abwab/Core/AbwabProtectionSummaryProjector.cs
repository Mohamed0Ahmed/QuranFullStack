using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

// Batch equivalent of ProtectionResolver for the tree/search composite read: given all active
// ManualProtection rows (one query, grouped by owning category) it resolves all five types plus the
// ordinary window per category with no N+1. Always builds the FULL summary — permission-based
// redaction is a separate later projection (Application.Abwab.Tree.AbwabCompositeReadRedactor, T060),
// never done here.
public static class AbwabProtectionSummaryProjector
{
    private static readonly TimeSpan OrdinaryProtectionWindow = TimeSpan.FromHours(24);

    public static CategoryProtectionSummaryDto Build(
        Category category,
        ILookup<Guid, ManualProtection> protectionsByCategory,
        DateTimeOffset serverTimeUtc)
    {
        var ordinaryActive = category.OrdinaryProtectionLastEditedAtUtc is { } lastEditedAtUtc &&
            lastEditedAtUtc + OrdinaryProtectionWindow > serverTimeUtc;

        var candidateIds = new List<Guid>(category.AncestorIds) { category.CategoryId };
        var activeProtections = candidateIds
            .SelectMany(id => protectionsByCategory[id])
            .Select(p => new ActiveManualProtectionRecordDto(p.CategoryId, p.ProtectionType, p.ProtectionScope))
            .ToList();

        var resolutions = Enum.GetValues<ManualProtectionType>()
            .Select(type => ManualProtectionResolution.Resolve(
                category.CategoryId, category.AncestorIds, activeProtections, type, ordinaryActive, serverTimeUtc))
            .ToList();

        var hasManualProtection = resolutions.Any(r => r.IsProtected);
        var isActionBlocked = hasManualProtection || ordinaryActive;

        return new CategoryProtectionSummaryDto(isActionBlocked, hasManualProtection, resolutions);
    }
}
