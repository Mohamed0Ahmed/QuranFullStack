using QuranDashboard.Application.Abstractions.Quran.Tafsirs;
using QuranDashboard.Infrastructure.Files.Quran.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirValidationFailureTests(TafsirImportTestFixture fixture)
{
    private readonly TafsirManifestReader reader = new();

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_PACKAGE_SHAPE_when_required_root_file_missing()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        File.Delete(Path.Combine(packageDir, "README.md"));

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckPackageShape));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_MANIFEST_FINAL_for_wrong_manifest_type()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync(
            manifestType: "draft-tafsir-manifest");

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckManifestFinal));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_MANIFEST_FINAL_when_not_final_import_manifest()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync(isFinalImportManifest: false);

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckManifestFinal));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_SOURCE_COUNT_when_summary_approved_count_is_wrong()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        await fixture.TamperManifestSummaryFieldAsync(packageDir, "copiedApprovedTafsirSources", 99);

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckSourceCount));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_EXCLUDED_COUNT_when_summary_excluded_count_is_wrong()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync(
            excludedSourceKeys: ["ar-excluded-test"]);
        await fixture.TamperManifestSummaryFieldAsync(packageDir, "excludedSources", 99);

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckExcludedCount));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_ARABIC_SOURCE_COUNT_when_summary_arabic_count_is_wrong()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        await fixture.TamperManifestSummaryFieldAsync(packageDir, "arabicApprovedCopied", 99);

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckArabicSourceCount));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_NON_ARABIC_SOURCE_COUNT_when_summary_non_arabic_count_is_wrong()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        await fixture.TamperManifestSummaryFieldAsync(packageDir, "nonArabicApprovedCopied", 99);

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check =>
                check.Id == TafsirInvariants.CheckNonArabicSourceCount));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_SOURCE_HASH_when_file_size_changes()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var sourcePath = Path.Combine(packageDir, "sources", "ar-test-tafsir.json");
        await File.AppendAllTextAsync(sourcePath, " ");

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckSourceHash));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_SOURCE_HASH_when_sha256_changes()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var sourcePath = Path.Combine(packageDir, "sources", "ar-test-tafsir.json");
        await File.WriteAllTextAsync(sourcePath, "{}");

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckSourceHash));
    }

    [Fact]
    public async Task Import_fails_TAFSIR_JSON_SHAPE_for_malformed_source_json_root()
    {
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);
        var packageDir = await fixture.WriteSyntheticPackageAsync(sources: TafsirSyntheticSeed.IntegrationSources);
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
        var packageDir = await fixture.WriteSyntheticPackageAsync(sources: TafsirSyntheticSeed.IntegrationSources);

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
