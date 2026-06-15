using QuranDashboard.Application.Quran.Translations.ImportTranslations;
using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

[Collection(nameof(TranslationImportTestCollection))]
public sealed class TranslationReportShapeTests(TranslationImportTestFixture fixture)
{
    private static readonly string[] AllFr032HardCheckIds =
    [
        TranslationInvariants.CheckPackageShape,
        TranslationInvariants.CheckManifestFinal,
        TranslationInvariants.CheckDisplayMetadataFinal,
        TranslationInvariants.CheckDisplayMetadataSet,
        TranslationInvariants.CheckDisplayMetadataRequiredFields,
        TranslationInvariants.CheckSourceCount,
        TranslationInvariants.CheckTypeCounts,
        TranslationInvariants.CheckExcludedCount,
        TranslationInvariants.CheckSourceSet,
        TranslationInvariants.CheckSourceHash,
        TranslationInvariants.CheckNoExcludedSources,
        TranslationInvariants.CheckJsonShape,
        TranslationInvariants.CheckCoverageCount,
        TranslationInvariants.CheckNoEmptyText,
        TranslationInvariants.CheckAyahKeysResolve,
        TranslationInvariants.CheckNoDuplicateAyahEntry,
        TranslationInvariants.CheckTextUnchanged,
        TranslationInvariants.CheckNoQuranTextCopy,
        TranslationInvariants.CheckPostCopySourceRows,
        TranslationInvariants.CheckPostCopyAyahMappings,
        TranslationInvariants.CheckSourceUnchanged,
        TranslationInvariants.CheckReportWritten,
        TranslationInvariants.CheckRollbackOnFail,
        TranslationInvariants.CheckRerunGuard
    ];

    [Fact]
    public async Task Success_json_report_contains_required_top_level_fields_and_all_hard_checks()
    {
        var (reportDir, reportRoot) = await RunSuccessfulReportImportAsync();

        reportRoot.GetProperty("runAtUtc").GetString().Should().NotBeNullOrWhiteSpace();
        reportRoot.GetProperty("verdict").GetString().Should().Be(TranslationImportConstants.PassVerdict);
        reportRoot.GetProperty("persisted").GetBoolean().Should().BeTrue();
        reportRoot.GetProperty("forced").GetBoolean().Should().BeFalse();
        reportRoot.GetProperty("sourcePath").GetString().Should().NotBeNullOrWhiteSpace();

        AssertTotalsShape(reportRoot.GetProperty("totals"));
        AssertSourceSummariesShape(reportRoot.GetProperty("sourceSummaries"));
        reportRoot.GetProperty("excludedSourceSummaries").EnumerateArray().Should().BeEmpty();

        var checks = reportRoot.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().NotBeEmpty();
        checks.Select(check => check.GetProperty("id").GetString()).Should().Contain(AllFr032HardCheckIds);
        checks.Should().OnlyContain(check => check.GetProperty("passed").GetBoolean());

        reportRoot.GetProperty("warnings").EnumerateArray().Should().NotBeEmpty();
        reportRoot.GetProperty("errors").EnumerateArray().Should().BeEmpty();
        reportRoot.GetProperty("infoNotes").EnumerateArray().Should().NotBeEmpty();

        File.Exists(Path.Combine(reportDir, TranslationImportConstants.JsonReportFileName)).Should().BeTrue();
        File.Exists(Path.Combine(reportDir, TranslationImportConstants.MarkdownReportFileName)).Should().BeTrue();
    }

    [Fact]
    public async Task Validation_failure_json_report_contains_failed_checks_errors_and_rollback_evidence()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.MinimalAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.MinimalSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();

