using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Application.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirReportWriteFailureTests(TafsirImportTestFixture fixture)
{
    [Fact]
    public async Task Report_write_failure_after_validation_rolls_back_without_accepted_tafsir_changes()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.IntegrationSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"tafsir-report-block-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(reportDir, "blocking-file");

        var beforeSnapshot = await fixture.CaptureTafsirTableSnapshotAsync();

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(ImportTafsirsResult.FailureExitCode);
        result.Message.Should().StartWith(TafsirInvariants.ReportRequired);
        result.Message
            .Split(TafsirInvariants.ReportRequired, StringSplitOptions.None)
            .Should()
            .HaveCount(2, "ReportRequired should appear exactly once in the operator message");

        var afterSnapshot = await fixture.CaptureTafsirTableSnapshotAsync();
        afterSnapshot.Should().Be(beforeSnapshot);
    }
}
