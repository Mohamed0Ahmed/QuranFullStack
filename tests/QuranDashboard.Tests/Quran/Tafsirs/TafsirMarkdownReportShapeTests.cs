using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirMarkdownReportShapeTests(TafsirImportTestFixture fixture)
{
    [Fact]
    public async Task Successful_import_markdown_report_contains_required_sections()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.TwoSourceIntegrationSources,
            excludedSourceKeys: ["ar-wajiz"]);
        var reportDir = Path.Combine(Path.GetTempPath(), $"tafsir-md-shape-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.TwoSourceTestExpectedCounts with { ExcludedSources = 1 },
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);

        var markdown = await File.ReadAllTextAsync(
            Path.Combine(reportDir, TafsirImportConstants.MarkdownReportFileName));

        markdown.Should().Contain("# Quran Tafsir Import Report");
        markdown.Should().Contain("- Verdict: PASS");
        markdown.Should().Contain("- Persisted: true");
        markdown.Should().Contain($"- Source path: {Path.GetFullPath(packageDir)}");
        markdown.Should().Contain("## Totals");
        markdown.Should().Contain("| Sources | 2 |");
        markdown.Should().Contain("| Text blocks | 4 |");
        markdown.Should().Contain("| Source-to-ayah mappings | 6 |");
        markdown.Should().Contain("| Excluded sources | 1 |");
        markdown.Should().Contain("| Languages | 2 |");
        markdown.Should().Contain("## Hard Checks");
        markdown.Should().Contain($"| {TafsirInvariants.CheckReportWritten} |");
        markdown.Should().Contain("## Warnings");
        markdown.Should().Contain("## Excluded Sources");
        markdown.Should().Contain("| ar-wajiz |");
    }

    [Fact]
    public async Task Refused_import_markdown_report_contains_verdict_and_persisted_flag()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.IntegrationSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"tafsir-md-refusal-{Guid.NewGuid():N}");

        var firstRun = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);
        firstRun.Succeeded.Should().BeTrue(firstRun.Message);

        var secondRun = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        secondRun.Succeeded.Should().BeFalse();
        File.Exists(Path.Combine(reportDir, TafsirImportConstants.MarkdownReportFileName)).Should().BeTrue();

        var markdown = await File.ReadAllTextAsync(
            Path.Combine(reportDir, TafsirImportConstants.MarkdownReportFileName));

        markdown.Should().Contain("- Verdict: FAIL");
        markdown.Should().Contain("- Persisted: false");
        markdown.Should().Contain("## Errors");
        markdown.Should().Contain(TafsirInvariants.TargetsNotEmpty);
    }

    [Fact]
    public async Task Failed_validation_markdown_report_contains_hard_checks_table()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.IntegrationSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"tafsir-md-fail-{Guid.NewGuid():N}");

        await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        var wrongExpectedCounts = TafsirSyntheticSeed.DefaultTestExpectedCounts with
        {
            SourceAyahMappings = 99
        };

        var failedRun = await fixture.RunImportAsync(
            packageDir,
            wrongExpectedCounts,
            reportDir,
            force: true);

        failedRun.Succeeded.Should().BeFalse();
        failedRun.Message.Should().Contain(TafsirInvariants.CheckPostCopyAyahMappings);

        var markdown = await File.ReadAllTextAsync(
            Path.Combine(reportDir, TafsirImportConstants.MarkdownReportFileName));

        markdown.Should().Contain("- Verdict: FAIL");
        markdown.Should().Contain("- Persisted: false");
        markdown.Should().Contain("- Forced: true");
        markdown.Should().Contain("## Hard Checks");
        markdown.Should().Contain($"| {TafsirInvariants.CheckPostCopyAyahMappings} |");
        markdown.Should().Contain("| no |");
    }
}
