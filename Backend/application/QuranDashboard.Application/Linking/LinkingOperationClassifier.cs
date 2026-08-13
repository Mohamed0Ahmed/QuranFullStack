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
        var doorWordImpacts = DoorWordImpactsOf(intent, confirmedAyahs);
        var sources = intent.Sources
            .Select(source => ClassifySource(
                WithDoorState(source, intent, state),
                state,
                confirmedAyahs,
                doorWordImpacts))
            .ToList();
        var totals = TotalsOf(intent, confirmedAyahs);
        var isBlocked = sources.Any(source => source.Classification == LinkingPreflightClassification.Invalid);

        var hasSourceChanges = sources.Any(source =>
            source.Classification is LinkingPreflightClassification.NewSource
                or LinkingPreflightClassification.Update);

        return new LinkingOperationClassification(
            !isBlocked && !hasSourceChanges && totals.New == 0 && totals.Updated == 0,
            isBlocked,
            totals,
            sources);
    }

    private static LinkingOperationSourceIntent WithDoorState(
        LinkingOperationSourceIntent source,
        LinkingOperationIntent intent,
        LinkingConfirmedDoorState state) =>
        intent.IsDoorArchived || state.IsArchived
            ? source with { InvalidReason = LinkingPreflightInvalidReason.DoorArchived }
            : source;

    private static LinkingSourceClassification ClassifySource(
        LinkingOperationSourceIntent source,
        LinkingConfirmedDoorState state,
        IReadOnlyDictionary<int, LinkingConfirmedDoorAyah> confirmedAyahs,
        IReadOnlyDictionary<int, LinkingDoorWordImpact> doorWordImpacts)
    {
        var live = state.Contributions.FirstOrDefault(contribution =>
            string.Equals(contribution.SourceIdentity, source.SourceIdentity, StringComparison.Ordinal));
        var ayahs = SubmittedAyahsOf(source)
            .Select(ayah => ClassifyAyah(source, ayah, state, live, confirmedAyahs, doorWordImpacts))
            .ToList();
        var counts = CountsOf(ayahs.Count, ayahs);

        return new LinkingSourceClassification(
            source,
            ClassifySourceState(source, live, counts),
            live?.Id,
            live?.Version,
            counts,
            ayahs);
    }

    private static LinkingAyahClassification ClassifyAyah(
        LinkingOperationSourceIntent source,
        LinkingOperationAyahIntent ayah,
        LinkingConfirmedDoorState state,
        LinkingConfirmedContribution? live,
        IReadOnlyDictionary<int, LinkingConfirmedDoorAyah> confirmedAyahs,
        IReadOnlyDictionary<int, LinkingDoorWordImpact> doorWordImpacts)
    {
        var confirmed = confirmedAyahs.GetValueOrDefault(ayah.AyahId);
        var wordChanges = WordChangesOf(confirmed?.QuranWordIds ?? [], ayah.WordIds);
        var classification = source.InvalidReason is not null || ayah.InvalidReason is not null
            ? LinkingPreflightClassification.Invalid
            : confirmed is null
                ? LinkingPreflightClassification.NewAyah
                : wordChanges.Added.Count > 0
                    ? LinkingPreflightClassification.Update
                    : LinkingPreflightClassification.Unchanged;

        return new LinkingAyahClassification(
            ayah.AyahId,
            ayah.VerseKey,
            ayah.SurahNumber,
            ayah.AyahNumber,
            classification,
            OverlappingSourcesOf(state, live, ayah.AyahId),
            wordChanges,
            doorWordImpacts.GetValueOrDefault(ayah.AyahId, new LinkingDoorWordImpact([], [], [])),
            NoDescriptionChanges,
            source.InvalidReason ?? ayah.InvalidReason);
    }

    private static LinkingPreflightClassification ClassifySourceState(
        LinkingOperationSourceIntent source,
        LinkingConfirmedContribution? live,
        LinkingClassificationCounts counts)
    {
        if (source.InvalidReason is not null || counts.Invalid > 0)
        {
            return LinkingPreflightClassification.Invalid;
        }

        if (live is null)
        {
            return LinkingPreflightClassification.NewSource;
        }

        return ContributionChanged(source, live)
            ? LinkingPreflightClassification.Update
            : LinkingPreflightClassification.Unchanged;
    }

    private static bool ContributionChanged(
        LinkingOperationSourceIntent source,
        LinkingConfirmedContribution live)
    {
        if (source.ContributionMode != live.ContributionMode
            || source.OrderValue != live.OrderValue
            || !string.Equals(source.Label, live.Label, StringComparison.Ordinal)
            || source.Units.Count != live.Units.Count)
        {
            return true;
        }

        for (var unitIndex = 0; unitIndex < source.Units.Count; unitIndex++)
        {
            var submittedUnit = source.Units[unitIndex];
            var confirmedUnit = live.Units[unitIndex];
            var grouped = source.ContributionMode == LinkingContributionMode.ManualGrouped;

            if (confirmedUnit.OrderValue != unitIndex + 1
                || confirmedUnit.IsGrouped != grouped
                || submittedUnit.Ayahs.Count != confirmedUnit.Ayahs.Count)
            {
                return true;
            }

            for (var ayahIndex = 0; ayahIndex < submittedUnit.Ayahs.Count; ayahIndex++)
            {
                var submittedAyah = submittedUnit.Ayahs[ayahIndex];
                var confirmedAyah = confirmedUnit.Ayahs[ayahIndex];

                if (confirmedAyah.OrderValue != ayahIndex + 1
                    || submittedAyah.AyahId != confirmedAyah.AyahId
                    || !submittedAyah.WordIds.SequenceEqual(confirmedAyah.QuranWordIds)
                    || !submittedAyah.Descriptions.SequenceEqual(confirmedAyah.Descriptions))
                {
                    return true;
                }
            }
        }

        return false;
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

    private static IReadOnlyList<LinkingOperationAyahIntent> SubmittedAyahsOf(
        LinkingOperationSourceIntent source) =>
        [.. source.Units.SelectMany(unit => unit.Ayahs)];

    private static LinkingWordChanges WordChangesOf(
        IReadOnlyList<int> confirmed,
        IReadOnlyList<int> submitted)
    {
        var confirmedWords = confirmed.ToHashSet();
        var submittedWords = submitted.ToHashSet();

        return new LinkingWordChanges(
            [.. submittedWords.Except(confirmedWords).Order()],
            [],
            [.. submittedWords.Intersect(confirmedWords).Order()]);
    }

    private static IReadOnlyDictionary<int, LinkingDoorWordImpact> DoorWordImpactsOf(
        LinkingOperationIntent intent,
        IReadOnlyDictionary<int, LinkingConfirmedDoorAyah> confirmedAyahs)
    {
        var submittedWords = intent.Sources
            .SelectMany(source => source.Units)
            .SelectMany(unit => unit.Ayahs)
            .GroupBy(ayah => ayah.AyahId)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(ayah => ayah.WordIds).ToHashSet());
        var ayahIds = confirmedAyahs.Keys.Concat(submittedWords.Keys).Distinct();

        return ayahIds.ToDictionary(
            ayahId => ayahId,
            ayahId =>
            {
                var existing = confirmedAyahs.GetValueOrDefault(ayahId)?.QuranWordIds.ToHashSet() ?? [];
                var submitted = submittedWords.GetValueOrDefault(ayahId, []);

                return new LinkingDoorWordImpact(
                    [.. submitted.Except(existing).Order()],
                    [.. existing.Order()],
                    []);
            });
    }

    private static LinkingClassificationCounts TotalsOf(
        LinkingOperationIntent intent,
        IReadOnlyDictionary<int, LinkingConfirmedDoorAyah> confirmedAyahs)
    {
        var ayahs = intent.Sources
            .SelectMany(source => source.Units.SelectMany(unit => unit.Ayahs)
                .Select(ayah => new SubmittedDoorAyah(source.InvalidReason, ayah)))
            .GroupBy(entry => entry.Ayah.AyahId)
            .Select(group =>
            {
                var confirmed = confirmedAyahs.GetValueOrDefault(group.Key);
                var invalid = group.Any(entry => entry.SourceInvalidReason is not null || entry.Ayah.InvalidReason is not null);
                var submittedWords = group.SelectMany(entry => entry.Ayah.WordIds).Distinct().ToList();

                return invalid
                    ? LinkingPreflightClassification.Invalid
                    : confirmed is null
                        ? LinkingPreflightClassification.NewAyah
                        : submittedWords.Except(confirmed.QuranWordIds).Any()
                            ? LinkingPreflightClassification.Update
                            : LinkingPreflightClassification.Unchanged;
            })
            .ToList();

        return CountsOf(ayahs.Count, ayahs);
    }

    private static LinkingClassificationCounts CountsOf(
        int requested,
        IReadOnlyList<LinkingAyahClassification> ayahs) =>
        CountsOf(requested, ayahs.Select(ayah => ayah.Classification).ToList());

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

    private sealed record SubmittedDoorAyah(
        LinkingPreflightInvalidReason? SourceInvalidReason,
        LinkingOperationAyahIntent Ayah);
}
