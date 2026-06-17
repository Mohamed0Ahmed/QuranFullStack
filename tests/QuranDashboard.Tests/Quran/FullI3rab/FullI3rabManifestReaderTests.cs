using QuranDashboard.Application.Abstractions.Quran.FullI3rab;
using QuranDashboard.Infrastructure.Files.Quran.FullI3rab;

namespace QuranDashboard.Tests.Quran.FullI3rab;

public sealed class FullI3rabManifestReaderTests : IDisposable
{
    private readonly FullI3rabManifestReader reader = new();
    private readonly FullI3rabSyntheticPackage packages = new();

    [Fact]
    public async Task ReadAsync_parses_final_package_shape_and_source_metadata()
    {
        var packageDir = await packages.WriteAsync();

        var manifest = await reader.ReadAsync(packageDir, CancellationToken.None);

        manifest.ManifestType.Should().Be(FullI3rabImportConstants.ManifestType);
        manifest.IsFinalImportManifest.Should().BeTrue();
        manifest.SourceCount.Should().Be(1);
        manifest.LicenseStatus.Should().Be(FullI3rabImportConstants.LicenseStatus);
        manifest.ProvenanceStatus.Should().Be(FullI3rabImportConstants.ProvenanceStatus);
        manifest.UsageScope.Should().Be(FullI3rabImportConstants.UsageScope);

        var source = manifest.ApprovedSources.Single();
        source.SourceKey.Should().Be("test-muyassar");
        source.PackageFile.Should().Be("sources/test-muyassar.json");
        source.FileSizeBytes.Should().BeGreaterThan(0);
        source.Sha256.Should().NotBeNullOrWhiteSpace();
        source.Sha256.Should().HaveLength(64);
        source.ContentCoverageCount.Should().Be(FullI3rabInvariants.ExpectedAyahsPerSource);
        source.LanguageCode.Should().Be("ar");
        source.Direction.Should().Be("rtl");
        source.MarkupFormat.Should().Be("html");
        source.HasQuranQuotationMarkup.Should().BeTrue();
        source.ContributorNameAr.Should().NotBeNullOrEmpty();
        source.ContributorNameEn.Should().NotBeNullOrEmpty();
        source.ShapeJson.Should().Contain("hasQpcHafsQuotationMarkup");
    }

    [Fact]
    public async Task ReadAsync_enforces_expected_source_count_when_provided()
    {
        var packageDir = await packages.WriteAsync();
        var expected = new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: FullI3rabInvariants.ExpectedAyahsPerSource);

        var manifest = await reader.ReadAsync(packageDir, CancellationToken.None, expected);

        manifest.ApprovedSources.Should().HaveCount(1);
    }

    [Fact]
    public async Task ReadAsync_fails_source_count_when_expected_mismatches()
    {
        var packageDir = await packages.WriteAsync();
        var expected = new FullI3rabExpectedCounts(Sources: 99, AyahsPerSource: FullI3rabInvariants.ExpectedAyahsPerSource);

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None, expected);

        await act.Should().ThrowAsync<FullI3rabValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == FullI3rabInvariants.CheckSourceCount));
    }

    [Fact]
    public async Task CaptureDigestsAsync_records_file_size_and_sha256_for_approved_sources()
    {
        var packageDir = await packages.WriteAsync();
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
    public async Task VerifyDigestsUnchangedAsync_returns_true_when_package_unchanged()
    {
        var packageDir = await packages.WriteAsync();
        var before = await reader.CaptureDigestsAsync(packageDir, CancellationToken.None);

        var unchanged = await reader.VerifyDigestsUnchangedAsync(packageDir, before, CancellationToken.None);

        unchanged.Should().BeTrue();
    }

    public void Dispose() => packages.Dispose();
}
