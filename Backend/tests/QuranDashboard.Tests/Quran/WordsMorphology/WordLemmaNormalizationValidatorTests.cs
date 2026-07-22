using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

public sealed class WordLemmaNormalizationValidatorTests
{
    private static readonly IReadOnlyList<WordLemmaMappingEvidenceEntry> RealEvidence =
        new WordLemmaNormalizationReader().LoadEvidence();

    private const string AutoReliableBuckwalter = ">aDal~";
    private const string AutoReliableArabic = "أَضَلّ";

    private const string CuratedBelowThresholdBuckwalter = "vuqifu";
    private const string CuratedBelowThresholdArabic = "ثُقِفُ";

    [Fact]
    public void Minimal_valid_artifact_validates()
    {
        var loaded = Loaded(AddEntry("WLN-1", "2:1:1", AutoReliableBuckwalter, AutoReliableArabic));

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(loaded);

        act.Should().NotThrow();
    }

    [Fact]
    public void Unsupported_schema_version_fails()
    {
        var baseLoaded = Loaded(AddEntry("WLN-1", "2:1:1", AutoReliableBuckwalter, AutoReliableArabic));
        var loaded = baseLoaded with
        {
            Artifact = baseLoaded.Artifact with { SchemaVersion = 99 },
        };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(loaded);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*Unsupported word-lemma-normalization schemaVersion*99*");
    }

