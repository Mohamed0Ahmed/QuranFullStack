using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Tests.Quran.FullI3rab;

[Collection(nameof(FullI3rabImportTestCollection))]
public sealed class FullI3rabMarkdownReportShapeTests(FullI3rabImportTestFixture fixture) : IDisposable
{
    private readonly FullI3rabSyntheticPackage packages = new();

    [Fact]
    public async Task Successful_import_markdown_report_contains_required_sections()
    {
        await fixture.TruncateFullI3rabTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(FullI3rabSyntheticSeed.DefaultAyahs);

        var packageDir = await packages.WriteAsync();
        var reportDir = fixture.CreateTempDir();

        var result = await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3),
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);

        var markdown = await File.ReadAllTextAsync(
            Path.Combine(reportDir, FullI3rabImportConstants.MarkdownReportFileName));

        markdown.Should().Contain("# Quran Full I'rab Import Report");
        markdown.Should().Contain("- Verdict: PASS");
        markdown.Should().Contain("- Persisted: true");
        markdown.Should().Contain($"- Source path: {Path.GetFullPath(packageDir)}");
        markdown.Should().Contain("## Provenance");
        markdown.Should().Contain($"licenseStatus: {FullI3rabImportConstants.LicenseStatus}");
        markdown.Should().Contain($"provenanceStatus: {FullI3rabImportConstants.ProvenanceStatus}");
        markdown.Should().Contain($"usageScope: {FullI3rabImportConstants.UsageScope}");
        markdown.Should().Contain("internal use only until cleared");
        markdown.Should().Contain("## Totals");
        markdown.Should().Contain("| Sources | 1 |");
        markdown.Should().Contain("| Entries | 2 |");
        markdown.Should().Contain("| Source-to-ayah mappings | 3 |");
        markdown.Should().Contain("## Hard Checks");
        markdown.Should().Contain($"| {FullI3rabInvariants.CheckReportWritten} |");
        markdown.Should().Contain("## Warnings");
        markdown.Should().Contain(FullI3rabInvariants.WarningProvenance);
        markdown.Should().Contain("## Source Summaries");
    }

    [Fact]
    public async Task Refused_import_markdown_report_contains_verdict_and_persisted_flag()
    {
        await fixture.TruncateFullI3rabTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(FullI3rabSyntheticSeed.DefaultAyahs);

        var packageDir = await packages.WriteAsync();
        var reportDir = fixture.CreateTempDir();

        var firstRun = await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3),
            reportDir);
        firstRun.Succeeded.Should().BeTrue(firstRun.Message);

        var secondRun = await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3),
            reportDir);

        secondRun.Succeeded.Should().BeFalse();
        File.Exists(Path.Combine(reportDir, FullI3rabImportConstants.MarkdownReportFileName)).Should().BeTrue();

        var markdown = await File.ReadAllTextAsync(
            Path.Combine(reportDir, FullI3rabImportConstants.MarkdownReportFileName));

        markdown.Should().Contain("- Verdict: FAIL");
        markdown.Should().Contain("- Persisted: false");
        markdown.Should().Contain("## Errors");
        markdown.Should().Contain(FullI3rabInvariants.TargetsNotEmpty);
        markdown.Should().Contain("internal use only until cleared");
    }

    [Fact]
    public async Task Failed_validation_markdown_report_contains_hard_checks_table_and_no_persistence()
    {
        await fixture.TruncateFullI3rabTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(FullI3rabSyntheticSeed.DefaultAyahs);

        var packageDir = await packages.WriteAsync();
        var reportDir = fixture.CreateTempDir();

        await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3),
            reportDir);

        var beforeSnapshot = await fixture.CaptureFullI3rabTableSnapshotAsync();

        var failedRun = await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3, ExpectedAyahMappingTotal: 99),
            reportDir,
            force: true);

        failedRun.Succeeded.Should().BeFalse();
        failedRun.Message.Should().Contain(FullI3rabInvariants.CheckPostCopyAyahMappings);

        var markdown = await File.ReadAllTextAsync(
            Path.Combine(reportDir, FullI3rabImportConstants.MarkdownReportFileName));

        markdown.Should().Contain("- Verdict: FAIL");
        markdown.Should().Contain("- Persisted: false");
        markdown.Should().Contain("- Forced: true");
        markdown.Should().Contain("## Hard Checks");
        markdown.Should().Contain($"| {FullI3rabInvariants.CheckPostCopyAyahMappings} |");
        markdown.Should().Contain("| no |");

        var afterSnapshot = await fixture.CaptureFullI3rabTableSnapshotAsync();
        afterSnapshot.Should().Be(beforeSnapshot);
    }

    public void Dispose() => packages.Dispose();
}
