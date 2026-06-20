using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Application.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirRollbackTests(TafsirImportTestFixture fixture)
{
    [Fact]
    public async Task Force_rebuild_rolls_back_when_post_copy_validation_fails()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.IntegrationSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"tafsir-report-{Guid.NewGuid():N}");

        var firstRun = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        firstRun.Succeeded.Should().BeTrue(firstRun.Message);

        var tafsirBeforeFailedForce = await CaptureTafsirContentFingerprintAsync(fixture);
        var foundationBeforeFailedForce = await fixture.CaptureQuranFoundationSnapshotAsync();

        var wrongExpectedCounts = TafsirSyntheticSeed.DefaultTestExpectedCounts with
        {
            SourceAyahMappings = 99
        };

        var failedForceRun = await fixture.RunImportAsync(
            packageDir,
            wrongExpectedCounts,
            reportDir,
            force: true);

        failedForceRun.Succeeded.Should().BeFalse();
        failedForceRun.ExitCode.Should().Be(ImportTafsirsResult.FailureExitCode);
        failedForceRun.Message.Should().Contain(TafsirInvariants.CheckPostCopyAyahMappings);

        var tafsirAfterFailedForce = await CaptureTafsirContentFingerprintAsync(fixture);
        tafsirAfterFailedForce.Should().Be(tafsirBeforeFailedForce);

        var foundationAfterFailedForce = await fixture.CaptureQuranFoundationSnapshotAsync();
        foundationAfterFailedForce.Should().Be(foundationBeforeFailedForce);

        var snapshot = await fixture.CaptureTafsirTableSnapshotAsync();
        snapshot.SourceRows.Should().Be(1);
        snapshot.EntryRows.Should().Be(2);
        snapshot.AyahLinkRows.Should().Be(3);

        var reportJson = await File.ReadAllTextAsync(
            Path.Combine(reportDir, TafsirImportConstants.JsonReportFileName));
        using var reportDocument = JsonDocument.Parse(reportJson);

        reportDocument.RootElement.GetProperty("persisted").GetBoolean().Should().BeFalse();
        reportDocument.RootElement.GetProperty("forced").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Force_with_wrong_source_count_refuses_before_write_without_changing_tafsir_data()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.IntegrationSources);

        var firstRun = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts);

        firstRun.Succeeded.Should().BeTrue(firstRun.Message);

        var beforeSnapshot = await fixture.CaptureTafsirTableSnapshotAsync();

        var wrongExpectedCounts = TafsirSyntheticSeed.DefaultTestExpectedCounts with
        {
            ApprovedSources = 99
        };

        var failedForceRun = await fixture.RunImportAsync(
            packageDir,
            wrongExpectedCounts,
            force: true);

        failedForceRun.Succeeded.Should().BeFalse();
        failedForceRun.Message.Should().Contain(TafsirInvariants.CheckSourceCount);

        var afterSnapshot = await fixture.CaptureTafsirTableSnapshotAsync();
        afterSnapshot.Should().Be(beforeSnapshot);
    }

    private static async Task<string> CaptureTafsirContentFingerprintAsync(TafsirImportTestFixture fixture)
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var rows = await dbContext.TafsirEntries
            .AsNoTracking()
            .Join(
                dbContext.TafsirSources.AsNoTracking(),
                entry => entry.SourceId,
                source => source.Id,
                (entry, source) => new { source.SourceKey, entry.SourceEntryKey, entry.TafsirText, entry.TextHash })
            .OrderBy(row => row.SourceKey)
            .ThenBy(row => row.SourceEntryKey)
            .ToListAsync();

        return string.Join(
            '|',
            rows.Select(row => $"{row.SourceKey}:{row.SourceEntryKey}:{row.TextHash}:{row.TafsirText}"));
    }
}
