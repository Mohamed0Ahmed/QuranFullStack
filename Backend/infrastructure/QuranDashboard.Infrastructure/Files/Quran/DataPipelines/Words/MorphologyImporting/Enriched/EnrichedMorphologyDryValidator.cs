using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

public sealed class EnrichedMorphologyDryValidator
{
    public EnrichedMorphologyDryValidationResult Validate(
        MorphologySourceData source,
        EnrichedMorphologyManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(manifest);

        var checks = new List<EnrichedMorphologyDryCheck>();

        checks.Add(new EnrichedMorphologyDryCheck(
            "ENRICHED-RECORD-COUNT",
            Expected: "77432",
            Observed: source.Words.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Passed: source.Words.Count == 77_432));

        var segmentCount = source.Words.Sum(word => word.Segments.Count);
        checks.Add(new EnrichedMorphologyDryCheck(
            "ENRICHED-SEGMENT-COUNT",
            Expected: "128219",
            Observed: segmentCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Passed: segmentCount == 128_219));

        // Gate 3: 0 fallback / pseudo-segments. The enriched pathway produces no marker rows; assert every
        // word has at least one real segment (the artifact carries 0 fallback records).
        var emptyWords = source.Words.Where(word => word.Segments.Count == 0).ToList();
        checks.Add(new EnrichedMorphologyDryCheck(
            "ENRICHED-NO-FALLBACK-WORDS",
            Expected: "0",
            Observed: emptyWords.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Passed: emptyWords.Count == 0));

        var duplicateLocations = source.Words
            .GroupBy(word => word.Location, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();
        checks.Add(new EnrichedMorphologyDryCheck(
            "ENRICHED-NO-DUPLICATE-LOCATIONS",
            Expected: "0",
            Observed: duplicateLocations.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Passed: duplicateLocations.Count == 0));

        var duplicateSegmentKeys = source.Words
            .SelectMany(word => word.Segments.Select(segment => $"{word.Location}:{segment.SegmentNumber}"))
            .GroupBy(key => key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();
        checks.Add(new EnrichedMorphologyDryCheck(
            "ENRICHED-NO-DUPLICATE-SEGMENT-KEYS",
            Expected: "0",
            Observed: duplicateSegmentKeys.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Passed: duplicateSegmentKeys.Count == 0));

        foreach (var (ayah, expectedWordCount) in BoundaryAyahExpectedWordCounts)
        {
            var observed = source.Words.Count(word => word.Location.StartsWith(ayah + ":", StringComparison.Ordinal));
            checks.Add(new EnrichedMorphologyDryCheck(
                $"ENRICHED-BOUNDARY-{ayah}",
                Expected: expectedWordCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Observed: observed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Passed: observed == expectedWordCount));
        }

        var eightSixTwelve = source.Words.FirstOrDefault(word => word.Location == "8:6:12");
        checks.Add(new EnrichedMorphologyDryCheck(
            "ENRICHED-BOUNDARY-8:6:12-SEGMENTS",
            Expected: "2",
            Observed: eightSixTwelve?.Segments.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "missing",
            Passed: eightSixTwelve is not null && eightSixTwelve.Segments.Count == 2));

        checks.Add(new EnrichedMorphologyDryCheck(
            "ENRICHED-POS-RESOLVES",
            Expected: "0 unknown",
            Observed: source.UnknownPosCodes.Count == 0
                ? "0 unknown"
                : $"{source.UnknownPosCodes.Count} unknown: {string.Join(",", source.UnknownPosCodes)}",
            Passed: source.UnknownPosCodes.Count == 0));

        // Gate 8: no QUL word-level lemma truth — structural assertion. The enriched pathway is the only
        // IMorphologyImportSource implementation that produces value-based lemma identity; we assert the
        // data came through it by checking the SourceArtifact provenance carried on the manifest.
        checks.Add(new EnrichedMorphologyDryCheck(
            "ENRICHED-NO-QUL-WORD-LEMMA-LINK",
            Expected: "corpus-bridge-enriched provenance; quranWordIdVerifiedAgainstDashboard=true",
            Observed: $"provenance={manifest.Provenance ?? "(null)"}; verified={manifest.QuranWordIdVerifiedAgainstDashboard}",
            Passed: manifest.QuranWordIdVerifiedAgainstDashboard
                && string.Equals(manifest.Provenance, "corpus-bridge-enriched", StringComparison.Ordinal)));

        // Corrected-lemma spot checks (gates 11a-d). Each must resolve to the Corpus-derived lemma, not
        // the QUL-shifted value.
        foreach (var (location, forbidden, expectedLemma) in CorrectedLemmaSpotChecks)
        {
            var word = source.Words.FirstOrDefault(w => w.Location == location);
            var lemmaText = word is not null && word.LemmaId.HasValue
                ? source.ResolvedLemmas.FirstOrDefault(l => l.AssignedId == word.LemmaId.Value)?.LemmaText
                : null;
            var observed = lemmaText ?? "(unresolved)";
            checks.Add(new EnrichedMorphologyDryCheck(
                $"ENRICHED-CORRECTED-LEMMA-{location}",
                Expected: $"{expectedLemma} (not {forbidden})",
                Observed: observed,
                Passed: !string.IsNullOrEmpty(lemmaText)
                    && !string.Equals(lemmaText, forbidden, StringComparison.Ordinal)
                    && string.Equals(lemmaText, expectedLemma, StringComparison.Ordinal)));
        }

        var allPassed = checks.All(check => check.Passed);
        return new EnrichedMorphologyDryValidationResult(allPassed, checks);
    }

    // Boundary ayahs from plan §5/§7 (gate 10).
    private static readonly (string Ayah, int ExpectedWordCount)[] BoundaryAyahExpectedWordCounts =
    [
        ("2:181", 14),
        ("2:282", 128),
        ("8:6", 12),
        ("13:37", 20),
    ];

    // Corrected-lemma spot checks from plan §7 (gates 11a-d). The expected lemma is the exact vocalized
    // lemmaArabic the Corpus bridge produces for the corrected lemmaBuckwalter.
    private static readonly (string Location, string Forbidden, string ExpectedLemma)[] CorrectedLemmaSpotChecks =
    [
        ("41:44:16", "ءَامَنَ", "شِفَاء"),
        ("11:29:17", "ءَامَنَ", "مُّلَٰقُوا"),
        ("2:102:41", "فَرَّقُ", "مَرْء"),
        ("2:144:20", "كَانَ", "شَطْر"),
    ];
}

public sealed record EnrichedMorphologyDryValidationResult(
    bool AllPassed,
    IReadOnlyList<EnrichedMorphologyDryCheck> Checks);

public sealed record EnrichedMorphologyDryCheck(
    string Id,
    string Expected,
    string Observed,
    bool Passed);
