using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

[Collection(nameof(NavigationImportTestCollection))]
public sealed class NavigationSourceSafetyTests(NavigationImportTestFixture fixture)
{
    [Fact]
    public async Task Import_with_decoy_text_fields_in_source_succeeds_without_reading_or_storing_quran_text()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var textBefore = await CaptureAyahTextSnapshotAsync();
        var packageDir = await fixture.WriteSyntheticPackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            NavigationSyntheticSeed.DecoyQuranTextPackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);

        var reportJson = await File.ReadAllTextAsync(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName));
        using var reportDocument = JsonDocument.Parse(reportJson);
        reportDocument.RootElement.GetProperty("noQuranAyahTextReadOrStored").GetBoolean().Should().BeTrue();

        var checks = reportDocument.RootElement.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == NavigationMetadataInvariants.CheckNoQuranTextCopy
            && check.GetProperty("passed").GetBoolean());

        var textAfter = await CaptureAyahTextSnapshotAsync();
        textAfter.Should().Equal(textBefore);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var juzJson = await File.ReadAllTextAsync(Path.Combine(packageDir, "sources/quran-metadata-juz.json"));
        juzJson.Should().Contain("DECOY-QURAN-TEXT-DO-NOT-STORE");
        juzJson.Should().Contain("DECOY-UTHMANI-DO-NOT-STORE");

        (await dbContext.QuranJuzs.CountAsync()).Should().BePositive();

        var ayahTexts = await dbContext.QuranAyahs
            .AsNoTracking()
            .Select(ayah => ayah.TextUthmani)
            .ToListAsync();
        ayahTexts.Should().NotContain(text => text.Contains("DECOY", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Validation_failure_report_still_asserts_no_quran_text_was_read_or_stored()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var textBefore = await CaptureAyahTextSnapshotAsync();
        var packageDir = await fixture.WriteSyntheticPackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            NavigationSyntheticSeed.JuzGapPackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();

        var reportJson = await File.ReadAllTextAsync(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName));
        using var reportDocument = JsonDocument.Parse(reportJson);
        reportDocument.RootElement.GetProperty("noQuranAyahTextReadOrStored").GetBoolean().Should().BeTrue();

        var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir, NavigationImportConstants.MarkdownReportFileName));
        markdown.Should().Contain("No Quran ayah text was read or stored by this import.");

        var textAfter = await CaptureAyahTextSnapshotAsync();
        textAfter.Should().Equal(textBefore);
    }

    private async Task<IReadOnlyList<(int Id, string TextUthmani)>> CaptureAyahTextSnapshotAsync()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return await dbContext.QuranAyahs
            .AsNoTracking()
            .OrderBy(ayah => ayah.Id)
            .Select(ayah => new ValueTuple<int, string>(ayah.Id, ayah.TextUthmani))
            .ToListAsync();
    }
}
