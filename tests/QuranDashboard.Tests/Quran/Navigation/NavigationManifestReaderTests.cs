using System.Text.Json.Nodes;
using QuranDashboard.Application.Abstractions.Quran.Navigation;
using QuranDashboard.Infrastructure.Files.Quran.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

public sealed class NavigationManifestReaderTests
{
    private readonly NavigationManifestReader reader = new();

    [Fact]
    public async Task ReadAsync_parses_final_manifest_contract_fields()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register);

        var manifest = await reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        manifest.PackageType.Should().Be(NavigationImportConstants.ManifestType);
        manifest.IsFinalImportManifest.Should().BeTrue();
        manifest.SourceFiles.Should().HaveCount(4);
        manifest.SourceFiles.Select(file => file.DatasetKey).Should()
            .Equal("juz", "hizb", "rub", "sajda");
    }

    [Fact]
    public async Task ReadAsync_rejects_wrong_package_type()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register,
            packageType: "wrong-package-type");

        var act = () => reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        (await act.Should().ThrowAsync<NavigationMetadataValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckManifestFinal && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_rejects_non_final_manifest()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register,
            isFinalImportManifest: false);

        var act = () => reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        (await act.Should().ThrowAsync<NavigationMetadataValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckManifestFinal && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_rejects_missing_source_file()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register);

        File.Delete(Path.Combine(packageDir, "sources", "quran-metadata-sajda.json"));

        var act = () => reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        (await act.Should().ThrowAsync<NavigationMetadataValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckPackageShape && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_rejects_extra_source_file()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register);

        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "sources", "quran-metadata-extra.json"),
            "{}");

        var act = () => reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        (await act.Should().ThrowAsync<NavigationMetadataValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckPackageShape && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_rejects_sha256_mismatch()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register);

        await File.AppendAllTextAsync(
            Path.Combine(packageDir, "sources", "quran-metadata-juz.json"),
            "TAMPERED");

        var act = () => reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        (await act.Should().ThrowAsync<NavigationMetadataValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckSourceHash && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_rejects_size_mismatch()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register);

        var manifestPath = Path.Combine(packageDir, "manifest.json");
        var manifestNode = JsonNode.Parse(await File.ReadAllTextAsync(manifestPath))!;
        var sourceFiles = manifestNode["sourceFiles"]!.AsArray();
        sourceFiles[0]!["sizeBytes"] = 1;
        await File.WriteAllTextAsync(manifestPath, manifestNode.ToJsonString());

        var act = () => reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        (await act.Should().ThrowAsync<NavigationMetadataValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckSourceHash && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_rejects_count_mismatch()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register);

        var wrongCounts = NavigationSyntheticSeed.DefaultTestExpectedCounts with { Juz = 999 };

        var act = () => reader.ReadAsync(packageDir, CancellationToken.None, wrongCounts);

        (await act.Should().ThrowAsync<NavigationMetadataValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckSourceCount && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_rejects_manifest_missing_source_files()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register);

        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "manifest.json"),
            """
            {
              "packageType": "quran-navigation-metadata-import-source-package",
              "isFinalImportManifest": true
            }
            """);

        var act = () => reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        (await act.Should().ThrowAsync<NavigationMetadataValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckJsonShape && !check.Passed);
    }

    [Fact]
    public async Task ReadAsync_rejects_malformed_source_files_entry()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register);

        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "manifest.json"),
            """
            {
              "packageType": "quran-navigation-metadata-import-source-package",
              "isFinalImportManifest": true,
              "sourceFiles": [
                {
                  "relativePath": "",
                  "datasetKey": "juz",
                  "recordCount": 2,
                  "sha256": "ABCD",
                  "sizeBytes": 100
                }
              ]
            }
            """);

        var act = () => reader.ReadAsync(
            packageDir,
            CancellationToken.None,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);

        (await act.Should().ThrowAsync<NavigationMetadataValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == NavigationMetadataInvariants.CheckJsonShape && !check.Passed);
    }
}
