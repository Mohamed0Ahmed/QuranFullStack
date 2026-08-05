using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class MorphologyValidationFailureTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public async Task Validation_Violation_On_Empty_Target_Rolls_Back_All_Six_Tables()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSourceFolderWithValidationViolationAsync();
        var readableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, force: false, expectedReadableWords: readableCount);

        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.MorphologyRows.Should().Be(0);
        snapshot.SegmentRows.Should().Be(0);
        snapshot.RootRows.Should().Be(0);
        snapshot.LemmaRows.Should().Be(0);
        snapshot.StemRows.Should().Be(0);
        snapshot.PosTagRows.Should().Be(0);
        snapshot.MorphologyContentHash.Should().Be(EmptyTableContentHash);
        snapshot.SegmentContentHash.Should().Be(EmptyTableContentHash);
        snapshot.RootContentHash.Should().Be(EmptyTableContentHash);
        snapshot.LemmaContentHash.Should().Be(EmptyTableContentHash);
        snapshot.StemContentHash.Should().Be(EmptyTableContentHash);
        snapshot.PosTagContentHash.Should().Be(EmptyTableContentHash);
    }

    private const string EmptyTableContentHash = "empty";

    [Fact]
    public async Task Failure_Report_Written_With_Verdict_Fail()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSourceFolderWithValidationViolationAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"morph-report-fail-{Guid.NewGuid():N}");
        var readableCount = fixture.GetReadableWordCount();

        try
        {
            await using var scope = fixture.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();
            var result = await handler.HandleAsync(
                new ImportMorphologyCommand(sourcePath, false, readableCount, reportDir),
                CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.ReportOutDir.Should().Be(reportDir);

            File.Exists(Path.Combine(reportDir, "morphology-import-report.md")).Should().BeTrue();
            File.Exists(Path.Combine(reportDir, "morphology-import-report.json")).Should().BeTrue();

            var jsonPath = Path.Combine(reportDir, "morphology-import-report.json");
            await using var jsonStream = File.OpenRead(jsonPath);
            var report = await JsonSerializer.DeserializeAsync<JsonElement>(jsonStream);

            report.GetProperty("verdict").GetString().Should().Be("fail");
            report.GetProperty("persisted").GetBoolean().Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, true);
            }
        }
    }

    [Fact]
    public async Task Import_fails_when_multi_stem_lemma_is_unresolved()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchCorpusSegmentsAsync(
            sourcePath,
            "1:1:3",
            [
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "PN",
                    form = "raboni",
                    features = "GEN",
                    root = "rbb",
                    lemma = "rabb"
                },
                new
                {
                    segmentNumber = (short)2,
                    kind = "STEM",
                    pos = "NEG",
                    form = "laA",
                    features = "STEM|POS:NEG",
                    root = (string?)null,
                    lemma = "missing"
                }
            ]);

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        result.Message.Should().Contain(MorphologyInvariants.CheckSegLemmaMultiStemResolves);

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.MorphologyRows.Should().Be(0);
        snapshot.SegmentRows.Should().Be(0);
    }

    [Fact]
    public async Task Import_fails_when_duplicate_multi_stem_lemma_has_no_safe_form_match()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchQulLemmaMapAsync(
            sourcePath,
            new Dictionary<string, string>
            {
                ["1:1:1"] = "SYNTHETIC_DUP_A",
                ["1:1:2"] = "SYNTHETIC_DUP_B",
                ["1:1:3"] = MorphologySyntheticSeed.LemmaValue2,
                ["1:2:1"] = MorphologySyntheticSeed.LemmaValue3,
                ["1:2:2"] = "كِتَاب"
            });
        await fixture.PatchCorpusSegmentsAsync(
            sourcePath,
            "1:1:1",
            [
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "N",
                    form = "kataba",
                    features = "NOM",
                    root = (string?)null,
                    lemma = "dup"
                }
            ]);
        await fixture.PatchCorpusSegmentsAsync(
            sourcePath,
            "1:1:2",
            [
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "N",
                    form = "kitAbi",
                    features = "GEN",
                    root = (string?)null,
                    lemma = "dup"
                }
            ]);
        await fixture.PatchCorpusSegmentsAsync(
            sourcePath,
            "1:1:3",
            [
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "PN",
                    form = "raboni",
                    features = "GEN",
                    root = "rbb",
                    lemma = "rabb"
                },
                new
                {
                    segmentNumber = (short)2,
                    kind = "STEM",
                    pos = "N",
                    form = "somi",
                    features = "NOM",
                    root = (string?)null,
                    lemma = "dup"
                }
            ]);

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        result.Message.Should().Contain(MorphologyInvariants.CheckSegLemmaNoFanout);

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.MorphologyRows.Should().Be(0);
        snapshot.SegmentRows.Should().Be(0);
    }

    [Fact]
    public async Task Import_fails_when_segment_root_is_unresolved()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchCorpusSegmentsAsync(
            sourcePath,
            "1:1:2",
            [
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "N",
                    form = "l~Ahi",
                    features = "GEN",
                    root = "missing",
                    lemma = (string?)null
                }
            ]);

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        result.Message.Should().Contain(MorphologyInvariants.CheckSegRootResolves);

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.MorphologyRows.Should().Be(0);
        snapshot.SegmentRows.Should().Be(0);
    }

    [Fact]
    public async Task Import_fails_when_segment_root_is_ambiguous()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchQulRootMapAsync(
            sourcePath,
            new Dictionary<string, string>
            {
                ["1:1:1"] = "SYNTHETIC_ROOT_A",
                ["1:1:2"] = "SYNTHETIC_ROOT_B",
                ["1:1:3"] = MorphologySyntheticSeed.RootValue2,
                ["1:2:1"] = MorphologySyntheticSeed.RootValue3,
                ["1:2:2"] = MorphologySyntheticSeed.RootValue3
            });
        await fixture.PatchCorpusSegmentsAsync(
            sourcePath,
            "1:1:1",
            [
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "N",
                    form = "kataba",
                    features = "NOM",
                    root = "dup",
                    lemma = (string?)null
                }
            ]);
        await fixture.PatchCorpusSegmentsAsync(
            sourcePath,
            "1:1:2",
            [
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "N",
                    form = "kitAbi",
                    features = "GEN",
                    root = "dup",
                    lemma = (string?)null
                }
            ]);

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        result.Message.Should().Contain(MorphologyInvariants.CheckSegRootResolves);

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.MorphologyRows.Should().Be(0);
        snapshot.SegmentRows.Should().Be(0);
    }

    [Fact]
    public async Task Forced_Run_Over_Populated_Tables_With_Validation_Failure_Preserves_Previous()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var first = await fixture.RunImportAsync(sourcePath, force: false, expectedReadableWords: readableCount);
        first.Succeeded.Should().BeTrue();

        var snapshotBefore = await fixture.CaptureTableSnapshotAsync();

        var violationPath = await fixture.WriteSourceFolderWithValidationViolationAsync();

        var forced = await fixture.RunImportAsync(violationPath, force: true, expectedReadableWords: readableCount);
        forced.Succeeded.Should().BeFalse();

        var snapshotAfter = await fixture.CaptureTableSnapshotAsync();

        snapshotAfter.MorphologyRows.Should().Be(snapshotBefore.MorphologyRows);
        snapshotAfter.SegmentRows.Should().Be(snapshotBefore.SegmentRows);
        snapshotAfter.PosTagRows.Should().Be(snapshotBefore.PosTagRows);
        snapshotAfter.QuranWordRows.Should().Be(snapshotBefore.QuranWordRows);
        snapshotAfter.MorphologyContentHash.Should().Be(snapshotBefore.MorphologyContentHash);
        snapshotAfter.SegmentContentHash.Should().Be(snapshotBefore.SegmentContentHash);
        snapshotAfter.RootContentHash.Should().Be(snapshotBefore.RootContentHash);
        snapshotAfter.LemmaContentHash.Should().Be(snapshotBefore.LemmaContentHash);
        snapshotAfter.StemContentHash.Should().Be(snapshotBefore.StemContentHash);
        snapshotAfter.PosTagContentHash.Should().Be(snapshotBefore.PosTagContentHash);
    }

    [Fact]
    public async Task Source_Changed_After_Load_Fails_With_Report_And_No_Persisted_State()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"morph-report-source-{Guid.NewGuid():N}");
        var readableCount = fixture.GetReadableWordCount();

        try
        {
            await using var scope = fixture.CreateScope(services =>
            {
                services.AddScoped<SourceChangedAfterLoadMorphologyImportSource>();
                services.AddScoped<IMorphologyImportSource>(sp =>
                    sp.GetRequiredService<SourceChangedAfterLoadMorphologyImportSource>());
            });

            var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();
            var result = await handler.HandleAsync(
                new ImportMorphologyCommand(sourcePath, false, readableCount, reportDir),
                CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
            result.ReportOutDir.Should().Be(reportDir);

            File.Exists(Path.Combine(reportDir, "morphology-import-report.md")).Should().BeTrue();
            File.Exists(Path.Combine(reportDir, "morphology-import-report.json")).Should().BeTrue();

            var jsonPath = Path.Combine(reportDir, "morphology-import-report.json");
            await using var jsonStream = File.OpenRead(jsonPath);
            var report = await JsonSerializer.DeserializeAsync<JsonElement>(jsonStream);

            report.GetProperty("verdict").GetString().Should().Be("fail");
            report.GetProperty("persisted").GetBoolean().Should().BeFalse();

            var checks = report.GetProperty("checks");
            var sourceCheck = checks.EnumerateArray()
                .Single(check => check.GetProperty("id").GetString() == MorphologyInvariants.CheckSourceUnchanged);
            sourceCheck.GetProperty("passed").GetBoolean().Should().BeFalse();
            sourceCheck.GetProperty("severity").GetString().Should().Be("hard");

            var warnings = report.GetProperty("warnings").EnumerateArray()
                .Select(warning => warning.GetString())
                .ToList();
            warnings.Should().Contain(warning =>
                warning!.StartsWith($"{MorphologyInvariants.CheckDimCounts}:", StringComparison.Ordinal));

            var snapshot = await fixture.CaptureTableSnapshotAsync();
            snapshot.MorphologyRows.Should().Be(0);
            snapshot.MorphologyContentHash.Should().Be(EmptyTableContentHash);
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, true);
            }
        }
    }

    [Fact]
    public async Task Import_fails_when_active_artifact_has_pending_entries()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"morph-report-pending-{Guid.NewGuid():N}");
        var readableCount = fixture.GetReadableWordCount();

        try
        {
            await using var scope = fixture.CreateScope(services =>
            {
                services.AddSingleton<IWordLemmaNormalizationReader>(new PendingWordLemmaNormalizationReader());
            });

            var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();
            var result = await handler.HandleAsync(
                new ImportMorphologyCommand(sourcePath, false, readableCount, reportDir),
                CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
            result.ReportOutDir.Should().Be(reportDir);

            var jsonPath = Path.Combine(reportDir, "morphology-import-report.json");
            File.Exists(jsonPath).Should().BeTrue();

            await using var jsonStream = File.OpenRead(jsonPath);
            var report = await JsonSerializer.DeserializeAsync<JsonElement>(jsonStream);
            var check = report.GetProperty("checks").EnumerateArray()
                .Single(item => item.GetProperty("id").GetString() == MorphologyInvariants.CheckWordLemmaUncertainZero);

            check.GetProperty("passed").GetBoolean().Should().BeFalse();
            check.GetProperty("severity").GetString().Should().Be("hard");
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, true);
            }
        }
    }

    [Fact]
    public async Task Import_fails_when_normalization_summary_lies_about_applied_entries()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"morph-report-applied-{Guid.NewGuid():N}");
        var readableCount = fixture.GetReadableWordCount();

        try
        {
            await using var scope = fixture.CreateScope(services =>
            {
                services.AddSingleton<IWordLemmaNormalizationReader>(new BrokenSummaryWordLemmaNormalizationReader());
            });

            var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();
            var result = await handler.HandleAsync(
                new ImportMorphologyCommand(sourcePath, false, readableCount, reportDir),
                CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);

            var jsonPath = Path.Combine(reportDir, "morphology-import-report.json");
            await using var jsonStream = File.OpenRead(jsonPath);
            var report = await JsonSerializer.DeserializeAsync<JsonElement>(jsonStream);

            var checks = report.GetProperty("checks").EnumerateArray().ToList();
            checks.Should().Contain(check =>
                check.GetProperty("id").GetString() == MorphologyInvariants.CheckWordLemmaNormalizationApplied
                && !check.GetProperty("passed").GetBoolean());
            checks.Should().Contain(check =>
                check.GetProperty("id").GetString() == MorphologyInvariants.CheckWordLemmaReplaceValid
                && !check.GetProperty("passed").GetBoolean());
            checks.Should().Contain(check =>
                check.GetProperty("id").GetString() == MorphologyInvariants.CheckWordLemmaMissingRecoveryClean
                && !check.GetProperty("passed").GetBoolean());
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, true);
            }
        }
    }

    [Fact]
    public async Task Strict_shift_check_fails_when_unapproved_shift_row_exists()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchQulLemmaMapAsync(sourcePath, CreateLemmaMapWithRepeatedRabb());
        await fixture.PatchCorpusSegmentsAsync(sourcePath, "1:1:1", PreviousWordSegmentsWithRabbLemma());
        await fixture.PatchCorpusSegmentsAsync(sourcePath, "1:1:2", CurrentWordSegmentsWithoutLemmaEvidence());

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(MorphologyInvariants.CheckWordLemmaShiftClean);

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.MorphologyRows.Should().Be(0);
        snapshot.SegmentRows.Should().Be(0);
        snapshot.LemmaRows.Should().Be(0);
    }

    [Fact]
    public async Task Strict_shift_check_allows_adjacent_repeated_lemma_with_current_stem_evidence()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchQulLemmaMapAsync(sourcePath, CreateLemmaMapWithRepeatedRabb());
        await fixture.PatchCorpusSegmentsAsync(sourcePath, "1:1:1", PreviousWordSegmentsWithRabbLemma());
        await fixture.PatchCorpusSegmentsAsync(sourcePath, "1:1:2", CurrentWordSegmentsWithRabbLemma());

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.Succeeded.Should().BeTrue();

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.MorphologyRows.Should().Be(fixture.GetReadableWordCount());
    }

    private static Dictionary<string, string> CreateLemmaMapWithRepeatedRabb()
    {
        var lemmas = MorphologySyntheticSeed.GetLemmaMap();
        lemmas["1:1:1"] = MorphologySyntheticSeed.LemmaValue2;
        lemmas["1:1:2"] = MorphologySyntheticSeed.LemmaValue2;
        return lemmas;
    }

    private static IReadOnlyList<object> PreviousWordSegmentsWithRabbLemma() =>
    [
        new { segmentNumber = (short)1, kind = "PREFIX", pos = "P", form = "bi", features = "PREFIX", root = (string?)null, lemma = (string?)null },
        new { segmentNumber = (short)2, kind = "STEM", pos = "N", form = "raboni", features = "NOM", root = (string?)null, lemma = "rabb" }
    ];

    private static IReadOnlyList<object> CurrentWordSegmentsWithoutLemmaEvidence() =>
    [
        new { segmentNumber = (short)1, kind = "STEM", pos = "N", form = "l~Ahi", features = "GEN", root = (string?)null, lemma = (string?)null },
        new { segmentNumber = (short)2, kind = "STEM", pos = "N", form = "somi", features = "GEN", root = (string?)null, lemma = (string?)null }
    ];

    private static IReadOnlyList<object> CurrentWordSegmentsWithRabbLemma() =>
    [
        new { segmentNumber = (short)1, kind = "STEM", pos = "N", form = "raboni", features = "GEN", root = (string?)null, lemma = "rabb" }
    ];

    [Fact]
    public async Task Forced_Run_With_Source_Failure_Leaves_Previous_Unchanged()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var first = await fixture.RunImportAsync(sourcePath, force: false, expectedReadableWords: readableCount);
        first.Succeeded.Should().BeTrue();

        var snapshotBefore = await fixture.CaptureTableSnapshotAsync();

        var badSource = await fixture.WriteSourceFolderWithChecksumMismatchAsync();

        var forced = await fixture.RunImportAsync(badSource, force: true);
        forced.Succeeded.Should().BeFalse();
        forced.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        forced.ReportOutDir.Should().BeNull();

        var snapshotAfter = await fixture.CaptureTableSnapshotAsync();

        snapshotAfter.MorphologyRows.Should().Be(snapshotBefore.MorphologyRows);
        snapshotAfter.SegmentRows.Should().Be(snapshotBefore.SegmentRows);
        snapshotAfter.PosTagRows.Should().Be(snapshotBefore.PosTagRows);
        snapshotAfter.QuranWordRows.Should().Be(snapshotBefore.QuranWordRows);
        snapshotAfter.MorphologyContentHash.Should().Be(snapshotBefore.MorphologyContentHash);
        snapshotAfter.SegmentContentHash.Should().Be(snapshotBefore.SegmentContentHash);
        snapshotAfter.RootContentHash.Should().Be(snapshotBefore.RootContentHash);
        snapshotAfter.LemmaContentHash.Should().Be(snapshotBefore.LemmaContentHash);
        snapshotAfter.StemContentHash.Should().Be(snapshotBefore.StemContentHash);
        snapshotAfter.PosTagContentHash.Should().Be(snapshotBefore.PosTagContentHash);
    }
}

