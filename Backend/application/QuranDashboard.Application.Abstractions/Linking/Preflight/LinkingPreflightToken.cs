namespace QuranDashboard.Application.Abstractions.Linking.Preflight;

public static class LinkingPreflightToken
{
    public static IReadOnlyList<LinkingPreflightContributionComponent> AffectedContributionsOf(
        LinkingConfirmedDoorState state,
        LinkingOperationClassification classification)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(classification);

        var replaced = classification.Sources
            .Where(source => source.ExistingContributionId is not null)
            .Select(source => source.ExistingContributionId!.Value)
            .ToHashSet();

        var overlapped = classification.Sources
            .SelectMany(source => source.Ayahs)
            .SelectMany(ayah => ayah.OverlappingSources)
            .Select(source => source.SourceIdentity)
            .ToHashSet(StringComparer.Ordinal);

        return
        [
            .. state.Contributions
                .Where(contribution =>
                    replaced.Contains(contribution.Id) || overlapped.Contains(contribution.SourceIdentity))
                .Select(contribution => new LinkingPreflightContributionComponent(
                    contribution.Id, contribution.Version))
        ];
    }

}

public sealed record LinkingPreflightDoorComponent(int DoorId, uint Version);

public sealed record LinkingPreflightContributionComponent(long Id, uint Version);
