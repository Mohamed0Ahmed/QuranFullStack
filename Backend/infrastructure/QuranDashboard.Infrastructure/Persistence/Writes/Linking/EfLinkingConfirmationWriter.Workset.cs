using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private static ConfirmationWorkset BuildWorkset(
        LinkingOperationRequest request,
        LinkingOperationClassification classification,
        LinkingConfirmedDoorState state)
    {
        var unitsByIdentity = new Dictionary<string, WorksetUnit>(StringComparer.Ordinal);
        var unitsByHash = new Dictionary<string, WorksetUnit>(StringComparer.Ordinal);
        var sources = new List<WorksetSource>();
        var oldAffectedAyahIds = new HashSet<int>();
        var newAffectedAyahIds = new HashSet<int>();
        var contributionsById = state.Contributions.ToDictionary(contribution => contribution.Id);

        for (var sourceIndex = 0; sourceIndex < classification.Sources.Count; sourceIndex++)
        {
            var source = classification.Sources[sourceIndex];
            if (source.Classification is not (
                LinkingPreflightClassification.NewSource or LinkingPreflightClassification.Update))
            {
                continue;
            }

            var orderedUnits = new List<WorksetSourceUnit>(source.Source.Units.Count);
            for (var unitIndex = 0; unitIndex < source.Source.Units.Count; unitIndex++)
            {
                var intent = source.Source.Units[unitIndex];
                if (!unitsByIdentity.TryGetValue(intent.Identity, out var unit))
                {
                    var hash = LinkingUnitIdentity.HashOf(intent.Identity);
                    var hashKey = Convert.ToHexString(hash);

                    if (unitsByHash.TryGetValue(hashKey, out var collision)
                        && !string.Equals(collision.Intent.Identity, intent.Identity, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("A linking unit identity hash collision was detected.");
                    }

                    unit = new WorksetUnit(intent, hash, hashKey);
                    unitsByIdentity.Add(intent.Identity, unit);
                    unitsByHash[hashKey] = unit;
                }

                orderedUnits.Add(new WorksetSourceUnit(unit, unitIndex + 1));
                newAffectedAyahIds.UnionWith(intent.Ayahs.Select(ayah => ayah.AyahId));
            }

            if (source.ExistingContributionId is { } contributionId)
            {
                var existing = contributionsById[contributionId];
                oldAffectedAyahIds.UnionWith(existing.Units
                    .SelectMany(unit => unit.Ayahs)
                    .Select(ayah => ayah.AyahId));
            }

            sources.Add(new WorksetSource(source, request.Sources[sourceIndex], orderedUnits));
        }

        return new ConfirmationWorkset(
            unitsByIdentity.Values.ToList(),
            sources,
            oldAffectedAyahIds,
            newAffectedAyahIds,
            oldAffectedAyahIds.Concat(newAffectedAyahIds).ToHashSet());
    }

    private sealed record ConfirmationWorkset(
        IReadOnlyList<WorksetUnit> Units,
        IReadOnlyList<WorksetSource> Sources,
        IReadOnlySet<int> OldAffectedAyahIds,
        IReadOnlySet<int> NewAffectedAyahIds,
        IReadOnlySet<int> AffectedAyahIds);

    private sealed record WorksetUnit(
        LinkingOperationUnitIntent Intent,
        byte[] IdentityHash,
        string IdentityHashKey);

    private sealed record WorksetSource(
        LinkingSourceClassification Classification,
        LinkingOperationSourceRequest Request,
        IReadOnlyList<WorksetSourceUnit> Units);

    private sealed record WorksetSourceUnit(WorksetUnit Unit, int OrderValue);
}