    [Fact]
    public void Duplicate_id_fails()
    {
        var entries = new[]
        {
            AddEntry("WLN-DUP", "2:1:1", AutoReliableBuckwalter, AutoReliableArabic),
            AddEntry("WLN-DUP", "2:1:2", AutoReliableBuckwalter, AutoReliableArabic),
        };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(entries));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*Duplicate normalization entry id 'WLN-DUP'*");
    }

    [Fact]
    public void Duplicate_location_fails()
    {
        var entries = new[]
        {
            AddEntry("WLN-1", "2:1:1", AutoReliableBuckwalter, AutoReliableArabic),
            AddEntry("WLN-2", "2:1:1", AutoReliableBuckwalter, AutoReliableArabic),
        };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(entries));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*duplicate operation location '2:1:1'*");
    }

    [Fact]
    public void Candidate_status_in_active_artifact_fails()
    {
        var entry = AddEntry("WLN-1", "2:1:1", AutoReliableBuckwalter, AutoReliableArabic)
            with { DecisionStatus = WordLemmaNormalizationDecisionStatus.Candidate };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*non-final decisionStatus*Candidate*");
    }

    [Fact]
    public void Needs_review_status_in_active_artifact_fails()
    {
        var entry = AddEntry("WLN-1", "2:1:1", AutoReliableBuckwalter, AutoReliableArabic)
            with { DecisionStatus = WordLemmaNormalizationDecisionStatus.NeedsReview };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*non-final decisionStatus*NeedsReview*");
    }

    [Fact]
    public void Add_with_non_null_expected_current_fails()
    {
        var entry = AddEntry("WLN-1", "2:1:1", AutoReliableBuckwalter, AutoReliableArabic)
            with { ExpectedCurrentLemmaArabic = "خاطئ" };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*'add' must have null expectedCurrentLemmaArabic*");
    }

    [Fact]
    public void Add_with_null_corrected_fails()
    {
        var entry = AddEntry("WLN-1", "2:1:1", AutoReliableBuckwalter, AutoReliableArabic)
            with { CorrectedLemmaArabic = null };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*'add' must have a non-null correctedLemmaArabic*");
    }

    [Fact]
    public void Remove_with_null_expected_current_fails()
    {
        var entry = RemoveEntry("WLN-1", "2:1:1", "نَفْس") with { ExpectedCurrentLemmaArabic = null };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*'remove' must have a non-null expectedCurrentLemmaArabic*");
    }

    [Fact]
    public void Remove_with_non_null_corrected_fails()
    {
        var entry = RemoveEntry("WLN-1", "2:1:1", "نَفْس") with { CorrectedLemmaArabic = "نَفْس" };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*'remove' must have null correctedLemmaArabic*");
    }

    [Fact]
    public void Replace_with_equal_expected_and_corrected_fails()
    {
        var entry = ReplaceEntry("WLN-1", "2:1:1", AutoReliableArabic, AutoReliableBuckwalter, AutoReliableArabic);

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*'replace' correctedLemmaArabic must differ from expectedCurrentLemmaArabic*");
    }

    [Fact]
    public void Replace_with_null_corrected_fails()
    {
        var entry = ReplaceEntry("WLN-1", "2:1:1", "خاطئ", AutoReliableBuckwalter, AutoReliableArabic)
            with { CorrectedLemmaArabic = null };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*'replace' must have a non-null correctedLemmaArabic*");
    }

    [Fact]
    public void Keep_with_corrected_not_equal_expected_fails()
    {
        var entry = KeepEntry("WLN-1", "2:1:1", "عَلا") with { CorrectedLemmaArabic = "أَعْلَى" };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*'keep' correctedLemmaArabic must equal expectedCurrentLemmaArabic*");
    }

    [Fact]
    public void Exception_without_reason_fails()
    {
        var entry = ExceptionEntry("WLN-1", "2:1:1", "مَا") with { Reason = null };

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*'exception' must include a reason*");
    }

    [Fact]
    public void Invalid_location_format_fails()
    {
        var entry = AddEntry("WLN-1", "not-a-location", AutoReliableBuckwalter, AutoReliableArabic);

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*invalid location format*");
    }

    [Fact]
    public void Add_without_any_mapping_evidence_fails()
    {
        var entry = AddEntry("WLN-1", "2:1:1", "ZZunmapped", "خَطَأ");

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*has no curated mapping evidence*");
    }

    [Fact]
    public void Auto_reliable_mapping_add_passes()
    {
        var entry = AddEntry("WLN-1", "2:1:1", AutoReliableBuckwalter, AutoReliableArabic);

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().NotThrow();
    }

    [Fact]
    public void Curated_qul_consistent_mapping_replace_passes_for_vuqifu()
    {
        var entry = ReplaceEntry(
            "WLN-1", "33:61:3", "أَيْن",
            CuratedBelowThresholdBuckwalter, CuratedBelowThresholdArabic);

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().NotThrow();
    }

    [Fact]
    public void Curated_below_threshold_mapping_add_passes_for_hil()
    {
        var entry = AddEntry("WLN-1", "5:5:9", "Hil~", "حِلّ");

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().NotThrow();
    }

    [Fact]
    public void Below_threshold_mapping_without_curated_evidence_fails()
    {
        var entry = ReplaceEntry(
            "WLN-1", "33:61:3", "أَيْن",
            "ZZnotCurated", CuratedBelowThresholdArabic);

        var act = () => WordLemmaNormalizationValidator.ValidateSchema(Loaded(new[] { entry }));

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*has no curated mapping evidence*");
    }

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

    private static WordLemmaNormalizationLoaded Loaded(WordLemmaNormalizationEntry entry) =>
        Loaded(new[] { entry });

    private static WordLemmaNormalizationCounts CountZero() =>
        new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    private static WordLemmaNormalizationEntry AddEntry(
        string id, string location, string buckwalter, string arabic) =>
        BaseEntry(id, location, WordLemmaNormalizationOperationKind.Add) with
        {
            ExpectedCurrentLemmaArabic = null,
            CorrectedLemmaArabic = arabic,
            CorpusLemmaBuckwalter = buckwalter,
            ArabicMappingEvidence = $"{buckwalter} -> {arabic}",
        };

    private static WordLemmaNormalizationEntry RemoveEntry(
        string id, string location, string expected) =>
        BaseEntry(id, location, WordLemmaNormalizationOperationKind.Remove) with
        {
            ExpectedCurrentLemmaArabic = expected,
            CorrectedLemmaArabic = null,
        };

    private static WordLemmaNormalizationEntry ReplaceEntry(
        string id, string location, string expected, string buckwalter, string arabic) =>
        BaseEntry(id, location, WordLemmaNormalizationOperationKind.Replace) with
        {
            ExpectedCurrentLemmaArabic = expected,
            CorrectedLemmaArabic = arabic,
            CorpusLemmaBuckwalter = buckwalter,
            ArabicMappingEvidence = $"{buckwalter} -> {arabic}",
        };

    private static WordLemmaNormalizationEntry KeepEntry(
        string id, string location, string expected) =>
        BaseEntry(id, location, WordLemmaNormalizationOperationKind.Keep) with
        {
            ExpectedCurrentLemmaArabic = expected,
            CorrectedLemmaArabic = expected,
        };

    private static WordLemmaNormalizationEntry ExceptionEntry(
        string id, string location, string observed) =>
        BaseEntry(id, location, WordLemmaNormalizationOperationKind.Exception) with
        {
            ExpectedCurrentLemmaArabic = observed,
            CorrectedLemmaArabic = observed,
        };

    private static WordLemmaNormalizationEntry BaseEntry(
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
