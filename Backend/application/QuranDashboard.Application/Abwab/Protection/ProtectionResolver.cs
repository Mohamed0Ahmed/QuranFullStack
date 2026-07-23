using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abwab.Protection;

public sealed class ProtectionResolver(IManualProtectionReadPort readPort, IServerClock clock)
{
    private static readonly TimeSpan OrdinaryProtectionWindow = TimeSpan.FromHours(24);

    public async Task<CategoryProtectionProfileDto?> ResolveProfileAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var context = await readPort.GetProtectionContextAsync(categoryId, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var serverTimeUtc = clock.UtcNow;
        var ordinary = ResolveOrdinary(context, serverTimeUtc);

        var manualResolutions = Enum.GetValues<ManualProtectionType>()
            .Select(type => ResolveManual(context, type, ordinary, serverTimeUtc))
            .ToList();

        return new CategoryProtectionProfileDto(
            ExpectedTimelineGeneration.Of(context.TimelineGeneration),
            categoryId,
            manualResolutions,
            ordinary,
            serverTimeUtc);
    }

    public async Task<ManualProtectionResolutionDto?> ResolveTypeAsync(
        Guid categoryId,
        ManualProtectionType protectionType,
        CancellationToken cancellationToken)
    {
        var context = await readPort.GetProtectionContextAsync(categoryId, cancellationToken);
        if (context is null)
        {
            return null;
        }

        var serverTimeUtc = clock.UtcNow;
        var ordinary = ResolveOrdinary(context, serverTimeUtc);
        return ResolveManual(context, protectionType, ordinary, serverTimeUtc);
    }

    private static OrdinaryProtectionResolutionDto ResolveOrdinary(
        CategoryProtectionContextDto context,
        DateTimeOffset serverTimeUtc)
    {
        if (context.OrdinaryProtectionLastEditedAtUtc is not { } lastEditedAtUtc)
        {
            return new OrdinaryProtectionResolutionDto(false, null, null, null);
        }

        var expiresAtUtc = lastEditedAtUtc + OrdinaryProtectionWindow;
        return new OrdinaryProtectionResolutionDto(
            expiresAtUtc > serverTimeUtc,
            context.OrdinaryProtectionActorSubject,
            lastEditedAtUtc,
            expiresAtUtc);
    }

    private static ManualProtectionResolutionDto ResolveManual(
        CategoryProtectionContextDto context,
        ManualProtectionType protectionType,
        OrdinaryProtectionResolutionDto ordinary,
        DateTimeOffset serverTimeUtc)
    {
        var direct = context.ActiveProtections.FirstOrDefault(
            p => p.CategoryId == context.CategoryId && p.ProtectionType == protectionType);

        if (direct is not null)
        {
            return new ManualProtectionResolutionDto(
                protectionType,
                IsProtected: true,
                IsDirect: true,
                context.CategoryId,
                direct.ProtectionScope,
                ProtectionActionClassification.ManuallyProtected,
                serverTimeUtc);
        }

        // AncestorIds is root-to-parent; walk from the end so the NEAREST ancestor's Subtree grant wins.
        for (var i = context.AncestorIds.Count - 1; i >= 0; i--)
        {
            var ancestorId = context.AncestorIds[i];
            var inherited = context.ActiveProtections.FirstOrDefault(p =>
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
                    serverTimeUtc);
            }
        }

        var classification = ordinary.IsActive
            ? ProtectionActionClassification.OrdinaryWindowActive
            : ProtectionActionClassification.Unprotected;

        return new ManualProtectionResolutionDto(
            protectionType,
            IsProtected: false,
            IsDirect: false,
            SourceCategoryId: null,
            Scope: null,
            classification,
            serverTimeUtc);
    }
}
