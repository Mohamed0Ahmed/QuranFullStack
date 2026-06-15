using QuranDashboard.Application.Abstractions.Quran.Tafsirs;
using QuranDashboard.Application.Quran.Tafsirs.ImportTafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirRefusalForceTests(TafsirImportTestFixture fixture)
{
    [Fact]
    public async Task Second_run_without_force_refuses_when_tafsir_tables_contain_data()
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

        var beforeSnapshot = await fixture.CaptureTafsirTableSnapshotAsync();

        var secondRun = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts,
            reportDir,
            force: false);

        secondRun.Succeeded.Should().BeFalse();
        secondRun.ExitCode.Should().Be(ImportTafsirsResult.RefusedExitCode);
        secondRun.Message.Should().Be(TafsirInvariants.TargetsNotEmpty);
        secondRun.Message.Should().Contain("--force");

        var afterSnapshot = await fixture.CaptureTafsirTableSnapshotAsync();
        afterSnapshot.Should().Be(beforeSnapshot);
    }

    [Fact]
    public async Task Refusal_occurs_when_any_tafsir_target_table_contains_rows()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.IntegrationSources);

        var firstRun = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts);

        firstRun.Succeeded.Should().BeTrue(firstRun.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<ITafsirImportWriter>();

        (await writer.AnyTargetTableHasDataAsync(CancellationToken.None)).Should().BeTrue();
    }
}
