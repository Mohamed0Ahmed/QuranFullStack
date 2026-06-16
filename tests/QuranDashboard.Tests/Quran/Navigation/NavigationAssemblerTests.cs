using QuranDashboard.Application.Abstractions.Quran.Navigation;
using QuranDashboard.Domain.Quran.Navigation;
using QuranDashboard.Infrastructure.Files.Quran.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

public sealed class NavigationAssemblerTests
{
    private readonly NavigationMetadataAssembler assembler = new();

    [Fact]
    public async Task Assemble_expands_mappings_covers_each_ayah_once_and_derives_hierarchy()
    {
        await using var packageScope = new NavigationTempPackageScope();
        var packageDir = await NavigationSyntheticPackageWriter.WritePackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            registerTempDir: packageScope.Register);

        var manifestReader = new NavigationManifestReader();
        var datasetReader = new JsonNavigationDatasetReader();
        var importSource = new NavigationMetadataImportSource(manifestReader, datasetReader);

        var source = await importSource.LoadAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            CancellationToken.None);

        var ayahIds = NavigationSyntheticSeed.DefaultAyahs.ToDictionary(
            ayah => ayah.VerseKey,
            ayah => ayah.Id,
            StringComparer.Ordinal);

        var assembled = assembler.Assemble(source, ayahIds, NavigationSyntheticSeed.DefaultTestExpectedCounts);

        assembled.Juz.Should().HaveCount(2);
        assembled.Hizb.Should().HaveCount(2);
        assembled.Rub.Should().HaveCount(4);
        assembled.Sajda.Should().HaveCount(2);
        assembled.AyahAssignments.Should().HaveCount(6);

        foreach (var (ayahId, juzNumber, hizbNumber, rubNumber) in NavigationSyntheticSeed.ExpectedAyahAssignments)
        {
            assembled.AyahAssignments.Should().ContainKey(ayahId);
            var assignment = assembled.AyahAssignments[ayahId];
            assignment.JuzNumber.Should().Be(juzNumber, $"ayah {ayahId} juz");
            assignment.HizbNumber.Should().Be(hizbNumber, $"ayah {ayahId} hizb");
            assignment.RubNumber.Should().Be(rubNumber, $"ayah {ayahId} rub");
        }

        assembled.Juz.Select(juz => juz.Number).Should().Equal(new short[] { 1, 2 });
        assembled.Juz.Single(juz => juz.Number == 1).VersesCount.Should().Be(3);
        assembled.Juz.Single(juz => juz.Number == 2).VersesCount.Should().Be(3);

        assembled.Hizb.Single(hizb => hizb.Number == 1).ParentJuzNumber.Should().Be(1);
        assembled.Hizb.Single(hizb => hizb.Number == 2).ParentJuzNumber.Should().Be(2);

        assembled.Rub.Single(rub => rub.Number == 1).ParentHizbNumber.Should().Be(1);
        assembled.Rub.Single(rub => rub.Number == 2).ParentHizbNumber.Should().Be(1);
        assembled.Rub.Single(rub => rub.Number == 3).ParentHizbNumber.Should().Be(2);
        assembled.Rub.Single(rub => rub.Number == 4).ParentHizbNumber.Should().Be(2);

        assembled.Sajda
            .OrderBy(sajda => sajda.SajdahNumber)
            .Select(sajda => (sajda.SajdahNumber, sajda.VerseKey, sajda.SajdahType))
            .Should()
            .Equal(
                NavigationSyntheticSeed.ExpectedSajdaRows.Select(row => (
                    row.SajdahNumber,
                    row.VerseKey,
                    string.Equals(row.SajdahType, "required", StringComparison.Ordinal)
                        ? SajdahType.Required
                        : SajdahType.Optional)));
    }

    [Fact]
    public void ExpandVerseMapping_expands_inclusive_ranges_in_surah_order()
    {
        var keys = NavigationMetadataAssembler.ExpandVerseMapping(
            new Dictionary<string, string> { ["901"] = "2-4", ["902"] = "1-2" });

        keys.Should().Equal("901:2", "901:3", "901:4", "902:1", "902:2");
    }
}
