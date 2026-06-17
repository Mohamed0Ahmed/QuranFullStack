using QuranDashboard.Application.Abstractions.Quran.FullI3rab;

namespace QuranDashboard.Tests.Quran.FullI3rab;

[Collection(nameof(FullI3rabImportTestCollection))]
public sealed class FullI3rabSourceUnchangedTests(FullI3rabImportTestFixture fixture) : IDisposable
{
    private readonly FullI3rabSyntheticPackage packages = new();

    [Fact]
    public async Task Import_fails_when_source_files_change_after_load_and_before_commit()
    {
        await fixture.TruncateFullI3rabTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(FullI3rabSyntheticSeed.DefaultAyahs);

        var packageDir = await packages.WriteAsync();
        var expectedCounts = new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var importSource = scope.ServiceProvider.GetRequiredService<IFullI3rabImportSource>();
        var writer = scope.ServiceProvider.GetRequiredService<IFullI3rabImportWriter>();

        var source = await importSource.LoadAsync(packageDir, expectedCounts, CancellationToken.None);

        await File.AppendAllTextAsync(
            packages.GetSourceFilePath(packageDir, "test-muyassar"),
            " ");

        var result = await writer.ExecuteAcceptedImportAsync(
            source,
            force: false,
            expectedCounts,
            token => importSource.SourceUnchangedAsync(packageDir, token),
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        result.Persisted.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.Contains(FullI3rabInvariants.CheckPostCopySourceUnchanged, StringComparison.Ordinal));

        var snapshot = await fixture.CaptureFullI3rabTableSnapshotAsync();
        snapshot.SourceRows.Should().Be(0);
        snapshot.EntryRows.Should().Be(0);
        snapshot.AyahLinkRows.Should().Be(0);
    }

    public void Dispose() => packages.Dispose();
}
