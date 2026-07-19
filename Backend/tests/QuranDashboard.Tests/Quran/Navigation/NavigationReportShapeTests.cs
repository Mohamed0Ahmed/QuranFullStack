using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Application.Quran.DataPipelines.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

[Collection(nameof(NavigationImportTestCollection))]
public sealed class NavigationReportShapeTests(NavigationImportTestFixture fixture)
{
    private static readonly string[] DbDependentCheckIdsNotEvaluatedOnRerunRefusal =
    [
        NavigationMetadataInvariants.CheckVerseKeysResolve,
        NavigationMetadataInvariants.CheckRangeCoverageJuz,
        NavigationMetadataInvariants.CheckRangeCoverageHizb,
        NavigationMetadataInvariants.CheckRangeCoverageRub,
        NavigationMetadataInvariants.CheckNoRangeGapsOverlaps,
        NavigationMetadataInvariants.CheckHierarchy,
        NavigationMetadataInvariants.CheckAyahColumnsComplete,
        NavigationMetadataInvariants.CheckSourceUnchanged,
        NavigationMetadataInvariants.CheckReportWritten
    ];

    private static readonly string[] AllHardCheckIds =
    [
        NavigationMetadataInvariants.CheckPackageShape,
        NavigationMetadataInvariants.CheckManifestFinal,
        NavigationMetadataInvariants.CheckSourceCount,
        NavigationMetadataInvariants.CheckSourceHash,
        NavigationMetadataInvariants.CheckJsonShape,
        NavigationMetadataInvariants.CheckVerseKeysResolve,
        NavigationMetadataInvariants.CheckRangeCoverageJuz,
        NavigationMetadataInvariants.CheckRangeCoverageHizb,
        NavigationMetadataInvariants.CheckRangeCoverageRub,
        NavigationMetadataInvariants.CheckNoRangeGapsOverlaps,
        NavigationMetadataInvariants.CheckHierarchy,
        NavigationMetadataInvariants.CheckSajdaType,
        NavigationMetadataInvariants.CheckAyahColumnsComplete,
        NavigationMetadataInvariants.CheckNoQuranTextCopy,
        NavigationMetadataInvariants.CheckSourceUnchanged,
        NavigationMetadataInvariants.CheckReportWritten,
        NavigationMetadataInvariants.CheckRollbackOnFail,
        NavigationMetadataInvariants.CheckRerunGuard
    ];

