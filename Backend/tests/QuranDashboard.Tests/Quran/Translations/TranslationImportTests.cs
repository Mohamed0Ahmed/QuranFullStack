using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

[Collection(nameof(TranslationImportTestCollection))]
public sealed class TranslationImportTests(TranslationImportTestFixture fixture)
{
    private const string JsonReportFileName = TranslationImportConstants.JsonReportFileName;
    private const string MarkdownReportFileName = TranslationImportConstants.MarkdownReportFileName;

    [Fact]
    public async Task Import_persists_sources_ayah_rows_type_totals_exact_text_and_success_reports()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);
        result.Totals.Should().NotBeNull();
        result.Totals!.SourceRows.Should().Be(1);
        result.Totals.SimpleSources.Should().Be(1);
        result.Totals.WithFootnotesSources.Should().Be(0);
        result.Totals.AyahMappingRows.Should().Be(TranslationInvariants.ExpectedAyahsPerSource);
        result.Totals.DistinctAyahs.Should().Be(TranslationInvariants.ExpectedAyahsPerSource);

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var sources = await dbContext.TranslationSources.AsNoTracking().ToListAsync();
        sources.Should().ContainSingle();
        sources[0].SourceKey.Should().Be("en-test-simple");
        sources[0].ContentCoverageCount.Should().Be(TranslationInvariants.ExpectedAyahsPerSource);

        var ayahEntries = await dbContext.TranslationAyahEntries.AsNoTracking().ToListAsync();
        ayahEntries.Should().HaveCount(TranslationInvariants.ExpectedAyahsPerSource);
        ayahEntries.Select(entry => entry.AyahId).Distinct().Should().HaveCount(TranslationInvariants.ExpectedAyahsPerSource);

        var firstEntry = ayahEntries.Single(entry => entry.VerseKey == "901:1");
        firstEntry.Text.Should().Be(TranslationSyntheticSeed.SyntheticText("en-test-simple", "901:1"));

        File.Exists(Path.Combine(reportDir, JsonReportFileName)).Should().BeTrue();
        File.Exists(Path.Combine(reportDir, MarkdownReportFileName)).Should().BeTrue();

        var reportJson = await File.ReadAllTextAsync(Path.Combine(reportDir, JsonReportFileName));
        using var reportDocument = JsonDocument.Parse(reportJson);
        var reportRoot = reportDocument.RootElement;

        reportRoot.GetProperty("verdict").GetString().Should().Be(TranslationImportConstants.PassVerdict);
        reportRoot.GetProperty("persisted").GetBoolean().Should().BeTrue();
        reportRoot.GetProperty("sourcePath").GetString().Should().Be(Path.GetFullPath(packageDir));

        var checks = reportRoot.GetProperty("checks").EnumerateArray().ToList();
        checks.Single(check =>
                check.GetProperty("id").GetString() == TranslationInvariants.CheckReportWritten)
            .GetProperty("passed")
            .GetBoolean()
            .Should()
            .BeTrue();

        var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir, MarkdownReportFileName));
        markdown.Should().Contain("Persisted: true");
        markdown.Should().Contain(TranslationInvariants.CheckReportWritten);

        var typeCountsCheck = checks.Single(check =>
            check.GetProperty("id").GetString() == TranslationInvariants.CheckTypeCounts);
        typeCountsCheck.GetProperty("passed").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Import_fails_when_content_reclassification_breaks_expected_type_totals()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.MinimalAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources:
            [
                new SyntheticTranslationSourceSpec(
                    SourceKey: "en-reclassify-test",
                    LanguageCode: "en",
                    LanguageNameEn: "English",
                    LanguageNameAr: "الإنجليزية",
                    NativeName: "English",
                    Direction: "ltr",
                    TranslationType: "simple",
                    DisplayNameEn: "Synthetic Simple",
                    DisplayNameAr: "ترجمة بسيطة",
                    TranslatorKey: "test",
                    TranslatorNameEn: "Test",
                    TranslatorNameAr: "اختبار",
                    ContentCoverageCount: 3,
                    Entries: new Dictionary<string, string>
                    {
                        ["901:1"] = "SYNTHETIC-TRANSLATION-en-reclassify-test-901:1 [[footnote]]",
                        ["901:2"] = "SYNTHETIC-TRANSLATION-en-reclassify-test-901:2",
                        ["901:3"] = "SYNTHETIC-TRANSLATION-en-reclassify-test-901:3"
                    })
            ]);
        var reportDir = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.MinimalTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(TranslationInvariants.CheckTypeCounts);

        var snapshot = await fixture.CaptureTranslationTableSnapshotAsync();
        snapshot.SourceRows.Should().Be(0);
        snapshot.AyahEntryRows.Should().Be(0);

        File.Exists(Path.Combine(reportDir, JsonReportFileName)).Should().BeTrue();
        var reportJson = await File.ReadAllTextAsync(Path.Combine(reportDir, JsonReportFileName));
        using var reportDocument = JsonDocument.Parse(reportJson);
        var typeCountsCheck = reportDocument.RootElement
            .GetProperty("checks")
            .EnumerateArray()
            .SingleOrDefault(check =>
                check.GetProperty("id").GetString() == TranslationInvariants.CheckTypeCounts);

        typeCountsCheck.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        if (typeCountsCheck.ValueKind != JsonValueKind.Undefined)
        {
            typeCountsCheck.GetProperty("passed").GetBoolean().Should().BeFalse();
        }
    }
}
