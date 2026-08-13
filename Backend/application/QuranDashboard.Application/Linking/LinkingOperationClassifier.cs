using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Application.Linking;

public static class LinkingOperationClassifier
{
    public static LinkingOperationClassification Classify(
        LinkingOperationIntent intent,
        LinkingConfirmedDoorState state)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(state);

        var doorWordImpacts = DoorWordImpactsOf(intent, state);
        var sources = intent.Sources
            .Select(source => ClassifySource(
                WithDoorState(source, intent, state),
                state,
                doorWordImpacts))
            .ToList();

        return new LinkingOperationClassification(
            sources.Count > 0 && sources.All(source => source.Classification == LinkingPreflightClassification.Unchanged),
            sources.Any(source => source.Classification == LinkingPreflightClassification.Invalid),
            Total(sources.Select(source => source.Counts)),
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
        IReadOnlyDictionary<int, LinkingDoorWordImpact> doorWordImpacts)
    {
        var live = state.Contributions.FirstOrDefault(contribution =>
            string.Equals(contribution.SourceIdentity, source.SourceIdentity, StringComparison.Ordinal));

        var confirmedAyahs = ConfirmedAyahsOf(live);
        var submitted = SubmittedAyahsOf(source);
        var configurationChanged = ConfigurationChanged(source, live);
        var ayahs = new List<LinkingAyahClassification>(submitted.Count);

        foreach (var entry in submitted)
        {
            var ayah = entry.Ayah;
            var overlapping = OverlappingSourcesOf(state, live, ayah.AyahId);
            var confirmed = confirmedAyahs.GetValueOrDefault(ayah.AyahId);
            var wordChanges = WordChangesOf(confirmed?.Ayah.QuranWordIds ?? [], ayah.WordIds);
            var descriptionChanges = DescriptionChangesOf(
                confirmed?.Ayah.Descriptions ?? [], ayah.Descriptions);

            ayahs.Add(new LinkingAyahClassification(
                ayah.AyahId,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber,
                ClassifyAyah(
                    source, entry, confirmed, configurationChanged, overlapping, wordChanges, descriptionChanges),
                overlapping,
                wordChanges,
                DoorWordImpactOf(doorWordImpacts, ayah.AyahId),
                descriptionChanges,
                source.InvalidReason ?? ayah.InvalidReason));
        }

        var submittedAyahIds = submitted.Select(entry => entry.Ayah.AyahId).ToHashSet();

        foreach (var removed in confirmedAyahs.Values
            .Where(entry => !submittedAyahIds.Contains(entry.Ayah.AyahId))
            .OrderBy(entry => entry.Ayah.AyahId))
        {
            ayahs.Add(new LinkingAyahClassification(
                removed.Ayah.AyahId,
                removed.Ayah.VerseKey,
                removed.Ayah.SurahNumber,
                removed.Ayah.AyahNumber,
                LinkingPreflightClassification.Remove,
                OverlappingSourcesOf(state, live, removed.Ayah.AyahId),
                new LinkingWordChanges([], [.. removed.Ayah.QuranWordIds.Order()], []),
                DoorWordImpactOf(doorWordImpacts, removed.Ayah.AyahId),
                new LinkingDescriptionChanges([], [.. removed.Ayah.Descriptions], [], []),
                null));
        }

        var counts = CountsOf(submitted.Count, ayahs);

        return new LinkingSourceClassification(
            source,
            ClassifySourceState(source, live, configurationChanged, counts),
            live?.Id,
            live?.Version,
            counts,
            ayahs);
    }

    private static bool ConfigurationChanged(
        LinkingOperationSourceIntent source,
        LinkingConfirmedContribution? live) =>
        live is not null
        && live.ContributionMode != source.ContributionMode;

    private static LinkingPreflightClassification ClassifyAyah(
        LinkingOperationSourceIntent source,
        SubmittedAyahEntry entry,
        ConfirmedAyahEntry? confirmed,
        bool configurationChanged,
        IReadOnlyList<LinkingOverlappingSource> overlapping,
        LinkingWordChanges wordChanges,
        LinkingDescriptionChanges descriptionChanges)
    {
        if (source.InvalidReason is not null || entry.Ayah.InvalidReason is not null)
        {
            return LinkingPreflightClassification.Invalid;
        }

        if (confirmed is null)
        {
            return overlapping.Count > 0
                ? LinkingPreflightClassification.OverlapOtherSource
                : LinkingPreflightClassification.NewAyah;
        }

        var unchanged = !configurationChanged
            && wordChanges.Added.Count == 0
            && wordChanges.Removed.Count == 0
            && descriptionChanges.Added.Count == 0
            && descriptionChanges.Removed.Count == 0
            && descriptionChanges.Changed.Count == 0
            && entry.GroupedWith.SequenceEqual(confirmed.GroupedWith)
            && entry.UnitRank == confirmed.UnitRank;

        return unchanged ? LinkingPreflightClassification.Unchanged : LinkingPreflightClassification.Update;
    }

