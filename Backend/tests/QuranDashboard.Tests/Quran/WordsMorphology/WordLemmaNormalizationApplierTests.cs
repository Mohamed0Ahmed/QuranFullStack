using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

public sealed class WordLemmaNormalizationApplierTests
{
    private static readonly IReadOnlyList<WordLemmaMappingEvidenceEntry> RealEvidence =
        new WordLemmaNormalizationReader().LoadEvidence();

    [Fact]
    public void Add_inserts_corrected_lemma_into_absent_location()
    {
        var raw = EmptyMap();
        var entry = Add("WLN-1", "2:1:1", ">aDal~", "أَضَلّ");

        var result = ApplyAll(raw, entry);

        result.CorrectedLemmas["2:1:1"].Should().Be("أَضَلّ");
        result.Summary.AppliedAdd.Should().Be(1);
        result.Summary.AppliedRemove.Should().Be(0);
        result.Summary.AppliedReplace.Should().Be(0);
        result.Summary.FailedOrSkipped.Should().Be(0);
    }

    [Fact]
    public void Add_fails_when_raw_lemma_already_present()
    {
        var raw = new Dictionary<string, string> { ["2:1:1"] = "سابق" };
        var entry = Add("WLN-1", "2:1:1", ">aDal~", "أَضَلّ");

        var act = () => ApplyAll(raw, entry);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*'add' expects raw lemma absent*");
    }

    [Fact]
    public void Remove_deletes_existing_raw_lemma_matching_expected()
    {
        var raw = new Dictionary<string, string> { ["2:1:1"] = "نَفْس" };
        var entry = Remove("WLN-1", "2:1:1", "نَفْس");

        var result = ApplyAll(raw, entry);

        result.CorrectedLemmas.ContainsKey("2:1:1").Should().BeFalse();
        result.Summary.AppliedRemove.Should().Be(1);
    }

    [Fact]
    public void Replace_swaps_wrong_lemma_to_corrected_lemma()
    {
        var raw = new Dictionary<string, string> { ["3:33:7"] = "ءَال" };
        var entry = Replace("WLN-1", "3:33:7", "ءَال", "<iboraAhiym", "إِبْرَاهِيم");

        var result = ApplyAll(raw, entry);

        result.CorrectedLemmas["3:33:7"].Should().Be("إِبْرَاهِيم");
        result.Summary.AppliedReplace.Should().Be(1);
    }

    [Fact]
    public void Keep_leaves_raw_lemma_unchanged()
    {
        var raw = new Dictionary<string, string> { ["17:7:22"] = "عَلا" };
        var entry = Keep("WLN-1", "17:7:22", "عَلا");

        var result = ApplyAll(raw, entry);

        result.CorrectedLemmas["17:7:22"].Should().Be("عَلا");
        result.Summary.ReviewedKeep.Should().Be(1);
        result.Summary.AppliedMutating.Should().Be(0);
    }

    [Fact]
    public void Exception_leaves_raw_lemma_unchanged()
    {
        var raw = new Dictionary<string, string> { ["33:61:2"] = "مَا" };
        var entry = Exception("WLN-1", "33:61:2", "مَا");

        var result = ApplyAll(raw, entry);

        result.CorrectedLemmas["33:61:2"].Should().Be("مَا");
        result.Summary.ReviewedException.Should().Be(1);
        result.Summary.AppliedMutating.Should().Be(0);
    }

    [Fact]
    public void Expected_current_mismatch_fails()
    {
        var raw = new Dictionary<string, string> { ["2:1:1"] = "واقعي" };
        var entry = Replace("WLN-1", "2:1:1", "متوقع", ">aDal~", "أَضَلّ");

        var act = () => ApplyAll(raw, entry);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*'replace' expectedCurrentLemmaArabic*does not match raw lemma*");
    }

    [Fact]
    public void Input_raw_map_is_not_mutated()
    {
        var raw = new Dictionary<string, string> { ["3:33:7"] = "ءَال" };
        var snapshot = new Dictionary<string, string>(raw);
        var entry = Replace("WLN-1", "3:33:7", "ءَال", "<iboraAhiym", "إِبْرَاهِيم");

        ApplyAll(raw, entry);

        raw.Should().Equal(snapshot);
        raw["3:33:7"].Should().Be("ءَال");
    }

    [Fact]
    public void Correction_location_absent_from_readable_words_fails()
    {
        var raw = EmptyMap();
        var entry = Add("WLN-1", "2:1:1", ">aDal~", "أَضَلّ");
        var readable = new HashSet<string> { "1:1:1" };

        var act = () => ApplyAll(raw, entry, readable);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*absent from readable quran_words*");
    }