internal sealed class SourceChangedAfterLoadMorphologyImportSource : IMorphologyImportSource
{
    private readonly MorphologyImportSource inner;

    public SourceChangedAfterLoadMorphologyImportSource(MorphologyImportSource inner)
    {
        this.inner = inner;
    }

    public Task<MorphologySourceData> LoadAsync(string sourcePath, CancellationToken ct) =>
        inner.LoadAsync(sourcePath, ct);

    public Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct) =>
        Task.FromResult(false);
}

internal sealed class PendingWordLemmaNormalizationReader : IWordLemmaNormalizationReader
{
    public WordLemmaNormalizationLoaded Load() => new(
        new WordLemmaNormalizationArtifact
        {
            SchemaVersion = WordLemmaNormalizationArtifact.SupportedSchemaVersion,
            ArtifactId = "pending-normalization-artifact",
            Entries = []
        },
        [],
        "1".PadRight(64, '0'),
        new WordLemmaNormalizationCounts(1, 0, 0, 0, 0, 0, 0, 0, 1));

    public WordLemmaNormalizationResult Apply(
        IReadOnlyDictionary<string, string> rawLemmas,
        WordLemmaNormalizationLoaded loaded,
        IReadOnlySet<string>? readableWordLocations = null,
        string? rawLemmasSha256 = null)
    {
        var summary = new WordLemmaCorrectionSummary(
            ArtifactSha256: loaded.ArtifactSha256,
            RawLemmasSha256: rawLemmasSha256,
            TotalEntries: 0,
            AppliedAdd: 0,
            AppliedRemove: 0,
            AppliedReplace: 0,
            ReviewedKeep: 0,
            ReviewedException: 0,
            FailedOrSkipped: 0,
            SpotChecks: []);

        return new WordLemmaNormalizationResult(new Dictionary<string, string>(rawLemmas, StringComparer.Ordinal), summary);
    }
}