    [Fact]
    public async Task Success_json_report_contains_required_top_level_fields_and_all_hard_checks()
    {
        var (reportDir, reportRoot) = await RunSuccessfulReportImportAsync();

        reportRoot.GetProperty("feature").GetString().Should().Be(NavigationImportConstants.FeatureId);
        reportRoot.GetProperty("runAtUtc").GetString().Should().NotBeNullOrWhiteSpace();
        reportRoot.GetProperty("verdict").GetString().Should().Be(NavigationImportConstants.AcceptedVerdict);
        reportRoot.GetProperty("persisted").GetBoolean().Should().BeTrue();
        reportRoot.GetProperty("forced").GetBoolean().Should().BeFalse();
        reportRoot.GetProperty("sourcePath").GetString().Should().NotBeNullOrWhiteSpace();
        reportRoot.GetProperty("noQuranAyahTextReadOrStored").GetBoolean().Should().BeTrue();

        AssertManifestShape(reportRoot.GetProperty("manifest"));
        AssertTotalsShape(reportRoot.GetProperty("totals"), expectedAyahsTagged: 6);
        AssertAyahCoverageShape(reportRoot.GetProperty("ayahCoverage"), totalAyahs: 6, taggedAyahs: 6, complete: true);

        var checks = reportRoot.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().NotBeEmpty();
        checks.Select(check => check.GetProperty("id").GetString()).Should().Contain(AllHardCheckIds);
        checks
            .Where(check => check.GetProperty("severity").GetString() == NavigationImportConstants.HardSeverity)
            .Should()
            .OnlyContain(check => check.GetProperty("passed").GetBoolean());

        reportRoot.GetProperty("warnings").EnumerateArray().Should().NotBeEmpty();
        reportRoot.GetProperty("errors").EnumerateArray().Should().BeEmpty();

        File.Exists(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName)).Should().BeTrue();
        File.Exists(Path.Combine(reportDir, NavigationImportConstants.MarkdownReportFileName)).Should().BeTrue();
    }

    [Fact]
    public async Task Validation_failure_json_report_contains_failed_checks_errors_and_rollback_evidence()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            NavigationSyntheticSeed.JuzGapPackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeFalse();

        using var reportDocument = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName)));
        var reportRoot = reportDocument.RootElement;

        reportRoot.GetProperty("verdict").GetString().Should().Be(NavigationImportConstants.ValidationFailedVerdict);
        reportRoot.GetProperty("persisted").GetBoolean().Should().BeFalse();
        reportRoot.GetProperty("noQuranAyahTextReadOrStored").GetBoolean().Should().BeTrue();
        reportRoot.GetProperty("errors").EnumerateArray().Should().NotBeEmpty();
        AssertAyahCoverageShape(reportRoot.GetProperty("ayahCoverage"), totalAyahs: 6, taggedAyahs: 0, complete: false);

        var checks = reportRoot.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == NavigationMetadataInvariants.CheckRollbackOnFail
            && check.GetProperty("passed").GetBoolean());
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == NavigationMetadataInvariants.CheckRangeCoverageJuz
            && !check.GetProperty("passed").GetBoolean());
        checks.Select(check => check.GetProperty("id").GetString()).Should().Contain(NavigationMetadataInvariants.CheckPackageShape);
    }

    [Fact]
    public async Task Refusal_json_report_contains_rerun_guard_and_refusal_errors()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var reportDir1 = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");
        var reportDir2 = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var first = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir1);
        first.Succeeded.Should().BeTrue(first.Message);

        var second = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir2);
        second.Succeeded.Should().BeFalse();
        second.ExitCode.Should().Be(ImportNavigationMetadataResult.RefusedExitCode);

        using var reportDocument = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(reportDir2, NavigationImportConstants.JsonReportFileName)));
        var reportRoot = reportDocument.RootElement;

        reportRoot.GetProperty("verdict").GetString().Should().Be(NavigationImportConstants.RefusedVerdict);
        reportRoot.GetProperty("persisted").GetBoolean().Should().BeFalse();
        reportRoot.GetProperty("noQuranAyahTextReadOrStored").GetBoolean().Should().BeTrue();
        reportRoot.GetProperty("errors").EnumerateArray().Should().Contain(error =>
            error.GetString()!.Contains(NavigationMetadataInvariants.TargetsNotEmpty, StringComparison.Ordinal));

        var checks = reportRoot.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == NavigationMetadataInvariants.CheckRerunGuard
            && !check.GetProperty("passed").GetBoolean());
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == NavigationMetadataInvariants.CheckRollbackOnFail
            && check.GetProperty("passed").GetBoolean());
        checks.Select(check => check.GetProperty("id").GetString()).Should().Contain(NavigationMetadataInvariants.CheckPackageShape);
        checks.Select(check => check.GetProperty("id").GetString())
            .Should()
            .NotContain(DbDependentCheckIdsNotEvaluatedOnRerunRefusal);
    }

    [Fact]
    public async Task Refusal_json_report_omits_db_dependent_checks_not_evaluated_before_rerun_guard()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var reportDir1 = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");
        var reportDir2 = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir1);

        await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir2);

        using var reportDocument = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(reportDir2, NavigationImportConstants.JsonReportFileName)));
        var checkIds = reportDocument.RootElement
            .GetProperty("checks")
            .EnumerateArray()
            .Select(check => check.GetProperty("id").GetString())
            .ToList();

        checkIds.Should().Contain(NavigationMetadataInvariants.CheckPackageShape);
        checkIds.Should().Contain(NavigationMetadataInvariants.CheckSourceCount);
        checkIds.Should().Contain(NavigationMetadataInvariants.CheckSajdaType);
        checkIds.Should().NotContain(DbDependentCheckIdsNotEvaluatedOnRerunRefusal);
    }

    [Fact]
    public async Task Success_markdown_report_contains_verdict_totals_coverage_checks_warnings_and_no_text_statement()
    {
        var (reportDir, _) = await RunSuccessfulReportImportAsync();
        var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir, NavigationImportConstants.MarkdownReportFileName));

        markdown.Should().Contain("# Quran Navigation Metadata Import Report");
        markdown.Should().Contain($"Verdict: **{NavigationImportConstants.AcceptedVerdict}**");
        markdown.Should().Contain("Persisted: true");
        markdown.Should().Contain("Forced: false");
        markdown.Should().Contain("Source path:");
        markdown.Should().Contain("## Totals");
        markdown.Should().Contain("## Ayah coverage");
        markdown.Should().Contain("## Checks");
        markdown.Should().Contain(NavigationMetadataInvariants.CheckReportWritten);
        markdown.Should().Contain("## Warnings");
        markdown.Should().Contain(NavigationMetadataInvariants.WarningVerseCountMatch);
        markdown.Should().Contain("No Quran ayah text was read or stored by this import.");
    }

    [Fact]
    public async Task Validation_failure_markdown_report_contains_verdict_errors_and_failed_checks()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            NavigationSyntheticSeed.JuzGapPackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);
        result.Succeeded.Should().BeFalse();

        var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir, NavigationImportConstants.MarkdownReportFileName));
        markdown.Should().Contain($"Verdict: **{NavigationImportConstants.ValidationFailedVerdict}**");
        markdown.Should().Contain("Persisted: false");
        markdown.Should().Contain("## Checks");
        markdown.Should().Contain("## Errors");
        markdown.Should().Contain(NavigationMetadataInvariants.CheckRangeCoverageJuz);
        markdown.Should().Contain("No Quran ayah text was read or stored by this import.");
    }

    [Fact]
    public async Task Refusal_markdown_report_contains_verdict_forced_source_path_and_rerun_guard()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var reportDir1 = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");
        var reportDir2 = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir1);

        await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir2);

        var markdown = await File.ReadAllTextAsync(Path.Combine(reportDir2, NavigationImportConstants.MarkdownReportFileName));
        markdown.Should().Contain($"Verdict: **{NavigationImportConstants.RefusedVerdict}**");
        markdown.Should().Contain("Persisted: false");
        markdown.Should().Contain("Forced: false");
        markdown.Should().Contain("Source path:");
        markdown.Should().Contain(NavigationMetadataInvariants.CheckRerunGuard);
        markdown.Should().Contain(NavigationMetadataInvariants.TargetsNotEmpty);
        markdown.Should().Contain("No Quran ayah text was read or stored by this import.");
    }

    [Fact]
    public async Task Success_reports_include_passing_warning_checks_when_counts_and_sajda_distribution_match()
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

        using var reportDocument = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName)));
        var checks = reportDocument.RootElement.GetProperty("checks").EnumerateArray().ToList();

        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == NavigationMetadataInvariants.WarningVerseCountMatch
            && check.GetProperty("passed").GetBoolean());
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == NavigationMetadataInvariants.WarningSajdaDistribution
            && check.GetProperty("passed").GetBoolean());
        reportDocument.RootElement.GetProperty("warnings").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Report_write_failure_after_validation_keeps_no_navigation_changes()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(NavigationSyntheticSeed.DefaultTestExpectedCounts);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        await using var scope = fixture.CreateServiceProvider(services =>
        {
            services.AddSingleton<INavigationMetadataReportWriter, FailingNavigationReportWriter>();
        }).CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<ImportNavigationMetadataHandler>();
        var result = await handler.HandleAsync(
            new ImportNavigationMetadataCommand(packageDir, Force: false, NavigationSyntheticSeed.DefaultTestExpectedCounts, reportDir),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(NavigationMetadataInvariants.ReportRequired);

        var snapshot = await fixture.CaptureNavigationSnapshotAsync();
        snapshot.JuzRows.Should().Be(0);
        snapshot.HizbRows.Should().Be(0);
        snapshot.RubRows.Should().Be(0);
        snapshot.SajdaRows.Should().Be(0);
        snapshot.TaggedAyahRows.Should().Be(0);
    }

    private async Task<(string ReportDir, JsonElement ReportRoot)> RunSuccessfulReportImportAsync()
    {
        await fixture.TruncateNavigationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(NavigationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            NavigationSyntheticSeed.ReportTestPackageSpec);
        var reportDir = Path.Combine(Path.GetTempPath(), $"navigation-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            NavigationSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);

        var reportJson = await File.ReadAllTextAsync(Path.Combine(reportDir, NavigationImportConstants.JsonReportFileName));
        using var reportDocument = JsonDocument.Parse(reportJson);
        return (reportDir, reportDocument.RootElement.Clone());
    }

    private static void AssertManifestShape(JsonElement manifest)
    {
        manifest.GetProperty("packageType").GetString().Should().Be(NavigationImportConstants.ManifestType);
        manifest.GetProperty("isFinalImportManifest").GetBoolean().Should().BeTrue();
    }

    private static void AssertTotalsShape(JsonElement totals, int expectedAyahsTagged)
    {
        totals.GetProperty("juz").GetInt32().Should().BePositive();
        totals.GetProperty("hizb").GetInt32().Should().BePositive();
        totals.GetProperty("rub").GetInt32().Should().BePositive();
        totals.GetProperty("sajda").GetInt32().Should().BePositive();
        totals.GetProperty("ayahsTagged").GetInt32().Should().Be(expectedAyahsTagged);
    }

    private static void AssertAyahCoverageShape(JsonElement ayahCoverage, int totalAyahs, int taggedAyahs, bool complete)
    {
        ayahCoverage.GetProperty("totalAyahs").GetInt32().Should().Be(totalAyahs);
        ayahCoverage.GetProperty("withJuz").GetInt32().Should().Be(taggedAyahs);
        ayahCoverage.GetProperty("withHizb").GetInt32().Should().Be(taggedAyahs);
        ayahCoverage.GetProperty("withRub").GetInt32().Should().Be(taggedAyahs);
        ayahCoverage.GetProperty("complete").GetBoolean().Should().Be(complete);
    }
}

internal sealed class FailingNavigationReportWriter : INavigationMetadataReportWriter
{
    public Task WriteAsync(NavigationMetadataImportReport report, string reportOutDir, CancellationToken ct) =>
        throw new IOException("simulated navigation report write failure");
}
