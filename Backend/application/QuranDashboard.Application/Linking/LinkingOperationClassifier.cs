using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking;

public static class LinkingOperationClassifier
{
    private static readonly LinkingDescriptionChanges NoDescriptionChanges = new([], [], [], []);

    public static LinkingOperationClassification Classify(
        LinkingOperationIntent intent,
        LinkingConfirmedDoorState state)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(state);

        var confirmedAyahs = state.Ayahs.ToDictionary(ayah => ayah.AyahId);
        var confirmedUnits = state.Contributions
            .SelectMany(contribution => contribution.Units)
            .GroupBy(unit => unit.Identity, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var doorWordImpacts = DoorWordImpactsOf(intent, state);
        var results = intent.Sources
            .Select(source => ClassifySource(
                WithDoorState(source, intent, state),
                state,
                confirmedUnits,
                confirmedAyahs,
                doorWordImpacts))
            .ToList();
        var totals = TotalsOf(results);
        var isBlocked = results.Any(result =>
            result.Classification.Classification == LinkingPreflightClassification.Invalid);

        return new LinkingOperationClassification(
            !isBlocked
                && totals.New == 0
                && totals.Updated == 0
                && results.All(result =>
                    result.Classification.Classification == LinkingPreflightClassification.Unchanged),
            isBlocked,
            totals,
            results.Select(result => result.Classification).ToList());
    }

    private static LinkingOperationSourceIntent WithDoorState(
        LinkingOperationSourceIntent source,
        LinkingOperationIntent intent,
        LinkingConfirmedDoorState state) =>
        intent.IsDoorArchived || state.IsArchived
            ? source with { InvalidReason = LinkingPreflightInvalidReason.DoorArchived }
            : source;

    private static SourceClassificationResult ClassifySource(
        LinkingOperationSourceIntent source,
        LinkingConfirmedDoorState state,
        IReadOnlyDictionary<string, LinkingConfirmedUnit> confirmedUnits,
        IReadOnlyDictionary<int, LinkingConfirmedDoorAyah> confirmedAyahs,
        IReadOnlyDictionary<int, LinkingDoorWordImpact> doorWordImpacts)
    {
        var live = state.Contributions.FirstOrDefault(contribution =>
            string.Equals(contribution.SourceIdentity, source.SourceIdentity, StringComparison.Ordinal));
        var unitResults = source.Units
            .Select((unit, index) => ClassifyUnit(
                source,
                unit,
                live,
                index,
                confirmedUnits,
                state,
                doorWordImpacts))
            .ToList();
        var unitClassifications = unitResults.Select(result => result.Classification).ToList();
        var counts = CountsOf(unitClassifications.Count, unitClassifications);
        var sourceClassification = ClassifySourceState(source, live, unitResults);

        return new SourceClassificationResult(
            new LinkingSourceClassification(
                source,
                sourceClassification,
                live?.Id,
                live?.Version,
                counts,
                unitResults.SelectMany(result => result.Ayahs).ToList()),
            unitResults);
    }

    private static UnitClassificationResult ClassifyUnit(
        LinkingOperationSourceIntent source,
        LinkingOperationUnitIntent unit,
        LinkingConfirmedContribution? live,
        int unitIndex,
        IReadOnlyDictionary<string, LinkingConfirmedUnit> confirmedUnits,
        LinkingConfirmedDoorState state,
        IReadOnlyDictionary<int, LinkingDoorWordImpact> doorWordImpacts)
    {
        var liveUnit = live?.Units.ElementAtOrDefault(unitIndex);
        var exactUnit = confirmedUnits.GetValueOrDefault(unit.Identity);
        var invalidReason = source.InvalidReason
            ?? unit.Ayahs.Select(ayah => ayah.InvalidReason).FirstOrDefault(reason => reason is not null);
        var classification = invalidReason is not null
            ? LinkingPreflightClassification.Invalid
            : liveUnit is not null
                ? string.Equals(liveUnit.Identity, unit.Identity, StringComparison.Ordinal)
                    ? LinkingPreflightClassification.Unchanged
                    : LinkingPreflightClassification.Update
                : live is not null
                    ? LinkingPreflightClassification.Update
                    : exactUnit is not null
                        ? LinkingPreflightClassification.Unchanged
                        : LinkingPreflightClassification.NewAyah;
        var comparisonUnit = liveUnit ?? exactUnit;
        var ayahs = unit.Ayahs
            .Select(ayah => ClassifyAyah(
                source,
                ayah,
                classification,
                comparisonUnit,
                state,
                live,
                doorWordImpacts))
            .ToList();

        return new UnitClassificationResult(unit.Identity, classification, ayahs);
    }

    private static LinkingAyahClassification ClassifyAyah(
        LinkingOperationSourceIntent source,
        LinkingOperationAyahIntent ayah,
        LinkingPreflightClassification unitClassification,
        LinkingConfirmedUnit? comparisonUnit,
        LinkingConfirmedDoorState state,
        LinkingConfirmedContribution? live,
        IReadOnlyDictionary<int, LinkingDoorWordImpact> doorWordImpacts)
    {
        var confirmedUnitAyah = comparisonUnit?.Ayahs.FirstOrDefault(candidate => candidate.AyahId == ayah.AyahId);
        var confirmedWords = confirmedUnitAyah?.QuranWordIds ?? [];

        return new LinkingAyahClassification(
            ayah.AyahId,
            ayah.VerseKey,
            ayah.SurahNumber,
            ayah.AyahNumber,
            unitClassification,
            OverlappingSourcesOf(state, live, ayah.AyahId),
            WordChangesOf(confirmedWords, ayah.WordIds),
            doorWordImpacts.GetValueOrDefault(ayah.AyahId, new LinkingDoorWordImpact([], [], [])),
            NoDescriptionChanges,
            source.InvalidReason ?? ayah.InvalidReason);
    }

