using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking.PreparedPreflights;

internal static class LinkingPreparedAdditiveContentMerger
{
    public static LinkingPreparedInput Merge(
        LinkingPreparedInput input,
        LinkingConfirmedDoorState state)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(state);

        var contributionsByIdentity = state.Contributions.ToDictionary(
            contribution => contribution.SourceIdentity,
            StringComparer.Ordinal);
        var unitsByIdentity = state.Contributions
            .SelectMany(contribution => contribution.Units)
            .GroupBy(unit => unit.Identity, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var intentsByOrder = input.Intent.Sources.ToDictionary(source => source.OrderValue);
        var mergedRequests = new List<LinkingOperationSourceRequest>(input.Request.Sources.Count);
        var mergedIntents = new List<LinkingOperationSourceIntent>(input.Intent.Sources.Count);

        foreach (var requestSource in input.Request.Sources)
        {
            var intentSource = intentsByOrder[requestSource.OrderValue];
            var hasExistingSource = contributionsByIdentity.ContainsKey(intentSource.SourceIdentity);
            if (hasExistingSource && intentSource.SourceKind != LinkingSourceKind.ManualMushafAyahs)
            {
                mergedRequests.Add(requestSource);
                mergedIntents.Add(intentSource);
                continue;
            }

            var merged = MergeSource(requestSource, intentSource, unitsByIdentity);
            mergedRequests.Add(merged.Request);
            mergedIntents.Add(merged.Intent);
        }

        return new LinkingPreparedInput(
            input.Request with { Sources = mergedRequests },
            input.Intent with { Sources = mergedIntents });
    }

    private static MergedSource MergeSource(
        LinkingOperationSourceRequest request,
        LinkingOperationSourceIntent intent,
        IReadOnlyDictionary<string, LinkingConfirmedUnit> unitsByIdentity)
    {
        if (request.Units.Count != intent.Units.Count)
        {
            throw new InvalidDataException("Prepared source request and intent unit counts differ.");
        }

        var requestUnits = new List<LinkingOperationUnitRequest>(request.Units.Count);
        var intentUnits = new List<LinkingOperationUnitIntent>(intent.Units.Count);
        for (var index = 0; index < intent.Units.Count; index++)
        {
            var requestUnit = request.Units[index];
            var intentUnit = intent.Units[index];
            if (!unitsByIdentity.TryGetValue(intentUnit.Identity, out var existingUnit))
            {
                requestUnits.Add(requestUnit);
                intentUnits.Add(intentUnit);
                continue;
            }

            var existingAyahs = existingUnit.Ayahs.ToDictionary(ayah => ayah.AyahId);
            requestUnits.Add(requestUnit with
            {
                Ayahs = requestUnit.Ayahs
                    .Select(ayah => MergeRequestAyah(ayah, existingAyahs.GetValueOrDefault(ayah.AyahId)))
                    .ToArray(),
            });
            intentUnits.Add(intentUnit with
            {
                Ayahs = intentUnit.Ayahs
                    .Select(ayah => MergeIntentAyah(ayah, existingAyahs.GetValueOrDefault(ayah.AyahId)))
                    .ToArray(),
            });
        }

        return new MergedSource(
            request with { Units = requestUnits },
            intent with { Units = intentUnits });
    }

    private static LinkingOperationAyahRequest MergeRequestAyah(
        LinkingOperationAyahRequest requested,
        LinkingConfirmedAyah? existing) =>
        existing is null
            ? requested
            : requested with
            {
                SelectedWordIds = MergeWords(existing.QuranWordIds, requested.SelectedWordIds),
                Descriptions = MergeDescriptions(existing.Descriptions, requested.Descriptions),
            };

    private static LinkingOperationAyahIntent MergeIntentAyah(
        LinkingOperationAyahIntent requested,
        LinkingConfirmedAyah? existing) =>
        existing is null
            ? requested
            : requested with
            {
                WordIds = MergeWords(existing.QuranWordIds, requested.WordIds),
                Descriptions = MergeDescriptions(existing.Descriptions, requested.Descriptions),
            };

    private static IReadOnlyList<int> MergeWords(
        IEnumerable<int> existing,
        IEnumerable<int> requested) =>
        [.. existing.Concat(requested).Distinct().Order()];

    private static IReadOnlyList<string> MergeDescriptions(
        IEnumerable<string> existing,
        IEnumerable<string> requested) =>
        [.. existing.Concat(requested).Distinct(StringComparer.Ordinal)];

    private sealed record MergedSource(
        LinkingOperationSourceRequest Request,
        LinkingOperationSourceIntent Intent);
}
