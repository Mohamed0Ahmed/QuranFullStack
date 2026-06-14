using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirSourceSafetyTests(TafsirImportTestFixture fixture)
{
    [Fact]
    public async Task Failed_import_does_not_modify_package_source_files()
    {
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources:
            [
                TafsirSyntheticSeed.IntegrationSources[0] with { SourceKey = TafsirInvariants.LockedExcludedSourceKeys[0] }
            ]);

        var beforeHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var relativePath in Directory.EnumerateFiles(packageDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(packageDir, relativePath).Replace('\\', '/');
            beforeHashes[relative] = await fixture.ComputePackageFileSha256Async(packageDir, relative);
        }

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts);

        result.Succeeded.Should().BeFalse();

        foreach (var (relative, beforeHash) in beforeHashes)
        {
            var afterHash = await fixture.ComputePackageFileSha256Async(packageDir, relative);
            afterHash.Should().Be(beforeHash, because: $"package file '{relative}' must remain unchanged");
        }
    }

    [Fact]
    public async Task Failed_import_does_not_insert_update_or_delete_quran_foundation_ayahs()
    {
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);
        var before = await fixture.CaptureQuranFoundationSnapshotAsync();

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources:
            [
                TafsirSyntheticSeed.IntegrationSources[0] with { SourceKey = TafsirInvariants.LockedExcludedSourceKeys[1] }
            ]);

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts);

        result.Succeeded.Should().BeFalse();

        var after = await fixture.CaptureQuranFoundationSnapshotAsync();
        after.Should().Be(before);
    }

    [Fact]
    public async Task Failed_import_leaves_tafsir_tables_empty()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources:
            [
                TafsirSyntheticSeed.IntegrationSources[0] with { SourceKey = TafsirInvariants.LockedExcludedSourceKeys[2] }
            ]);

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts);

        result.Succeeded.Should().BeFalse();

        var snapshot = await fixture.CaptureTafsirTableSnapshotAsync();
        snapshot.SourceRows.Should().Be(0);
        snapshot.EntryRows.Should().Be(0);
        snapshot.AyahLinkRows.Should().Be(0);
    }
}
