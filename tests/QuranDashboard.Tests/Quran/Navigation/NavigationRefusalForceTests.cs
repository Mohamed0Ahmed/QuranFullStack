using QuranDashboard.Application.Abstractions.Quran.Navigation;
using QuranDashboard.Application.Quran.Navigation.ImportNavigationMetadata;

namespace QuranDashboard.Tests.Quran.Navigation;

[Collection(nameof(NavigationImportTestCollection))]
public sealed class NavigationRefusalForceTests(NavigationImportTestFixture fixture)
{
    [Fact]
    public async Task Normal_rerun_refuses_when_navigation_target_tables_contain_data()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var firstReportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");
        var secondReportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var firstResult = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            firstReportDir);
        firstResult.Succeeded.Should().BeTrue(firstResult.Message);

        var snapshotBefore = await fixture.CaptureNavigationSnapshotAsync();
        snapshotBefore.JuzRows.Should().BeGreaterThan(0);
        snapshotBefore.TaggedAyahRows.Should().Be(6);

        var secondResult = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            secondReportDir);

        secondResult.Succeeded.Should().BeFalse();
        secondResult.ExitCode.Should().Be(ImportNavigationMetadataResult.RefusedExitCode);
        secondResult.Message.Should().Contain(NavigationMetadataInvariants.TargetsNotEmpty);

        var snapshotAfter = await fixture.CaptureNavigationSnapshotAsync();
        snapshotAfter.Should().Be(snapshotBefore);

        using var reportDocument = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(secondReportDir, NavigationImportConstants.JsonReportFileName)));
        var checks = reportDocument.RootElement.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == NavigationMetadataInvariants.CheckRerunGuard
            && !check.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public async Task Rerun_guard_detects_and_refuses_when_only_ayah_navigation_columns_are_populated()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);
        await fixture.SeedOrphanedAyahNavigationColumnsAsync();

        var navSnapshot = await fixture.CaptureNavigationSnapshotAsync();
        navSnapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 1));

        await using (var scope = fixture.CreateServiceProvider().CreateAsyncScope())
        {
            var writer = scope.ServiceProvider.GetRequiredService<INavigationMetadataImportWriter>();
            (await writer.AnyTargetTableHasDataAsync(CancellationToken.None)).Should().BeTrue();
        }

        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(ImportNavigationMetadataResult.RefusedExitCode);
        result.Message.Should().Contain(NavigationMetadataInvariants.TargetsNotEmpty);

        (await fixture.CaptureNavigationSnapshotAsync()).Should().Be(navSnapshot);
    }

    [Fact]
    public async Task Forced_replacement_rebuilds_navigation_data_to_match_fresh_import()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var firstReportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");
        var secondReportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var firstResult = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            firstReportDir);
        firstResult.Succeeded.Should().BeTrue(firstResult.Message);

        var snapshotAfterFirstImport = await fixture.CaptureNavigationDataSnapshotAsync();

        var secondResult = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            secondReportDir,
            force: true);
        secondResult.Succeeded.Should().BeTrue(secondResult.Message);

        var snapshotAfterForce = await fixture.CaptureNavigationDataSnapshotAsync();
        snapshotAfterForce.Should().BeEquivalentTo(snapshotAfterFirstImport);
    }
}
