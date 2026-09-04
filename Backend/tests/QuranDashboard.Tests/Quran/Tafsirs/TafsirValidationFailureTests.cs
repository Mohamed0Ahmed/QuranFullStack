using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

public sealed class TafsirValidationFailureTests : IDisposable
{
    private readonly TafsirManifestReader reader = new();
    private readonly TafsirSyntheticPackage packages = new();

    public void Dispose() => packages.Dispose();

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_PACKAGE_SHAPE_when_required_root_file_missing()
    {
        var packageDir = await packages.WriteAsync();
        File.Delete(Path.Combine(packageDir, "README.md"));

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckPackageShape));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_MANIFEST_FINAL_for_wrong_manifest_type()
    {
        var packageDir = await packages.WriteAsync(
            manifestType: "draft-tafsir-manifest");

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckManifestFinal));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_MANIFEST_FINAL_when_not_final_import_manifest()
    {
        var packageDir = await packages.WriteAsync(isFinalImportManifest: false);

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckManifestFinal));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_SOURCE_COUNT_when_summary_approved_count_is_wrong()
    {
        var packageDir = await packages.WriteAsync();
        await packages.TamperManifestSummaryFieldAsync(packageDir, "copiedApprovedTafsirSources", 99);

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckSourceCount));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_EXCLUDED_COUNT_when_summary_excluded_count_is_wrong()
    {
        var packageDir = await packages.WriteAsync(
            excludedSourceKeys: ["ar-excluded-test"]);
        await packages.TamperManifestSummaryFieldAsync(packageDir, "excludedSources", 99);

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckExcludedCount));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_ARABIC_SOURCE_COUNT_when_summary_arabic_count_is_wrong()
    {
        var packageDir = await packages.WriteAsync();
        await packages.TamperManifestSummaryFieldAsync(packageDir, "arabicApprovedCopied", 99);

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckArabicSourceCount));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_NON_ARABIC_SOURCE_COUNT_when_summary_non_arabic_count_is_wrong()
    {
        var packageDir = await packages.WriteAsync();
        await packages.TamperManifestSummaryFieldAsync(packageDir, "nonArabicApprovedCopied", 99);

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check =>
                check.Id == TafsirInvariants.CheckNonArabicSourceCount));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_SOURCE_HASH_when_file_size_changes()
    {
        var packageDir = await packages.WriteAsync();
        var sourcePath = Path.Combine(packageDir, "sources", "ar-test-tafsir.json");
        await File.AppendAllTextAsync(sourcePath, " ");

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckSourceHash));
    }

    [Fact]
    public async Task ReadAsync_fails_TAFSIR_SOURCE_HASH_when_sha256_changes()
    {
        var packageDir = await packages.WriteAsync();
        var sourcePath = Path.Combine(packageDir, "sources", "ar-test-tafsir.json");
        await File.WriteAllTextAsync(sourcePath, "{}");

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckSourceHash));
    }

    [Fact]
    public async Task ReadAsync_fails_for_malformed_source_json_root()
    {
        var packageDir = await packages.WriteAsync(sources: TafsirSyntheticSeed.IntegrationSources);
        var sourcePath = Path.Combine(packageDir, "sources", "ar-test-tafsir.json");
        await File.WriteAllTextAsync(sourcePath, "[1,2,3]");

        var act = () => new JsonTafsirSourceReader().ReadAsync(sourcePath, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirSourceException>();
    }

    [Fact]
    public void AssembleSource_fails_TAFSIR_JSON_SHAPE_for_wrong_top_level_ayah_count()
    {
        var assembler = new TafsirAssembler();
        var manifestSource = new TafsirManifestSourceRecord(
            SourceKey: "ar-test-tafsir",
            LanguageCode: "ar",
            LanguageNameAr: "العربية",
            LanguageNameEn: "Arabic",
            Direction: "rtl",
            DisplayNameAr: "العربية",
            ShortNameAr: "العربية",
            DisplayNameEn: "Arabic",
            ShortNameEn: "Arabic",
            ContributorKey: null,
            ContributorNameAr: null,
            ContributorNameEn: null,
            ContributorType: "scholar",
            ResourceKind: "tafsir",
            TafsirKind: "brief",
            ContentCoverageCount: 3,
            PackageFile: "sources/ar-test-tafsir.json",
            SourceFileOriginal: "sources/ar-test-tafsir.json",
            Sha256: "hash",
            FileSizeBytes: 100,
            License: null,
            Provenance: null);
        var parsed = new ParsedTafsirSourceFile(
            "ar-test-tafsir",
            new Dictionary<string, ParsedTafsirSourceEntry>
            {
                ["900:1"] = new ParsedTafsirSourceEntry.TextOwning(
                    "900:1",
                    "<p>text</p>",
                    ["900:1"])
            });

        var act = () => assembler.AssembleSource(
            manifestSource,
            parsed,
            "{}",
            new Dictionary<string, int> { ["900:1"] = 1 },
            new Dictionary<string, string> { ["900:1"] = "text" },
            new HashSet<(string SourceKey, int AyahId)>(),
            expectedAyahsPerSource: 4);

        act.Should().Throw<TafsirValidationException>()
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TafsirInvariants.CheckJsonShape && !check.Passed);
    }
}
