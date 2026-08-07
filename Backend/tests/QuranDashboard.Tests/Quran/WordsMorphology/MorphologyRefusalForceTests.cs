using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class MorphologyRefusalForceTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public async Task Second_Run_Without_Force_Refuses_And_Writes_Nothing()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var first = await fixture.RunImportAsync(sourcePath, force: false, expectedReadableWords: readableCount);
        first.Succeeded.Should().BeTrue();
        first.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        var second = await fixture.RunImportAsync(sourcePath, force: false, expectedReadableWords: readableCount);
        second.Succeeded.Should().BeFalse();
        second.ExitCode.Should().Be(ImportMorphologyResult.RefusedExitCode);
        second.Message.Should().Be(MorphologyInvariants.TargetsNotEmpty);
        second.ReportOutDir.Should().BeNull();

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.MorphologyRows.Should().Be(readableCount);
    }

    [Fact]
    public async Task Missing_Foundation_Refuses_Cleanly_No_Report()
    {
        await fixture.SeedEmptyFoundationAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var result = await fixture.RunImportAsync(sourcePath, force: false);
        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(ImportMorphologyResult.RefusedExitCode);
        result.Message.Should().Be(MorphologyInvariants.FoundationNotLoaded);
        result.ReportOutDir.Should().BeNull();
    }

    [Fact]
    public async Task Force_Truncates_And_Rebuilds_Only_Morphology_Tables()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var first = await fixture.RunImportAsync(sourcePath, force: false, expectedReadableWords: readableCount);
        first.Succeeded.Should().BeTrue();

        var snapshotBefore = await fixture.CaptureTableSnapshotAsync();

        var forced = await fixture.RunImportAsync(sourcePath, force: true, expectedReadableWords: readableCount);
        forced.Succeeded.Should().BeTrue();
        forced.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        var snapshotAfter = await fixture.CaptureTableSnapshotAsync();
        snapshotAfter.QuranWordRows.Should().Be(snapshotBefore.QuranWordRows);
        snapshotAfter.MorphologyRows.Should().Be(readableCount);
        snapshotAfter.PosTagRows.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Force_Rerun_Idempotent_Stable_Snapshots()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var first = await fixture.RunImportAsync(sourcePath, force: false, expectedReadableWords: readableCount);
        first.Succeeded.Should().BeTrue();

        var snapshotBefore = await fixture.CaptureTableSnapshotAsync();

        var forced = await fixture.RunImportAsync(sourcePath, force: true, expectedReadableWords: readableCount);
        forced.Succeeded.Should().BeTrue();

        var snapshotAfter = await fixture.CaptureTableSnapshotAsync();

        snapshotAfter.MorphologyContentHash.Should().Be(snapshotBefore.MorphologyContentHash);
        snapshotAfter.SegmentContentHash.Should().Be(snapshotBefore.SegmentContentHash);
        snapshotAfter.PosTagContentHash.Should().Be(snapshotBefore.PosTagContentHash);
        snapshotAfter.QuranWordRows.Should().Be(snapshotBefore.QuranWordRows);
    }

    [Fact]
    public async Task Quran_Words_Unchanged_After_Run()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var wordCountBefore = fixture.GetReadableWordCount();

        await fixture.RunImportAsync(sourcePath, force: false, expectedReadableWords: readableCount);

        var wordCountAfter = fixture.GetReadableWordCount();
        wordCountAfter.Should().Be(wordCountBefore);
    }

    [Fact]
    public async Task Source_Files_Unchanged_After_Run()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var readableCount = fixture.GetReadableWordCount();

        var digestsBefore = await fixture.CaptureSourceDigestsAsync(sourcePath);

        await fixture.RunImportAsync(sourcePath, force: false, expectedReadableWords: readableCount);

        var unchanged = await fixture.AreSourceFilesUnchangedAsync(sourcePath, digestsBefore);
        unchanged.Should().BeTrue();
    }

    [Fact]
    public async Task Source_Mismatch_Fails_Early_No_Report_With_Specific_Message()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSourceFolderWithChecksumMismatchAsync();

        var result = await fixture.RunImportAsync(sourcePath, force: false);
        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(ImportMorphologyResult.FailureExitCode);
        result.Message.Should().Contain("Checksum mismatch");
        result.ReportOutDir.Should().BeNull();
    }

    [Fact]
    public async Task Report_Written_On_Success()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"morph-report-{Guid.NewGuid():N}");
        var readableCount = fixture.GetReadableWordCount();

        try
        {
            await using var scope = fixture.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();
            var result = await handler.HandleAsync(
                new ImportMorphologyCommand(sourcePath, false, readableCount, reportDir),
                CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.ReportOutDir.Should().Be(reportDir);
            File.Exists(Path.Combine(reportDir, "morphology-import-report.md")).Should().BeTrue();
            File.Exists(Path.Combine(reportDir, "morphology-import-report.json")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, true);
            }
        }
    }
}
