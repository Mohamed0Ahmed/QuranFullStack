namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

// Never mutates the input map (works on a copy); every approved mutating op must apply or the whole
// apply fails (all-or-nothing).
internal static class WordLemmaNormalizationApplier
{
    private static readonly IReadOnlySet<string> SpotCheckLocations = new HashSet<string>(StringComparer.Ordinal)
    {
        "3:33:7", "21:51:3", "28:50:10", "28:50:11",
        "33:61:3", "4:91:26", "17:7:22", "4:116:1", "33:61:2",
    };

    public static WordLemmaNormalizationResult Apply(
        IReadOnlyDictionary<string, string> rawLemmas,
        WordLemmaNormalizationArtifact artifact,
        IReadOnlySet<string>? readableWordLocations,
        string? rawLemmasSha256)
    {
        ArgumentNullException.ThrowIfNull(rawLemmas);
        ArgumentNullException.ThrowIfNull(artifact);

        var corrected = new Dictionary<string, string>(rawLemmas.Count, StringComparer.Ordinal);
        foreach (var pair in rawLemmas)
        {
            corrected[pair.Key] = pair.Value;
        }

        var add = 0;
        var remove = 0;
        var replace = 0;
        var keep = 0;
        var exception = 0;
        var failed = 0;
        var spotChecks = new List<WordLemmaNormalizationSpotCheck>();

        foreach (var entry in artifact.Entries)
        {
            var where = $"entry '{entry.Id}' at '{entry.Location}'";

            if (readableWordLocations is not null
                && !readableWordLocations.Contains(entry.Location))
            {
                throw new InvalidDataException(
                    $"{where}: correction location '{entry.Location}' is absent from readable quran_words.");
            }

            var rawHasLemma = corrected.TryGetValue(entry.Location, out var rawLemma);
            var rawAtLocation = rawHasLemma ? rawLemma : null;

            switch (entry.OperationKind)
            {
                case WordLemmaNormalizationOperationKind.Add:
                    FailWhen(rawAtLocation is not null,
                        $"{where}: 'add' expects raw lemma absent, but found '{rawAtLocation}'.");
                    corrected[entry.Location] = entry.CorrectedLemmaArabic!;
                    add++;
                    break;

                case WordLemmaNormalizationOperationKind.Remove:
                    FailWhen(rawAtLocation is null,
                        $"{where}: 'remove' expects raw lemma present, but it is absent.");
                    FailWhen(!string.Equals(rawAtLocation, entry.ExpectedCurrentLemmaArabic,
                            StringComparison.Ordinal),
                        $"{where}: 'remove' expectedCurrentLemmaArabic '{entry.ExpectedCurrentLemmaArabic}' "
                        + $"does not match raw lemma '{rawAtLocation}'.");
                    corrected.Remove(entry.Location);
                    remove++;
                    break;

                case WordLemmaNormalizationOperationKind.Replace:
                    FailWhen(rawAtLocation is null,
                        $"{where}: 'replace' expects raw lemma present, but it is absent.");
                    FailWhen(!string.Equals(rawAtLocation, entry.ExpectedCurrentLemmaArabic,
                            StringComparison.Ordinal),
                        $"{where}: 'replace' expectedCurrentLemmaArabic '{entry.ExpectedCurrentLemmaArabic}' "
                        + $"does not match raw lemma '{rawAtLocation}'.");
                    corrected[entry.Location] = entry.CorrectedLemmaArabic!;
                    replace++;
                    break;

                case WordLemmaNormalizationOperationKind.Keep:
                    // Non-mutating. If the keep carries a non-null expected value, the raw lemma (when
                    // present) must match it. Null/null keeps represent "valid null / absence accepted"
                    // and impose no constraint on the raw map.
                    if (entry.ExpectedCurrentLemmaArabic is not null
                        && rawAtLocation is not null
                        && !string.Equals(rawAtLocation, entry.ExpectedCurrentLemmaArabic,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            $"{where}: 'keep' expectedCurrentLemmaArabic '{entry.ExpectedCurrentLemmaArabic}' "
                            + $"does not match raw lemma '{rawAtLocation}'.");
                    }

                    keep++;
                    break;

                case WordLemmaNormalizationOperationKind.Exception:
                    // Non-mutating; recorded only. Observed value, if any, is left as-is.
                    exception++;
                    break;

                default:
                    // Fail(true, ...) always throws, so this arm never returns normally.
                    Fail(true, $"{where}: invalid operationKind '{entry.OperationKind}'.");
                    break;
            }

            if (SpotCheckLocations.Contains(entry.Location))
            {
                var applied = corrected.TryGetValue(entry.Location, out var after) ? after : null;
                spotChecks.Add(new WordLemmaNormalizationSpotCheck(
                    entry.Location, entry.OperationKind.ToString().ToLowerInvariant(), applied));
            }
        }

        var summary = new WordLemmaCorrectionSummary(
            ArtifactSha256: string.Empty, // Filled by caller (reader) when apply is invoked via Load+Apply.
            RawLemmasSha256: rawLemmasSha256,
            TotalEntries: artifact.Entries.Count,
            AppliedAdd: add,
            AppliedRemove: remove,
            AppliedReplace: replace,
            ReviewedKeep: keep,
            ReviewedException: exception,
            FailedOrSkipped: failed, // Apply is all-or-nothing: any invalid entry throws, so this is always 0.
            SpotChecks: spotChecks)
        {
            ProblemClassCounts = BuildProblemClassCounts(artifact.Entries),
        };

        return new WordLemmaNormalizationResult(corrected, summary);
    }

    private static IReadOnlyDictionary<string, int> BuildProblemClassCounts(
        IReadOnlyList<WordLemmaNormalizationEntry> entries)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.ProblemClass))
            {
                continue;
            }

            counts.TryGetValue(entry.ProblemClass, out var count);
            counts[entry.ProblemClass] = count + 1;
        }

        return counts;
    }

    private static void FailWhen(bool condition, string message) => Fail(condition, message);

    private static void Fail(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidDataException(message);
        }
    }
}
