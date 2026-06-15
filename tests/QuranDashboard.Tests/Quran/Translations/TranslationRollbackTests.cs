using QuranDashboard.Application.Quran.Translations.ImportTranslations;
using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

[Collection(nameof(TranslationImportTestCollection))]
public sealed class TranslationRollbackTests(TranslationImportTestFixture fixture)
{
    [Fact]
    public async Task Failed_type_validation_leaves_zero_accepted_rows()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await CreateReclassifiedSourcePackageAsync();

        var reportDir = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir, TranslationSyntheticSeed.DefaultTestExpectedCounts, reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(TranslationInvariants.CheckTypeCounts);

        var snapshot = await fixture.CaptureTranslationTableSnapshotAsync();
        snapshot.SourceRows.Should().Be(0);
        snapshot.AyahEntryRows.Should().Be(0);
    }

    [Fact]
    public async Task Report_write_failure_after_copy_rolls_back_all_writes()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.DefaultSources);

        var reportBlocker = Path.Combine(Path.GetTempPath(), $"translation-report-blocker-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(reportBlocker, "blocker");
        var reportDir = Path.Combine(reportBlocker, "subdir");

        var result = await fixture.RunImportAsync(
            packageDir, TranslationSyntheticSeed.DefaultTestExpectedCounts, reportDir);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(TranslationInvariants.ReportRequired);
        result.ReportOutDir.Should().Be(reportDir);
        result.Message.Should().Contain($"Report directory: {reportDir}");

        var snapshot = await fixture.CaptureTranslationTableSnapshotAsync();
        snapshot.SourceRows.Should().Be(0);
        snapshot.AyahEntryRows.Should().Be(0);
    }

    [Fact]
    public async Task Previous_accepted_data_remains_when_replacement_fails_validation()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var validPackage = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.DefaultSources);
        var reportDir1 = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var firstResult = await fixture.RunImportAsync(
            validPackage, TranslationSyntheticSeed.DefaultTestExpectedCounts, reportDir1);
        firstResult.Succeeded.Should().BeTrue(firstResult.Message);

        var snapshotBefore = await fixture.CaptureTranslationTableSnapshotAsync();
        snapshotBefore.SourceRows.Should().BeGreaterThan(0);

        var invalidPackage = await CreateReclassifiedSourcePackageAsync();
        var reportDir2 = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var secondResult = await fixture.RunImportAsync(
            invalidPackage, TranslationSyntheticSeed.DefaultTestExpectedCounts, reportDir2, force: true);

        secondResult.Succeeded.Should().BeFalse();

        var snapshotAfter = await fixture.CaptureTranslationTableSnapshotAsync();
        snapshotAfter.SourceRows.Should().Be(snapshotBefore.SourceRows);
        snapshotAfter.AyahEntryRows.Should().Be(snapshotBefore.AyahEntryRows);
    }

    private static async Task<string> CreateReclassifiedSourcePackageAsync()
    {
        var entries = new Dictionary<string, string>(TranslationInvariants.ExpectedAyahsPerSource);
        for (var ayah = 1; ayah <= TranslationInvariants.ExpectedAyahsPerSource; ayah++)
        {
            var verseKey = TranslationSyntheticSeed.SyntheticVerseKey(ayah);
            var text = ayah == 1
                ? $"{TranslationSyntheticSeed.SyntheticText("en-reclassify", verseKey)} [[footnote]]"
                : TranslationSyntheticSeed.SyntheticText("en-reclassify", verseKey);
            entries[verseKey] = text;
        }

        var fixture = new TranslationImportTestFixture();
        return await fixture.WriteSyntheticPackageAsync(
            sources:
            [
                new SyntheticTranslationSourceSpec(
                    SourceKey: "en-reclassify",
                    LanguageCode: "en",
                    LanguageNameEn: "English",
                    LanguageNameAr: "الإنجليزية",
                    NativeName: "English",
                    Direction: "ltr",
                    TranslationType: "simple",
                    DisplayNameEn: "Synthetic Reclassify",
                    DisplayNameAr: "ترجمة اختبارية",
                    TranslatorKey: "test",
                    TranslatorNameEn: "Test",
                    TranslatorNameAr: "اختبار",
                    ContentCoverageCount: TranslationInvariants.ExpectedAyahsPerSource,
                    Entries: entries)
            ]);
    }
}