    private static LinkingPreflightClassification ClassifySourceState(
        LinkingOperationSourceIntent source,
        LinkingConfirmedContribution? live,
        bool configurationChanged,
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

        var unchanged = !configurationChanged
            && counts.Removed == 0
            && counts.New == 0
            && counts.Overlapping == 0
            && counts.Updated == 0;

        return unchanged ? LinkingPreflightClassification.Unchanged : LinkingPreflightClassification.Update;
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

    private static Dictionary<int, ConfirmedAyahEntry> ConfirmedAyahsOf(LinkingConfirmedContribution? live)
    {
        var confirmed = new Dictionary<int, ConfirmedAyahEntry>();

        if (live is null)
        {
            return confirmed;
        }

        var units = live.Units.OrderBy(unit => unit.OrderValue).ToList();

        for (var unitRank = 0; unitRank < units.Count; unitRank++)
        {
            var unit = units[unitRank];
            var groupedWith = unit.Ayahs
                .OrderBy(ayah => ayah.OrderValue)
                .Select(ayah => ayah.AyahId)
                .ToList();

            foreach (var ayah in unit.Ayahs)
            {
                confirmed[ayah.AyahId] = new ConfirmedAyahEntry(ayah, groupedWith, unitRank + 1);
            }
        }

        return confirmed;
    }

    private static List<SubmittedAyahEntry> SubmittedAyahsOf(LinkingOperationSourceIntent source)
    {
        var submitted = new List<SubmittedAyahEntry>();

        for (var unitIndex = 0; unitIndex < source.Units.Count; unitIndex++)
        {
            var unit = source.Units[unitIndex];
            var groupedWith = unit.Ayahs.Select(ayah => ayah.AyahId).ToList();

            submitted.AddRange(unit.Ayahs.Select(ayah => new SubmittedAyahEntry(
                ayah, groupedWith, unitIndex + 1)));
        }

        return submitted;
    }

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
        var before = WordsByAyah(state.Contributions);
        var affectedSourceIdentities = intent.Sources
            .Select(source => source.SourceIdentity)
            .ToHashSet(StringComparer.Ordinal);
        var after = WordsByAyah(state.Contributions.Where(contribution =>
            !affectedSourceIdentities.Contains(contribution.SourceIdentity)));

        foreach (var source in intent.Sources)
        {
            foreach (var ayah in source.Units.SelectMany(unit => unit.Ayahs))
            {
                AddWords(after, ayah.AyahId, ayah.WordIds);
            }
        }

        var ayahIds = before.Keys.Concat(after.Keys).Distinct();
        return ayahIds.ToDictionary(
            ayahId => ayahId,
            ayahId =>
            {
                var existingWords = before.GetValueOrDefault(ayahId, []);
                var finalWords = after.GetValueOrDefault(ayahId, []);
                return new LinkingDoorWordImpact(
                    [.. finalWords.Except(existingWords).Order()],
                    [.. finalWords.Intersect(existingWords).Order()],
                    [.. existingWords.Except(finalWords).Order()]);
            });
    }

    private static Dictionary<int, HashSet<int>> WordsByAyah(
        IEnumerable<LinkingConfirmedContribution> contributions)
    {
        var wordsByAyah = new Dictionary<int, HashSet<int>>();
        foreach (var ayah in contributions.SelectMany(contribution => contribution.Units)
            .SelectMany(unit => unit.Ayahs))
        {
            AddWords(wordsByAyah, ayah.AyahId, ayah.QuranWordIds);
        }

        return wordsByAyah;
    }

    private static void AddWords(
        IDictionary<int, HashSet<int>> wordsByAyah,
        int ayahId,
        IEnumerable<int> wordIds)
    {
        if (!wordsByAyah.TryGetValue(ayahId, out var words))
        {
            words = [];
            wordsByAyah.Add(ayahId, words);
        }

        words.UnionWith(wordIds);
    }

    private static LinkingDoorWordImpact DoorWordImpactOf(
        IReadOnlyDictionary<int, LinkingDoorWordImpact> impacts,
        int ayahId) =>
        impacts.GetValueOrDefault(ayahId, new LinkingDoorWordImpact([], [], []));

    private static LinkingDescriptionChanges DescriptionChangesOf(
        IReadOnlyList<string> confirmed,
        IReadOnlyList<string> submitted)
    {
        var shared = Math.Min(confirmed.Count, submitted.Count);
        var changed = new List<string>();
        var unchanged = new List<string>();

        for (var position = 0; position < shared; position++)
        {
            if (string.Equals(confirmed[position], submitted[position], StringComparison.Ordinal))
            {
                unchanged.Add(submitted[position]);
            }
            else
            {
                changed.Add(submitted[position]);
            }
        }

        return new LinkingDescriptionChanges(
            [.. submitted.Skip(shared)],
            [.. confirmed.Skip(shared)],
            changed,
            unchanged);
    }

    private static LinkingClassificationCounts CountsOf(
        int requested,
        IReadOnlyList<LinkingAyahClassification> ayahs) =>
        new(
            requested,
            ayahs.Count(ayah => ayah.Classification == LinkingPreflightClassification.NewAyah),
            ayahs.Count(ayah => ayah.Classification == LinkingPreflightClassification.OverlapOtherSource),
            ayahs.Count(ayah => ayah.Classification == LinkingPreflightClassification.Unchanged),
            ayahs.Count(ayah => ayah.Classification == LinkingPreflightClassification.Update),
            ayahs.Count(ayah => ayah.Classification == LinkingPreflightClassification.Remove),
            ayahs.Count(ayah => ayah.Classification == LinkingPreflightClassification.Invalid));

    private static LinkingClassificationCounts Total(IEnumerable<LinkingClassificationCounts> counts)
    {
        var totals = counts.ToList();

        return new LinkingClassificationCounts(
            totals.Sum(count => count.Requested),
            totals.Sum(count => count.New),
            totals.Sum(count => count.Overlapping),
            totals.Sum(count => count.Unchanged),
            totals.Sum(count => count.Updated),
            totals.Sum(count => count.Removed),
            totals.Sum(count => count.Invalid));
    }

    private sealed record ConfirmedAyahEntry(
        LinkingConfirmedAyah Ayah,
        IReadOnlyList<int> GroupedWith,
        int UnitRank);

    private sealed record SubmittedAyahEntry(
        LinkingOperationAyahIntent Ayah,
        IReadOnlyList<int> GroupedWith,
        int UnitRank);
}
