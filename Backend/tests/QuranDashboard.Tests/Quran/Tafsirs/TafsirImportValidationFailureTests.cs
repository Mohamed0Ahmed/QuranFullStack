using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirImportValidationFailureTests(TafsirImportTestFixture fixture)
{
    [Fact]
    public async Task Import_fails_TAFSIR_JSON_SHAPE_for_malformed_source_json_root()
    {
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);
        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.IntegrationSources);
        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "sources", "ar-test-tafsir.json"),
            "[1,2,3]");
        await fixture.RefreshManifestChecksumsAsync(packageDir);

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(TafsirInvariants.SourceMismatch);
    }

    [Fact]
    public async Task Import_fails_TAFSIR_JSON_SHAPE_for_wrong_top_level_ayah_count()
    {
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);
        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.IntegrationSources);

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts with
            {
                AyahsPerSource = 4,
                SourceAyahMappings = 4
            });

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(TafsirInvariants.CheckJsonShape);
    }
}