internal sealed class BrokenSummaryWordLemmaNormalizationReader : IWordLemmaNormalizationReader
{
    public WordLemmaNormalizationLoaded Load() => new(
        new WordLemmaNormalizationArtifact
        {
            SchemaVersion = WordLemmaNormalizationArtifact.SupportedSchemaVersion,
            ArtifactId = "broken-summary-normalization-artifact",
            Entries =
            [
                new WordLemmaNormalizationEntry
                {
                    Id = "WLN-BROKEN-ADD",
                    Location = "1:1:2",
                    OperationKind = WordLemmaNormalizationOperationKind.Add,
                    CorrectedLemmaArabic = "جَدِيد",
                    DecisionStatus = WordLemmaNormalizationDecisionStatus.Approved,
                    ProblemClass = "missing-recovery",
                    Reason = "test"
                },
                new WordLemmaNormalizationEntry
                {
                    Id = "WLN-BROKEN-REMOVE",
                    Location = "1:1:3",
                    OperationKind = WordLemmaNormalizationOperationKind.Remove,
                    ExpectedCurrentLemmaArabic = MorphologySyntheticSeed.LemmaValue2,
                    DecisionStatus = WordLemmaNormalizationDecisionStatus.Approved,
                    ProblemClass = "shift-63",
                    Reason = "test"
                },
                new WordLemmaNormalizationEntry
                {
                    Id = "WLN-BROKEN-REPLACE",
                    Location = "1:2:2",
                    OperationKind = WordLemmaNormalizationOperationKind.Replace,
                    ExpectedCurrentLemmaArabic = "كِتَاب",
                    CorrectedLemmaArabic = "كِتَابٌ",
                    DecisionStatus = WordLemmaNormalizationDecisionStatus.Approved,
                    ProblemClass = "shift-63-replace",
                    Reason = "test"
                }
            ]
        },
        [],
        "3".PadRight(64, '0'),
        new WordLemmaNormalizationCounts(3, 1, 1, 1, 0, 0, 3, 0, 0));

    public WordLemmaNormalizationResult Apply(
        IReadOnlyDictionary<string, string> rawLemmas,
        WordLemmaNormalizationLoaded loaded,
        IReadOnlySet<string>? readableWordLocations = null,
        string? rawLemmasSha256 = null)
    {
        var summary = new WordLemmaCorrectionSummary(
            ArtifactSha256: loaded.ArtifactSha256,
            RawLemmasSha256: rawLemmasSha256,
            TotalEntries: 3,
            AppliedAdd: 1,
            AppliedRemove: 1,
            AppliedReplace: 1,
            ReviewedKeep: 0,
            ReviewedException: 0,
            FailedOrSkipped: 0,
            SpotChecks: [])
        {
            ProblemClassCounts = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["shift-63"] = 1,
                ["shift-63-replace"] = 1,
                ["missing-recovery"] = 1,
            },
        };

        return new WordLemmaNormalizationResult(new Dictionary<string, string>(rawLemmas, StringComparer.Ordinal), summary);
    }
}
