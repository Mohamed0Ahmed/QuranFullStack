using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Domain.Quran.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

[Collection(nameof(NavigationImportTestCollection))]
public sealed class NavigationImportTests(NavigationImportTestFixture fixture)
{
    [Fact]
    public async Task Import_tags_all_seeded_ayahs_persists_navigation_rows_and_writes_reports()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);
        result.Totals.Should().NotBeNull();
        result.Totals!.Juz.Should().Be(2);
        result.Totals.Hizb.Should().Be(2);
        result.Totals.Rub.Should().Be(4);
        result.Totals.Sajda.Should().Be(2);
        result.Totals.AyahsTagged.Should().Be(6);

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await dbContext.QuranJuzs.CountAsync()).Should().Be(2);
        (await dbContext.QuranHizbs.CountAsync()).Should().Be(2);
        (await dbContext.QuranRubs.CountAsync()).Should().Be(4);
        (await dbContext.QuranSajdas.CountAsync()).Should().Be(2);

        var ayahs = await dbContext.QuranAyahs
            .AsNoTracking()
            .OrderBy(ayah => ayah.Id)
            .ToListAsync();
        ayahs.Should().HaveCount(6);

        foreach (var (ayahId, juzNumber, hizbNumber, rubNumber) in NavigationSyntheticSeed.ExpectedAyahAssignments)
        {
            var ayah = ayahs.Single(row => row.Id == ayahId);
            ayah.JuzNumber.Should().Be(juzNumber, $"ayah {ayahId} juz");
            ayah.HizbNumber.Should().Be(hizbNumber, $"ayah {ayahId} hizb");
            ayah.RubNumber.Should().Be(rubNumber, $"ayah {ayahId} rub");
        }

        var sajdas = await dbContext.QuranSajdas
            .AsNoTracking()
            .OrderBy(sajda => sajda.SajdahNumber)
            .Select(sajda => new { sajda.SajdahNumber, sajda.VerseKey, sajda.SajdahType })
            .ToListAsync();

        sajdas.Should().Equal(
            NavigationSyntheticSeed.ExpectedSajdaRows.Select(row => new
            {
                SajdahNumber = row.SajdahNumber,
                VerseKey = row.VerseKey,
                SajdahType = string.Equals(row.SajdahType, "required", StringComparison.Ordinal)
                    ? SajdahType.Required
                    : SajdahType.Optional
            }));

        File.Exists(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName)).Should().BeTrue();
        File.Exists(Path.Combine(reportDir, NavigationImportConstants.MarkdownReportFileName)).Should().BeTrue();

        var reportJson = await File.ReadAllTextAsync(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName));
        using var reportDocument = JsonDocument.Parse(reportJson);
        reportDocument.RootElement.GetProperty("verdict").GetString().Should().Be(NavigationImportConstants.AcceptedVerdict);
        reportDocument.RootElement.GetProperty("persisted").GetBoolean().Should().BeTrue();
        reportDocument.RootElement.GetProperty("noQuranAyahTextReadOrStored").GetBoolean().Should().BeTrue();
    }
}
