using FluentAssertions;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;
using QuranDashboard.Application.Quran.Words.ImportMorphology;

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

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
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

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await dbContext.WordMorphologies.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Import_fails_us1_gate_when_readable_word_has_duplicate_stem_segments()
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
                    form = "TSTSC",
                    features = "GEN",
                    root = "TSTRC",
                    lemma = "TSTLC"
                },
                new
                {
                    segmentNumber = (short)2,
                    kind = "STEM",
                    pos = "PN",
                    form = "TSTSD",
                    features = "GEN",
                    root = "TSTRC",
                    lemma = "TSTLC"
                }
            ]);

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        result.Message.Should().Contain("MORPH-POS-PRESENT");

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await dbContext.WordMorphologies.CountAsync()).Should().Be(0);
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

        secondRun.ExitCode.Should().Be(ImportMorphologyResult.RefusedExitCode);
        secondRun.Message.Should().Be(MorphologyInvariants.SourceMismatch);
        secondRun.Message.Should().NotBe(MorphologyInvariants.TargetsNotEmpty);
    }
}
