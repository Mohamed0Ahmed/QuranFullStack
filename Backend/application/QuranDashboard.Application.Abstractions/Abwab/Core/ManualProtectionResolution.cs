using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

// Shared direct/inherited resolution rule (§7.2): the category's own active record wins; otherwise the
// NEAREST ancestor with an active Subtree-scoped record of the same type wins. Reused by ProtectionResolver
// (single-category, read-port-backed, Application) and the batch composite-read projector (in-memory,
// tree-snapshot-backed, Infrastructure) so the inheritance rule is defined exactly once, in the one layer
// both can reach.
public static class ManualProtectionResolution
{
    public static ManualProtectionResolutionDto Resolve(
        Guid categoryId,
        IReadOnlyList<Guid> ancestorIds,
        IEnumerable<ActiveManualProtectionRecordDto> activeProtections,
        ManualProtectionType protectionType,
        bool ordinaryWindowActive,
        DateTimeOffset serverTimeUtc)
    {
        var protections = activeProtections as IReadOnlyCollection<ActiveManualProtectionRecordDto> ?? activeProtections.ToList();

        var direct = protections.FirstOrDefault(p => p.CategoryId == categoryId && p.ProtectionType == protectionType);
        if (direct is not null)
        {
            return new ManualProtectionResolutionDto(
                protectionType,
                IsProtected: true,
                IsDirect: true,
                categoryId,
                direct.ProtectionScope,
                ProtectionActionClassification.ManuallyProtected,
                serverTimeUtc,
                direct.ManualProtectionId,
                direct.Version);
        }

        for (var i = ancestorIds.Count - 1; i >= 0; i--)
        {
            var ancestorId = ancestorIds[i];
            var inherited = protections.FirstOrDefault(p =>
                p.CategoryId == ancestorId &&
                p.ProtectionType == protectionType &&
                p.ProtectionScope == ManualProtectionScope.Subtree);

            if (inherited is not null)
            {
                return new ManualProtectionResolutionDto(
                    protectionType,
                    IsProtected: true,
                    IsDirect: false,
                    ancestorId,
                    inherited.ProtectionScope,
                    ProtectionActionClassification.ManuallyProtected,
                    serverTimeUtc,
                    ManualProtectionId: null,
                    Version: null);
            }
        }

        var classification = ordinaryWindowActive
            ? ProtectionActionClassification.OrdinaryWindowActive
            : ProtectionActionClassification.Unprotected;

        return new ManualProtectionResolutionDto(protectionType, IsProtected: false, IsDirect: false, null, null, classification, serverTimeUtc, null, null);
    }
}
