using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class MorphologyUs1GateTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public async Task Import_fails_us1_gate_when_verb_has_multiple_tense_markers()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchCorpusStemFeaturesAsync(sourcePath, "1:2:1", "PERF IMPF");

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        result.Message.Should().Contain("MORPH-VERB-FEATURE-CONSISTENCY");

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await dbContext.WordMorphologies.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Import_fails_us1_gate_when_head_verb_has_no_tense_marker()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchCorpusStemFeaturesAsync(sourcePath, "1:2:1", "STEM|POS:V|LEM:kataba|ROOT:ktb");

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        result.Message.Should().Contain("MORPH-VERB-FEATURE-CONSISTENCY");

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await dbContext.WordMorphologies.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Import_fails_us1_gate_when_readable_word_has_no_stem_segment()
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
                    kind = "PREFIX",
                    pos = "P",
                    form = "TSTPX",
                    features = "PREFIX",
                    root = (string?)null,
                    lemma = (string?)null
                }
            ]);

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        result.Message.Should().Contain("MORPH-POS-PRESENT");

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await dbContext.WordMorphologies.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Import_accepts_multi_stem_word_and_uses_first_stem_as_head()
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
                    pos = "P",
                    form = "mi",
                    features = "STEM|POS:P|LEM:min",
                    root = (string?)null,
                    lemma = "min"
                },
                new
                {
                    segmentNumber = (short)2,
                    kind = "STEM",
                    pos = "REL",
                    form = "kataba",
                    features = "STEM|POS:REL|LEM:katab",
                    root = (string?)null,
                    lemma = "katab"
                }
            ]);

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var row = await dbContext.WordMorphologies
            .AsNoTracking()
            .SingleAsync(m => m.Location == "1:1:3");
        row.HeadPos.Should().Be("P");
        row.IsVerb.Should().BeFalse();
        row.VerbTense.Should().BeNull();
        row.VerbVoice.Should().BeNull();

        var stems = await dbContext.WordMorphologySegments
            .AsNoTracking()
            .Where(s => s.QuranWordId == row.QuranWordId && s.Kind == "STEM")
            .OrderBy(s => s.SegmentNumber)
            .ToListAsync();
        stems.Should().HaveCount(2, "multi-STEM words keep all source segments");
        stems[0].Pos.Should().Be("P");
        stems[1].Pos.Should().Be("REL");

        var jsonPath = Path.Combine(result.ReportOutDir!, "morphology-import-report.json");
        await using var reportStream = File.OpenRead(jsonPath);
        var report = await System.Text.Json.JsonSerializer.DeserializeAsync<System.Text.Json.JsonElement>(
            reportStream);
        var warnings = report.GetProperty("warnings").EnumerateArray()
            .Select(warning => warning.GetString())
            .ToList();
        warnings.Should().Contain(warning =>
            warning!.StartsWith("MORPH-MULTI-STEM-LIST: 1 multi-STEM word(s)", StringComparison.Ordinal)
            && warning.Contains("P+REL=1", StringComparison.Ordinal)
            && warning.Contains("multi-stem-words-report.md", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Import_refuses_extra_corpus_location_before_targets_not_empty_check()
    {
        await fixture.SeedSyntheticWordsAsync();
        var goodSourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var expectedReadableCount = fixture.GetReadableWordCount();

        var firstRun = await fixture.RunImportAsync(
            goodSourcePath,
            expectedReadableWords: expectedReadableCount);
        firstRun.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        var badSourcePath = await fixture.WriteSourceFolderWithExtraCorpusLocationAsync();
        var secondRun = await fixture.RunImportAsync(
            badSourcePath,
            expectedReadableWords: expectedReadableCount);

        secondRun.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        secondRun.Message.Should().Contain("Aligned corpus contains locations");
        secondRun.Message.Should().NotBe(MorphologyInvariants.TargetsNotEmpty);
    }
}