    [Fact]
    public void Spot_check_real_artifact_applies_all_known_corrections()
    {
        var loaded = new WordLemmaNormalizationReader().Load();
        var raw = BuildRawMapMatchingExpected(loaded.Artifact);
        var readable = new HashSet<string>(raw.Keys, StringComparer.Ordinal);
        foreach (var entry in loaded.Artifact.Entries)
        {
            readable.Add(entry.Location);
        }

        var result = new WordLemmaNormalizationReader().Apply(raw, loaded, readable);

        result.Summary.TotalEntries.Should().Be(1918);
        result.Summary.AppliedAdd.Should().Be(1659);
        result.Summary.AppliedRemove.Should().Be(69);
        result.Summary.AppliedReplace.Should().Be(90);
        result.Summary.ReviewedKeep.Should().Be(92);
        result.Summary.ReviewedException.Should().Be(8);
        result.Summary.FailedOrSkipped.Should().Be(0);
        result.Summary.ArtifactSha256.Should().NotBeNullOrWhiteSpace();

        AssertSpot(result, "3:33:7", "إِبْرَاهِيم");
        AssertSpot(result, "21:51:3", "إِبْرَاهِيم");
        AssertSpot(result, "28:50:10", "أَضَلّ");
        AssertSpot(result, "28:50:11", "مِن");
        AssertSpot(result, "33:61:3", "ثُقِفُ");
        AssertSpot(result, "4:91:26", "ثُقِفُ");
        AssertSpot(result, "17:7:22", "عَلا");
        AssertSpot(result, "4:116:1", "إِنّ");
        AssertSpot(result, "33:61:2", "مَا");
    }

    private static void AssertSpot(WordLemmaNormalizationResult result, string location, string expected)
    {
        result.CorrectedLemmas.TryGetValue(location, out var actual).Should().BeTrue(
            $"spot-check location {location} should be present in the corrected map");
        actual.Should().Be(expected, $"spot-check location {location}");
    }

    private static Dictionary<string, string> BuildRawMapMatchingExpected(WordLemmaNormalizationArtifact artifact)
    {
        var raw = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in artifact.Entries)
        {
            if (entry.ExpectedCurrentLemmaArabic is not null)
            {
                raw[entry.Location] = entry.ExpectedCurrentLemmaArabic;
            }
        }

        return raw;
    }

    private static WordLemmaNormalizationResult ApplyAll(
        Dictionary<string, string> raw,
        WordLemmaNormalizationEntry entry,
        IReadOnlySet<string>? readable = null)
    {
        var loaded = Loaded(new[] { entry });
        return WordLemmaNormalizationApplier.Apply(raw, loaded.Artifact, readable, rawLemmasSha256: null);
    }

    private static Dictionary<string, string> EmptyMap() => new(StringComparer.Ordinal);

    private static WordLemmaNormalizationLoaded Loaded(IReadOnlyList<WordLemmaNormalizationEntry> entries)
    {
        var artifact = new WordLemmaNormalizationArtifact
        {
            SchemaVersion = WordLemmaNormalizationArtifact.SupportedSchemaVersion,
            ArtifactId = "word-lemma-normalization",
            Entries = entries,
        };

        return new WordLemmaNormalizationLoaded(
            artifact, RealEvidence, "deadbeef".PadRight(64, '0'), CountZero());
    }

    private static WordLemmaNormalizationCounts CountZero() =>
        new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    private static WordLemmaNormalizationEntry Add(
        string id, string location, string buckwalter, string arabic) =>
        Base(id, location, WordLemmaNormalizationOperationKind.Add) with
        {
            ExpectedCurrentLemmaArabic = null,
            CorrectedLemmaArabic = arabic,
            CorpusLemmaBuckwalter = buckwalter,
            ArabicMappingEvidence = $"{buckwalter} -> {arabic}",
        };

    private static WordLemmaNormalizationEntry Remove(string id, string location, string expected) =>
        Base(id, location, WordLemmaNormalizationOperationKind.Remove) with
        {
            ExpectedCurrentLemmaArabic = expected,
            CorrectedLemmaArabic = null,
        };

    private static WordLemmaNormalizationEntry Replace(
        string id, string location, string expected, string buckwalter, string arabic) =>
        Base(id, location, WordLemmaNormalizationOperationKind.Replace) with
        {
            ExpectedCurrentLemmaArabic = expected,
            CorrectedLemmaArabic = arabic,
            CorpusLemmaBuckwalter = buckwalter,
            ArabicMappingEvidence = $"{buckwalter} -> {arabic}",
        };

    private static WordLemmaNormalizationEntry Keep(string id, string location, string expected) =>
        Base(id, location, WordLemmaNormalizationOperationKind.Keep) with
        {
            ExpectedCurrentLemmaArabic = expected,
            CorrectedLemmaArabic = expected,
        };

    private static WordLemmaNormalizationEntry Exception(string id, string location, string observed) =>
        Base(id, location, WordLemmaNormalizationOperationKind.Exception) with
        {
            ExpectedCurrentLemmaArabic = observed,
            CorrectedLemmaArabic = observed,
        };

    private static WordLemmaNormalizationEntry Base(
        string id, string location, WordLemmaNormalizationOperationKind kind) =>
        new()
        {
            Id = id,
            Location = location,
            OperationKind = kind,
            DecisionStatus = WordLemmaNormalizationDecisionStatus.Approved,
            Reason = "test reason",
        };
}
