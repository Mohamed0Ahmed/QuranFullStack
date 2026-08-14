using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking;

public static class LinkingOperationClassifier
{
    private static readonly LinkingDescriptionChanges NoDescriptionChanges = new([], [], [], []);
    private static readonly LinkingDoorWordImpact NoDoorWordImpact = new([], [], []);

    public static LinkingOperationClassification Classify(
        LinkingOperationIntent intent,
        LinkingConfirmedDoorState state)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(state);

        var index = ClassificationIndex.Create(intent, state);
        var results = intent.Sources
            .Select(source => ClassifySource(
                WithDoorState(source, intent, state),
                index))
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
        ClassificationIndex classificationIndex)
    {
        var live = classificationIndex.ContributionsBySourceIdentity.GetValueOrDefault(source.SourceIdentity);
        var unitResults = source.Units
            .Select((unit, unitIndex) => ClassifyUnit(
                source,
                unit,
                live,
                unitIndex,
                classificationIndex))
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
        ClassificationIndex index)
    {
        var liveUnit = live is not null && unitIndex < live.Units.Count
            ? index.UnitsById[live.Units[unitIndex].Id]
            : null;
        var exactUnit = index.UnitsByIdentity.GetValueOrDefault(unit.Identity);
        var invalidReason = source.InvalidReason
            ?? unit.Ayahs.Select(ayah => ayah.InvalidReason).FirstOrDefault(reason => reason is not null);
        var classification = invalidReason is not null
            ? LinkingPreflightClassification.Invalid
            : liveUnit is not null
                ? string.Equals(liveUnit.Unit.Identity, unit.Identity, StringComparison.Ordinal)
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
                live,
                index))
            .ToList();

        return new UnitClassificationResult(unit.Identity, classification, ayahs);
    }

    private static LinkingAyahClassification ClassifyAyah(
        LinkingOperationSourceIntent source,
        LinkingOperationAyahIntent ayah,
        LinkingPreflightClassification unitClassification,
        ConfirmedUnitIndex? comparisonUnit,
        LinkingConfirmedContribution? live,
        ClassificationIndex index)
    {
        var confirmedUnitAyah = comparisonUnit?.AyahsById.GetValueOrDefault(ayah.AyahId);
        var confirmedWords = confirmedUnitAyah?.QuranWordIds ?? [];

        return new LinkingAyahClassification(
            ayah.AyahId,
            ayah.VerseKey,
            ayah.SurahNumber,
            ayah.AyahNumber,
            unitClassification,
            OverlappingSourcesOf(index, live, ayah.AyahId),
            WordChangesOf(confirmedWords, ayah.WordIds),
            index.DoorWordImpacts.GetValueOrDefault(ayah.AyahId, NoDoorWordImpact),
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
        ClassificationIndex index,
        LinkingConfirmedContribution? live,
        int ayahId) =>
        index.ContributionsByAyahId.TryGetValue(ayahId, out var contributions)
            ? [
                .. contributions
                    .Where(contribution => contribution.Id != live?.Id)
                    .Select(contribution => new LinkingOverlappingSource(
                        contribution.SourceIdentity,
                        contribution.Label,
                        LinkingSourceTokens.ToToken(contribution.SourceKind)))
            ]
            : [];

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

    private static LinkingClassificationCounts TotalsOf(
        IReadOnlyList<SourceClassificationResult> sources)
    {
        var byIdentity = new Dictionary<string, LinkingPreflightClassification>(StringComparer.Ordinal);

        foreach (var unit in sources.SelectMany(source => source.Units))
        {
            if (!byIdentity.TryGetValue(unit.Identity, out var current)
                || PriorityOf(unit.Classification) > PriorityOf(current))
            {
                byIdentity[unit.Identity] = unit.Classification;
            }
        }

        var classifications = byIdentity.Values.ToList();

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

    private sealed record ConfirmedUnitIndex(
        LinkingConfirmedUnit Unit,
        IReadOnlyDictionary<int, LinkingConfirmedAyah> AyahsById);

    private sealed record ClassificationIndex(
        IReadOnlyDictionary<string, LinkingConfirmedContribution> ContributionsBySourceIdentity,
        IReadOnlyDictionary<int, IReadOnlyList<LinkingConfirmedContribution>> ContributionsByAyahId,
        IReadOnlyDictionary<long, ConfirmedUnitIndex> UnitsById,
        IReadOnlyDictionary<string, ConfirmedUnitIndex> UnitsByIdentity,
        IReadOnlyDictionary<int, LinkingDoorWordImpact> DoorWordImpacts)
    {
        public static ClassificationIndex Create(
            LinkingOperationIntent intent,
            LinkingConfirmedDoorState state)
        {
            var contributionsBySourceIdentity = state.Contributions.ToDictionary(
                contribution => contribution.SourceIdentity,
                StringComparer.Ordinal);
            var contributionsByAyahId = BuildContributionsByAyahId(state.Contributions);
            var unitIndexes = state.Contributions
                .SelectMany(contribution => contribution.Units)
                .GroupBy(unit => unit.Id)
                .Select(group => group.First())
                .Select(unit => new ConfirmedUnitIndex(
                    unit,
                    unit.Ayahs.ToDictionary(ayah => ayah.AyahId)))
                .ToList();
            var unitsById = unitIndexes.ToDictionary(unit => unit.Unit.Id);
            var unitsByIdentity = unitIndexes
                .GroupBy(unit => unit.Unit.Identity, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            return new ClassificationIndex(
                contributionsBySourceIdentity,
                contributionsByAyahId,
                unitsById,
                unitsByIdentity,
                BuildDoorWordImpacts(intent, state, contributionsBySourceIdentity));
        }

        private static IReadOnlyDictionary<int, IReadOnlyList<LinkingConfirmedContribution>>
            BuildContributionsByAyahId(IReadOnlyList<LinkingConfirmedContribution> contributions)
        {
            var byAyahId = new Dictionary<int, List<LinkingConfirmedContribution>>();

            foreach (var contribution in contributions)
            {
                foreach (var ayahId in contribution.Units
                             .SelectMany(unit => unit.Ayahs)
                             .Select(ayah => ayah.AyahId)
                             .Distinct())
                {
                    if (!byAyahId.TryGetValue(ayahId, out var matches))
                    {
                        matches = [];
                        byAyahId.Add(ayahId, matches);
                    }

                    matches.Add(contribution);
                }
            }

            return byAyahId.ToDictionary(
                entry => entry.Key,
                entry => (IReadOnlyList<LinkingConfirmedContribution>)entry.Value);
        }

        private static IReadOnlyDictionary<int, LinkingDoorWordImpact> BuildDoorWordImpacts(
            LinkingOperationIntent intent,
            LinkingConfirmedDoorState state,
            IReadOnlyDictionary<string, LinkingConfirmedContribution> contributionsBySourceIdentity)
        {
            var replacedContributionIds = intent.Sources
                .Select(source => contributionsBySourceIdentity.GetValueOrDefault(source.SourceIdentity)?.Id)
                .Where(id => id is not null)
                .Select(id => id!.Value)
                .ToHashSet();
            var projectedWordsByAyahId = new Dictionary<int, HashSet<int>>();

            foreach (var ayah in state.Contributions
                         .Where(contribution => !replacedContributionIds.Contains(contribution.Id))
                         .SelectMany(contribution => contribution.Units)
                         .SelectMany(unit => unit.Ayahs))
            {
                AddWords(projectedWordsByAyahId, ayah.AyahId, ayah.QuranWordIds);
            }

            foreach (var ayah in intent.Sources
                         .SelectMany(source => source.Units)
                         .SelectMany(unit => unit.Ayahs))
            {
                AddWords(projectedWordsByAyahId, ayah.AyahId, ayah.WordIds);
            }

            var confirmedWordsByAyahId = state.Ayahs.ToDictionary(
                ayah => ayah.AyahId,
                ayah => ayah.QuranWordIds.ToHashSet());
            var impacts = new Dictionary<int, LinkingDoorWordImpact>();

            foreach (var ayahId in confirmedWordsByAyahId.Keys.Concat(projectedWordsByAyahId.Keys).Distinct())
            {
                var existing = confirmedWordsByAyahId.GetValueOrDefault(ayahId, []);
                var projected = projectedWordsByAyahId.GetValueOrDefault(ayahId, []);
                impacts.Add(
                    ayahId,
                    new LinkingDoorWordImpact(
                        [.. projected.Except(existing).Order()],
                        [.. projected.Intersect(existing).Order()],
                        [.. existing.Except(projected).Order()]));
            }

            return impacts;
        }

        private static void AddWords(
            IDictionary<int, HashSet<int>> wordsByAyahId,
            int ayahId,
            IEnumerable<int> wordIds)
        {
            if (!wordsByAyahId.TryGetValue(ayahId, out var words))
            {
                words = [];
                wordsByAyahId.Add(ayahId, words);
            }

            words.UnionWith(wordIds);
        }
    }
}
