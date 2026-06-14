using QuranDashboard.Application.Abstractions.Quran.Tafsirs;
using QuranDashboard.Infrastructure.Files.Quran.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

public sealed class TafsirManifestReaderTests
{
    private readonly TafsirManifestReader reader = new();
    private readonly TafsirImportTestFixture fixture = new();

    [Fact]
    public async Task ReadAsync_parses_final_package_shape_and_source_metadata()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.DefaultSources,
            excludedSourceKeys: ["ar-excluded-test"]);

        var manifest = await reader.ReadAsync(packageDir, CancellationToken.None);

        manifest.ManifestType.Should().Be(TafsirImportConstants.ManifestType);
        manifest.IsFinalImportManifest.Should().BeTrue();
        manifest.SourceCount.Should().Be(1);
        manifest.ApprovedSourceCount.Should().Be(1);
        manifest.ExcludedSourceCount.Should().Be(1);
        manifest.ArabicSourceCount.Should().Be(1);
        manifest.NonArabicSourceCount.Should().Be(0);
        manifest.LanguageCount.Should().Be(1);

        var source = manifest.ApprovedSources.Single();
        source.SourceKey.Should().Be("ar-test-tafsir");
        source.PackageFile.Should().Be("sources/ar-test-tafsir.json");
        source.FileSizeBytes.Should().BeGreaterThan(0);
        source.Sha256.Should().NotBeNullOrWhiteSpace();
        source.Sha256.Should().HaveLength(64);
        source.ContentCoverageCount.Should().Be(3);
        source.LanguageCode.Should().Be("ar");
        source.ResourceKind.Should().Be("tafsir");

        manifest.ExcludedSources.Should().ContainSingle();
        manifest.ExcludedSources[0].SourceKey.Should().Be("ar-excluded-test");
    }

    [Fact]
    public async Task CaptureDigestsAsync_records_file_size_and_sha256_for_approved_sources()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var manifest = await reader.ReadAsync(packageDir, CancellationToken.None);
        var digests = await reader.CaptureDigestsAsync(packageDir, CancellationToken.None);

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
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var before = await reader.CaptureDigestsAsync(packageDir, CancellationToken.None);

        var unchanged = await reader.VerifyDigestsUnchangedAsync(packageDir, before, CancellationToken.None);

        unchanged.Should().BeTrue();
    }

    [Fact]
    public async Task ReadAsync_refuses_extra_unapproved_files_in_sources_directory()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var extraPath = Path.Combine(packageDir, "sources", "ar-unapproved-extra.json");
        await File.WriteAllTextAsync(extraPath, "{}");

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None);

        await act.Should().ThrowAsync<TafsirSourceException>()
            .WithMessage("*Unexpected source files outside approved manifest set*");
    }
}
