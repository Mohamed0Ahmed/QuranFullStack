using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Application.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Tests.Quran.FullI3rab;

[Collection(nameof(FullI3rabImportTestCollection))]
public sealed class FullI3rabRefusalForceTests(FullI3rabImportTestFixture fixture) : IDisposable
{
    private readonly FullI3rabSyntheticPackage packages = new();

    [Fact]
    public async Task Second_run_without_force_refuses_when_full_i3rab_tables_contain_data()
    {
        await fixture.TruncateFullI3rabTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(FullI3rabSyntheticSeed.DefaultAyahs);

        var packageDir = await packages.WriteAsync();
        var reportDir = fixture.CreateTempDir();

        var firstRun = await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3),
            reportDir);

        firstRun.Succeeded.Should().BeTrue(firstRun.Message);

        var beforeSnapshot = await fixture.CaptureFullI3rabTableSnapshotAsync();

        var secondRun = await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3),
            reportDir,
            force: false);

        secondRun.Succeeded.Should().BeFalse();
        secondRun.ExitCode.Should().Be(ImportFullI3rabResult.RefusedExitCode);
        secondRun.Message.Should().Be(FullI3rabInvariants.TargetsNotEmpty);
        secondRun.Message.Should().Contain("--force");

        var afterSnapshot = await fixture.CaptureFullI3rabTableSnapshotAsync();
        afterSnapshot.Should().Be(beforeSnapshot);
    }

    [Fact]
    public async Task Refusal_occurs_when_any_full_i3rab_target_table_contains_rows()
    {
        await fixture.TruncateFullI3rabTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(FullI3rabSyntheticSeed.DefaultAyahs);

        var packageDir = await packages.WriteAsync();

        var firstRun = await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3));

        firstRun.Succeeded.Should().BeTrue(firstRun.Message);

        await using var scope = fixture.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<IFullI3rabImportWriter>();

        (await writer.AnyTargetTableHasDataAsync(CancellationToken.None)).Should().BeTrue();
    }

    public void Dispose() => packages.Dispose();
}
