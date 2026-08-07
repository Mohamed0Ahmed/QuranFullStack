using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

public sealed class TranslationManifestReaderTests : IDisposable
{
    private readonly TranslationManifestReader reader = new();
    private readonly TranslationSyntheticPackage packages = new();

    public void Dispose() => packages.Dispose();

    [Fact]
    public async Task ReadAsync_parses_final_manifest_contract_fields()
    {
        var packageDir = await packages.WriteAsync(
            sources: TranslationSyntheticSeed.DefaultSources,
            excludedSourceKeys: ["en-excluded-test"]);

        var manifest = await reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            new TranslationExpectedCounts(
                ApprovedSources: 1,
                SimpleSources: 1,
                WithFootnotesSources: 0,
                ExcludedSources: 1,
                Languages: 1,
                AyahsPerSource: TranslationInvariants.ExpectedAyahsPerSource,
                SourceAyahMappings: TranslationInvariants.ExpectedAyahsPerSource));

        manifest.ManifestType.Should().Be(TranslationImportConstants.ManifestType);
        manifest.IsFinalImportManifest.Should().BeTrue();
        manifest.SourceCount.Should().Be(1);
        manifest.SimpleSourceCount.Should().Be(1);
        manifest.WithFootnotesSourceCount.Should().Be(0);
        manifest.ExcludedSourceCount.Should().Be(1);
        manifest.LanguageCount.Should().Be(1);
        manifest.ContentCoverageCount.Should().Be(TranslationInvariants.ExpectedAyahsPerSource);
        manifest.ApprovedAyahMappingCount.Should().Be(TranslationInvariants.ExpectedAyahsPerSource);

        var source = manifest.ApprovedSources.Single();
        source.SourceKey.Should().Be("en-test-simple");
        source.PackageFile.Should().Be("sources/en-test-simple.json");
        source.FileSizeBytes.Should().BeGreaterThan(0);
        source.Sha256.Should().NotBeNullOrWhiteSpace();
        source.Sha256.Should().HaveLength(64);
        source.ContentCoverageCount.Should().Be(TranslationInvariants.ExpectedAyahsPerSource);

        manifest.ExcludedSources.Should().ContainSingle();
        manifest.ExcludedSources[0].SourceKey.Should().Be("en-excluded-test");
        manifest.ExcludedSources[0].PackageFile.Should().Be("excluded/en-excluded-test.json");
        manifest.ExcludedSources[0].Status.Should().Be("excluded");
        manifest.ExcludedSources[0].Reason.Should().Be("word_by_word");
    }

    [Fact]
    public async Task ReadAsync_enforces_production_counts_when_expected_counts_are_production()
    {
        var packageDir = await packages.WriteAsync();

        var act = () => reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            TranslationInvariants.Production);

        await act.Should().ThrowAsync<TranslationValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TranslationInvariants.CheckSourceCount));
    }

    [Fact]
    public void Production_invariants_match_final_manifest_contract()
    {
        TranslationInvariants.ExpectedApprovedSources.Should().Be(167);
        TranslationInvariants.ExpectedSimpleSources.Should().Be(129);
        TranslationInvariants.ExpectedWithFootnotesSources.Should().Be(38);
        TranslationInvariants.ExpectedExcludedSources.Should().Be(19);
        TranslationInvariants.ExpectedLanguageCount.Should().Be(83);
        TranslationInvariants.ExpectedAyahsPerSource.Should().Be(6_236);
        TranslationInvariants.ExpectedSourceAyahMappings.Should().Be(1_041_412);
        TranslationInvariants.Production.ApprovedSources.Should().Be(167);
        TranslationInvariants.Production.SourceAyahMappings.Should().Be(1_041_412);
    }

    [Fact]
    public async Task ReadAsync_validates_type_counts_for_multi_source_package()
    {
        var packageDir = await packages.WriteAsync(
            sources: TranslationSyntheticSeed.IntegrationSources);

        var manifest = await reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            TranslationSyntheticSeed.IntegrationTestExpectedCounts);

        manifest.SimpleSourceCount.Should().Be(1);
        manifest.WithFootnotesSourceCount.Should().Be(1);
        manifest.ApprovedAyahMappingCount.Should().Be(6);
    }

    [Fact]
    public async Task CaptureDigestsAsync_records_file_size_and_sha256_for_approved_sources()
    {
        var packageDir = await packages.WriteAsync();
        var manifest = await reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            TranslationSyntheticSeed.DefaultTestExpectedCounts);
        var digests = await reader.CapturePackageDigestsAsync(packageDir, CancellationToken.None);

        digests.Digests.Should().ContainKey("manifest.json");
        digests.Digests.Should().ContainKey("source-display-metadata.json");
        digests.Digests.Should().ContainKey("README.md");
        digests.Digests.Should().ContainKey("package-report.md");

        var source = manifest.ApprovedSources.Single();
        var relativePath = source.PackageFile.Replace('\\', '/');
        digests.Digests.Should().ContainKey(relativePath);

        var digest = digests.Digests[relativePath];
        digest.Size.Should().Be(source.FileSizeBytes);
        digest.Sha256.Should().Be(source.Sha256);
    }

    [Fact]
    public async Task VerifyDigestsUnchangedAsync_returns_true_when_package_is_unchanged()
    {
        var packageDir = await packages.WriteAsync();
        var before = await reader.CapturePackageDigestsAsync(packageDir, CancellationToken.None);

        var unchanged = await reader.VerifyDigestsUnchangedAsync(packageDir, before, CancellationToken.None);

        unchanged.Should().BeTrue();
    }
}
