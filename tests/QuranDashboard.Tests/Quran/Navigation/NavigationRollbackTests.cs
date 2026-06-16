using QuranDashboard.Application.Abstractions.Quran.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

[Collection(nameof(NavigationImportTestCollection))]
public sealed class NavigationRollbackTests(NavigationImportTestFixture fixture)
{
    [Fact]
    public async Task Source_changed_after_load_rolls_back_all_navigation_writes()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportWithTamperAfterLoadAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            tamper: path => File.AppendAllText(
                Path.Combine(path, "sources", "quran-metadata-juz.json"),
                "TAMPERED"),
            reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(NavigationMetadataInvariants.CheckSourceUnchanged);

        var snapshot = await fixture.CaptureNavigationSnapshotAsync();
        snapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 0));

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var ayahs = await dbContext.QuranAyahs.AsNoTracking().OrderBy(ayah => ayah.Id).ToListAsync();
        ayahs.Should().OnlyContain(ayah =>
            ayah.JuzNumber == null && ayah.HizbNumber == null && ayah.RubNumber == null);
    }

    [Fact]
    public async Task Validation_failure_before_commit_leaves_navigation_tables_empty()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            NavigationSyntheticSeed.JuzGapPackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();

        var snapshot = await fixture.CaptureNavigationSnapshotAsync();
        snapshot.Should().Be(new NavigationTableSnapshot(0, 0, 0, 0, 0));
    }
}
