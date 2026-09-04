using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Application.Quran.DataPipelines.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

[Collection(nameof(NavigationImportTestCollection))]
public sealed class NavigationSourcePathTests(NavigationImportTestFixture fixture)
{
    [Fact]
    public async Task Explicit_source_path_reads_custom_package_root()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var customPackageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            customPackageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);

        using var reportDocument = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName)));
        reportDocument.RootElement.GetProperty("sourcePath").GetString().Should().Be(Path.GetFullPath(customPackageDir));
    }

    [Fact]
    public async Task Missing_source_directory_refuses_with_no_writes()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var missingSourcePath = Path.Combine(Path.GetTempPath(), $"missing-navigation-package-{Guid.NewGuid():N}");
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            missingSourcePath,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(NavigationMetadataInvariants.SourceMismatch);

        var snapshot = await fixture.CaptureNavigationSnapshotAsync();
        snapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 0));
    }
}
