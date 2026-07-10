using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

public sealed class NavigationDatasetReaderTests
{
    [Fact]
    public async Task ReadAllAsync_parses_divisions_and_sajda_type()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register);

        var manifestReader = new NavigationManifestReader();
        var datasetReader = new JsonNavigationDatasetReader();

        var manifest = await manifestReader.ReadAsync(
            packageDir,
            CancellationToken.None,
            NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var parsed = await datasetReader.ReadAllAsync(manifest, CancellationToken.None);

        parsed.Juz.Should().HaveCount(2);
        parsed.Hizb.Should().HaveCount(2);
        parsed.Rub.Should().HaveCount(4);
        parsed.Sajda.Should().HaveCount(2);
        parsed.Juz[0].Number.Should().Be(1);
        parsed.Juz[0].VerseMapping["901"].Should().Be("1-3");
        parsed.Sajda
            .OrderBy(sajda => sajda.SajdahNumber)
            .Select(sajda => (sajda.SajdahNumber, sajda.VerseKey, sajda.SajdahType))
            .Should()
            .Equal(NavigationSyntheticSeed.ExpectedSajdaRows);
    }
}