    private static LinkingPreflightClassification ClassifySourceState(
        LinkingOperationSourceIntent source,
        LinkingConfirmedContribution? live,
        IReadOnlyList<UnitClassificationResult> units)
    {
        if (source.InvalidReason is not null
            || units.Any(unit => unit.Classification == LinkingPreflightClassification.Invalid))
        {
            return LinkingPreflightClassification.Invalid;
        }

        if (live is null)
        {
            return units.All(unit => unit.Classification == LinkingPreflightClassification.Unchanged)
                ? LinkingPreflightClassification.Unchanged
                : LinkingPreflightClassification.NewSource;
        }

        return live.Units.Count != units.Count
            || units.Any(unit => unit.Classification != LinkingPreflightClassification.Unchanged)
            ? LinkingPreflightClassification.Update
            : LinkingPreflightClassification.Unchanged;
    }

    private static IReadOnlyList<LinkingOverlappingSource> OverlappingSourcesOf(
        LinkingConfirmedDoorState state,
        LinkingConfirmedContribution? live,
        int ayahId) =>
    [
        .. state.Contributions
            .Where(contribution => contribution.Id != live?.Id)
            .Where(contribution => contribution.Units
                .SelectMany(unit => unit.Ayahs)
                .Any(ayah => ayah.AyahId == ayahId))
            .Select(contribution => new LinkingOverlappingSource(
                contribution.SourceIdentity,
                contribution.Label,
                LinkingSourceTokens.ToToken(contribution.SourceKind)))
    ];

    private static LinkingWordChanges WordChangesOf(
        IReadOnlyList<int> confirmed,
        IReadOnlyList<int> submitted)
    {
        var confirmedWords = confirmed.ToHashSet();
        var submittedWords = submitted.ToHashSet();

        return new LinkingWordChanges(
            [.. submittedWords.Except(confirmedWords).Order()],
            [.. confirmedWords.Except(submittedWords).Order()],
            [.. submittedWords.Intersect(confirmedWords).Order()]);
    }

    private static IReadOnlyDictionary<int, LinkingDoorWordImpact> DoorWordImpactsOf(
        LinkingOperationIntent intent,
        LinkingConfirmedDoorState state)
    {
        var replacedContributionIds = intent.Sources
            .Select(source => state.Contributions.FirstOrDefault(contribution =>
                string.Equals(contribution.SourceIdentity, source.SourceIdentity, StringComparison.Ordinal))?.Id)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToHashSet();
        var projectedAyahs = state.Contributions
            .Where(contribution => !replacedContributionIds.Contains(contribution.Id))
            .SelectMany(contribution => contribution.Units)
            .SelectMany(unit => unit.Ayahs.Select(ayah => new ProjectedAyah(ayah.AyahId, ayah.QuranWordIds)))
            .Concat(intent.Sources
            .SelectMany(source => source.Units)
            .SelectMany(unit => unit.Ayahs)
            .Select(ayah => new ProjectedAyah(ayah.AyahId, ayah.WordIds)))
            .GroupBy(ayah => ayah.AyahId)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(ayah => ayah.WordIds).ToHashSet());
        var confirmedAyahs = state.Ayahs.ToDictionary(ayah => ayah.AyahId);
        var ayahIds = confirmedAyahs.Keys.Concat(projectedAyahs.Keys).Distinct();

        return ayahIds.ToDictionary(
            ayahId => ayahId,
            ayahId =>
            {
                var existing = confirmedAyahs.GetValueOrDefault(ayahId)?.QuranWordIds.ToHashSet() ?? [];
                var projected = projectedAyahs.GetValueOrDefault(ayahId, []);

                return new LinkingDoorWordImpact(
                    [.. projected.Except(existing).Order()],
                    [.. projected.Intersect(existing).Order()],
                    [.. existing.Except(projected).Order()]);
            });
    }

    private static LinkingClassificationCounts TotalsOf(
        IReadOnlyList<SourceClassificationResult> sources)
    {
        var classifications = sources
            .SelectMany(source => source.Units)
            .GroupBy(unit => unit.Identity, StringComparer.Ordinal)
            .Select(group => group
                .Select(unit => unit.Classification)
                .OrderByDescending(PriorityOf)
                .First())
            .ToList();

        return CountsOf(classifications.Count, classifications);
    }

    private static int PriorityOf(LinkingPreflightClassification classification) =>
        classification switch
        {
            LinkingPreflightClassification.Invalid => 4,
            LinkingPreflightClassification.Update => 3,
            LinkingPreflightClassification.NewAyah => 2,
            _ => 1,
        };

    private static LinkingClassificationCounts CountsOf(
        int requested,
        IReadOnlyList<LinkingPreflightClassification> classifications) =>
        new(
            requested,
            classifications.Count(value => value == LinkingPreflightClassification.NewAyah),
            0,
            classifications.Count(value => value == LinkingPreflightClassification.Unchanged),
            classifications.Count(value => value == LinkingPreflightClassification.Update),
            0,
            classifications.Count(value => value == LinkingPreflightClassification.Invalid));

    private sealed record SourceClassificationResult(
        LinkingSourceClassification Classification,
        IReadOnlyList<UnitClassificationResult> Units);

    private sealed record UnitClassificationResult(
        string Identity,
        LinkingPreflightClassification Classification,
        IReadOnlyList<LinkingAyahClassification> Ayahs);

    private sealed record ProjectedAyah(int AyahId, IReadOnlyList<int> WordIds);
}
