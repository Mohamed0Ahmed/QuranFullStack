using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

[Collection(nameof(TranslationImportTestCollection))]
public sealed class TranslationSourceSafetyTests(TranslationImportTestFixture fixture)
{
    [Fact]
    public async Task Success_reports_do_not_include_translation_body_text_or_arabic_quran_ayah_text()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.ReportTestSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.ReportTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);

        var json = await File.ReadAllTextAsync(Path.Combine(reportDir, TranslationImportConstants.JsonReportFileName));
        var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir, TranslationImportConstants.MarkdownReportFileName));

        AssertReportDoesNotLeakSensitiveText(json);
        AssertReportDoesNotLeakSensitiveText(markdown);
    }

    [Fact]
    public async Task Validation_failure_reports_do_not_include_translation_body_text_or_arabic_quran_ayah_text()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.MinimalSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();

        var json = await File.ReadAllTextAsync(Path.Combine(reportDir, TranslationImportConstants.JsonReportFileName));
        var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir, TranslationImportConstants.MarkdownReportFileName));

        AssertReportDoesNotLeakSensitiveText(json);
        AssertReportDoesNotLeakSensitiveText(markdown);
    }

    [Fact]
    public async Task Refusal_reports_do_not_include_translation_body_text_or_arabic_quran_ayah_text()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.DefaultSources);
        var reportDir1 = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");
        var reportDir2 = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir1);

        var result = await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir2);

        result.Succeeded.Should().BeFalse();

        var json = await File.ReadAllTextAsync(Path.Combine(reportDir2, TranslationImportConstants.JsonReportFileName));
        var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir2, TranslationImportConstants.MarkdownReportFileName));

        AssertReportDoesNotLeakSensitiveText(json);
        AssertReportDoesNotLeakSensitiveText(markdown);
    }

    private static void AssertReportDoesNotLeakSensitiveText(string reportContent)
    {
        reportContent.Should().NotContain("SYNTHETIC-TRANSLATION-");
        reportContent.Should().NotContain("[[footnote]]");
        reportContent.Should().NotContain("اختبار-");
    }
}