        using var reportDocument = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(reportDir, TranslationImportConstants.JsonReportFileName)));
        var reportRoot = reportDocument.RootElement;

        reportRoot.GetProperty("verdict").GetString().Should().Be(TranslationImportConstants.FailVerdict);
        reportRoot.GetProperty("persisted").GetBoolean().Should().BeFalse();
        reportRoot.GetProperty("errors").EnumerateArray().Should().NotBeEmpty();

        var checks = reportRoot.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == TranslationInvariants.CheckRollbackOnFail
            && check.GetProperty("passed").GetBoolean());
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == TranslationInvariants.CheckCoverageCount
            && !check.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public async Task Refusal_json_report_contains_rerun_guard_and_refusal_errors()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.DefaultSources);
        var reportDir1 = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");
        var reportDir2 = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var first = await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir1);
        first.Succeeded.Should().BeTrue(first.Message);

        var second = await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir2);
        second.Succeeded.Should().BeFalse();
        second.ExitCode.Should().Be(ImportTranslationsResult.RefusedExitCode);

        using var reportDocument = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(reportDir2, TranslationImportConstants.JsonReportFileName)));
        var reportRoot = reportDocument.RootElement;

        reportRoot.GetProperty("verdict").GetString().Should().Be(TranslationImportConstants.FailVerdict);
        reportRoot.GetProperty("persisted").GetBoolean().Should().BeFalse();
        reportRoot.GetProperty("errors").EnumerateArray().Should().Contain(error =>
            error.GetString()!.Contains(TranslationInvariants.TargetsNotEmpty, StringComparison.Ordinal));

        var checks = reportRoot.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == TranslationInvariants.CheckRerunGuard
            && !check.GetProperty("passed").GetBoolean());
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == TranslationInvariants.CheckRollbackOnFail
            && check.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public async Task Success_markdown_report_contains_verdict_totals_checks_warnings_and_reclassified_sources()
    {
        var (reportDir, _) = await RunSuccessfulReportImportAsync();
        var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir, TranslationImportConstants.MarkdownReportFileName));

        markdown.Should().Contain("# Quran Translation Import Report");
        markdown.Should().Contain("Verdict: PASS");
        markdown.Should().Contain("Persisted: true");
        markdown.Should().Contain("Forced: false");
        markdown.Should().Contain("Source path:");
        markdown.Should().Contain("## Totals");
        markdown.Should().Contain("## Hard Checks");
        markdown.Should().Contain(TranslationInvariants.CheckReportWritten);
        markdown.Should().Contain("## Warnings");
        markdown.Should().Contain(TranslationInvariants.WarningProvenance);
        markdown.Should().Contain("## Informational Notes");
        markdown.Should().Contain(TranslationInvariants.InfoLanguageCoverage);
        markdown.Should().Contain("## Reclassified Sources");
        markdown.Should().Contain("en-test-reclassified");
    }

    [Fact]
    public async Task Validation_failure_markdown_report_contains_verdict_errors_and_failed_checks()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.MinimalAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.MinimalSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);
        result.Succeeded.Should().BeFalse();

        var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir, TranslationImportConstants.MarkdownReportFileName));
        markdown.Should().Contain("Verdict: FAIL");
        markdown.Should().Contain("Persisted: false");
        markdown.Should().Contain("## Hard Checks");
        markdown.Should().Contain("## Errors");
        markdown.Should().Contain(TranslationInvariants.CheckCoverageCount);
    }

    [Fact]
    public async Task Refusal_markdown_report_contains_verdict_forced_source_path_and_rerun_guard()
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

        await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir2);

        var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir2, TranslationImportConstants.MarkdownReportFileName));
        markdown.Should().Contain("Verdict: FAIL");
        markdown.Should().Contain("Persisted: false");
        markdown.Should().Contain("Forced: false");
        markdown.Should().Contain("Source path:");
        markdown.Should().Contain(TranslationInvariants.CheckRerunGuard);
        markdown.Should().Contain(TranslationInvariants.TargetsNotEmpty);
    }

    [Fact]
    public async Task Success_reports_include_provenance_warning_inline_markup_language_coverage_and_reclassified_notes()
    {
        var (_, reportRoot) = await RunSuccessfulReportImportAsync();

        var warnings = reportRoot.GetProperty("warnings").EnumerateArray()
            .Select(warning => warning.GetString())
            .ToList();
        warnings.Should().Contain(warning =>
            warning!.StartsWith(TranslationInvariants.WarningProvenance, StringComparison.Ordinal));

        var infoNotes = reportRoot.GetProperty("infoNotes").EnumerateArray()
            .Select(note => note.GetString())
            .ToList();
        infoNotes.Should().Contain(note =>
            note!.StartsWith(TranslationInvariants.InfoInlineMarkup, StringComparison.Ordinal));
        infoNotes.Should().Contain(note =>
            note!.StartsWith(TranslationInvariants.InfoLanguageCoverage, StringComparison.Ordinal));
        infoNotes.Should().Contain(note =>
            note!.StartsWith(TranslationInvariants.InfoReclassified, StringComparison.Ordinal));
    }

    private async Task<(string ReportDir, JsonElement ReportRoot)> RunSuccessfulReportImportAsync()
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

        var reportJson = await File.ReadAllTextAsync(Path.Combine(reportDir, TranslationImportConstants.JsonReportFileName));
        using var reportDocument = JsonDocument.Parse(reportJson);
        return (reportDir, reportDocument.RootElement.Clone());
    }

    private static void AssertTotalsShape(JsonElement totals)
    {
        totals.GetProperty("sourceRows").GetInt32().Should().BePositive();
        totals.GetProperty("ayahMappingRows").GetInt64().Should().BePositive();
        totals.GetProperty("approvedSources").GetInt32().Should().BePositive();
        totals.GetProperty("simpleSources").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        totals.GetProperty("withFootnotesSources").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        totals.GetProperty("excludedSources").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        totals.GetProperty("languageCount").GetInt32().Should().BePositive();
        totals.GetProperty("distinctAyahs").GetInt32().Should().BePositive();
    }

    private static void AssertSourceSummariesShape(JsonElement sourceSummaries)
    {
        var summaries = sourceSummaries.EnumerateArray().ToList();
        summaries.Should().NotBeEmpty();

        foreach (var summary in summaries)
        {
            summary.GetProperty("sourceKey").GetString().Should().NotBeNullOrWhiteSpace();
            summary.GetProperty("languageCode").GetString().Should().NotBeNullOrWhiteSpace();
            summary.GetProperty("direction").GetString().Should().NotBeNullOrWhiteSpace();
            summary.GetProperty("translationType").GetString().Should().NotBeNullOrWhiteSpace();
            summary.GetProperty("displayNameEn").GetString().Should().NotBeNullOrWhiteSpace();
            summary.GetProperty("displayNameAr").GetString().Should().NotBeNullOrWhiteSpace();
            summary.GetProperty("packageFile").GetString().Should().NotBeNullOrWhiteSpace();
            summary.GetProperty("sha256").GetString().Should().NotBeNullOrWhiteSpace();
            summary.GetProperty("fileSizeBytes").GetInt64().Should().BePositive();
            summary.TryGetProperty("containsInlineFootnotes", out _).Should().BeTrue();
            summary.TryGetProperty("containsHtmlMarkup", out _).Should().BeTrue();
            summary.TryGetProperty("reclassifiedFromSimpleByContent", out _).Should().BeTrue();
        }
    }
}
