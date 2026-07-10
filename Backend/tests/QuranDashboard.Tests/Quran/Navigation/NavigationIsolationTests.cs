namespace QuranDashboard.Tests.Quran.Navigation;

[Collection(nameof(NavigationImportTestCollection))]
public sealed class NavigationIsolationTests(NavigationImportTestFixture fixture)
{
    [Fact]
    public async Task Import_changes_only_navigation_owned_tables_and_ayah_navigation_columns()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);
        await fixture.SeedIsolationCompanionDataAsync();

        var baseline = await fixture.CaptureIsolationBaselineAsync();
        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);

        var afterImport = await fixture.CaptureIsolationBaselineAsync();
        afterImport.Surahs.Should().BeEquivalentTo(baseline.Surahs);
        afterImport.Ayahs.Should().BeEquivalentTo(baseline.Ayahs);
        afterImport.Pages.Should().BeEquivalentTo(baseline.Pages);
        afterImport.Lines.Should().BeEquivalentTo(baseline.Lines);
        afterImport.Words.Should().BeEquivalentTo(baseline.Words);
        afterImport.TranslationSources.Should().BeEquivalentTo(baseline.TranslationSources);
        afterImport.TranslationEntries.Should().BeEquivalentTo(baseline.TranslationEntries);
        afterImport.TafsirSources.Should().BeEquivalentTo(baseline.TafsirSources);
        afterImport.TafsirEntries.Should().BeEquivalentTo(baseline.TafsirEntries);
        afterImport.TafsirAyahEntries.Should().BeEquivalentTo(baseline.TafsirAyahEntries);
        afterImport.MutashabihatGroups.Should().BeEquivalentTo(baseline.MutashabihatGroups);
        afterImport.MutashabihatOccurrences.Should().BeEquivalentTo(baseline.MutashabihatOccurrences);

        afterImport.WordMorphologyRows.Should().Be(baseline.WordMorphologyRows);
        afterImport.WordMorphologySegmentRows.Should().Be(baseline.WordMorphologySegmentRows);
        afterImport.RootRows.Should().Be(baseline.RootRows);
        afterImport.LemmaRows.Should().Be(baseline.LemmaRows);
        afterImport.StemRows.Should().Be(baseline.StemRows);
        afterImport.PosTagRows.Should().Be(baseline.PosTagRows);
        afterImport.I3rabRuleRows.Should().Be(baseline.I3rabRuleRows);

        var navigationSnapshot = await fixture.CaptureNavigationDataSnapshotAsync();
        navigationSnapshot.Juz.Should().HaveCount(2);
        navigationSnapshot.Hizb.Should().HaveCount(2);
        navigationSnapshot.Rub.Should().HaveCount(4);
        navigationSnapshot.Sajda.Should().HaveCount(2);
        navigationSnapshot.AyahNavigation.Should().OnlyContain(row =>
            row.JuzNumber != null && row.HizbNumber != null && row.RubNumber != null);
    }
}
