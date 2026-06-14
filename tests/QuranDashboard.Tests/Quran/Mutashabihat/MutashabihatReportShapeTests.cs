using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;
using QuranDashboard.Application.Quran.Mutashabihat.ImportMutashabihat;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

[Collection(nameof(MutashabihatImportTestCollection))]
public sealed class MutashabihatReportShapeTests(MutashabihatImportTestFixture fixture)
{
    private static readonly MutashabihatExpectedCounts SyntheticExpected = new(1, 2, 2, 1, 1, 2);

    [Fact]
    public async Task Successful_Import_Writes_Markdown_And_Json_Report_With_Required_Shape()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var (result, reportDir) = await fixture.RunImportWithReportAsync(
            sourcePath,
            expectedCounts: SyntheticExpected);

        result.Succeeded.Should().BeTrue();
        result.ReportOutDir.Should().Be(reportDir);

        var jsonPath = MutashabihatImportTestFixture.GetJsonReportPath(reportDir);
        var markdownPath = MutashabihatImportTestFixture.GetMarkdownReportPath(reportDir);

        File.Exists(jsonPath).Should().BeTrue();
        File.Exists(markdownPath).Should().BeTrue();

        await using var jsonStream = File.OpenRead(jsonPath);
        using var document = await JsonDocument.ParseAsync(jsonStream);
        var root = document.RootElement;

        root.GetProperty("verdict").GetString().Should().Be("pass");
        root.GetProperty("persisted").GetBoolean().Should().BeTrue();

        var totals = root.GetProperty("totals");
        totals.GetProperty("groupRows").GetInt32().Should().Be(1);
        totals.GetProperty("rawOccurrenceEntries").GetInt32().Should().Be(2);
        totals.GetProperty("storedOccurrenceRows").GetInt32().Should().Be(2);
        totals.GetProperty("linkRows").GetInt32().Should().Be(1);
        totals.GetProperty("distinctSimilarSources").GetInt32().Should().Be(1);
        totals.GetProperty("distinctReferencedAyahs").GetInt32().Should().Be(2);

        var checks = await MutashabihatReportTestSupport.ReadChecksAsync(reportDir);

        MutashabihatReportTestSupport.AssertCheck(
            checks, MutashabihatInvariants.CheckCoverageGt100, "warning", "0");
        MutashabihatReportTestSupport.AssertCheck(
            checks, MutashabihatInvariants.CheckDuplicateOccurrence, "warning", "0");
        MutashabihatReportTestSupport.AssertCheck(
            checks, MutashabihatInvariants.CheckSourceKeyAbsent, "warning", "0");
        MutashabihatReportTestSupport.AssertCheck(
            checks, MutashabihatInvariants.CheckStaleSourceCounters, "warning", "0");
        MutashabihatReportTestSupport.AssertCheck(
            checks, MutashabihatInvariants.CheckProvenanceLicenseUnknown, "warning", "2");
        MutashabihatReportTestSupport.AssertCheck(
            checks, MutashabihatInvariants.CheckWordRangeUpperBound, "warning", "0");

        MutashabihatReportTestSupport.AssertCheck(
            checks, MutashabihatInvariants.CheckOnewayLinks, "info", "~1");
        MutashabihatReportTestSupport.AssertCheck(
            checks, MutashabihatInvariants.CheckCrossDatasetOverlap, "info", "~2 ayahs / ~1 pairs");
        MutashabihatReportTestSupport.AssertCheck(
            checks, MutashabihatInvariants.CheckSurahCoverage, "info", "1/114 surahs; 2 distinct ayahs");

        checks.Should().NotContainKey(MutashabihatInvariants.CheckPhraseVersesConsistency);

        var markdown = await File.ReadAllTextAsync(markdownPath);
        markdown.Should().Contain("# Quran Mutashabihat — Import Report");
        markdown.Should().Contain("quran_mutashabihat_groups");
        markdown.Should().Contain("raw source occurrence entries");
        markdown.Should().Contain(MutashabihatInvariants.CheckCoverageGt100);
        markdown.Should().Contain(MutashabihatInvariants.CheckProvenanceLicenseUnknown);
        markdown.Should().Contain("1/114 surahs; 2 distinct ayahs");
    }

    [Fact]
    public async Task Failed_Import_Still_Writes_Report_With_Fail_Verdict()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSourceFolderWithValidationViolationAsync();

        var (result, reportDir) = await fixture.RunImportWithReportAsync(
            sourcePath,
            expectedCounts: SyntheticExpected);

        result.Succeeded.Should().BeFalse();
        File.Exists(MutashabihatImportTestFixture.GetJsonReportPath(reportDir)).Should().BeTrue();

        await using var jsonStream = File.OpenRead(MutashabihatImportTestFixture.GetJsonReportPath(reportDir));
        using var document = await JsonDocument.ParseAsync(jsonStream);

        document.RootElement.GetProperty("verdict").GetString().Should().Be("fail");
        document.RootElement.GetProperty("persisted").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("errors").EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Early_Refusal_With_Explicit_Report_Dir_Writes_No_Report_Artifacts()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var first = await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);
        first.Succeeded.Should().BeTrue();

        var reportDir = Path.Combine(Path.GetTempPath(), $"mutashabihat-refusal-report-{Guid.NewGuid():N}");

        var second = await fixture.RunImportAsync(
            sourcePath,
            expectedCounts: SyntheticExpected,
            reportOutDir: reportDir);

        second.Succeeded.Should().BeFalse();
        second.ExitCode.Should().Be(ImportMutashabihatResult.RefusedExitCode);
        second.ReportOutDir.Should().BeNull();
        MutashabihatReportTestSupport.AssertNoReportArtifacts(reportDir);
    }

    [Fact]
    public async Task Missing_Foundation_Refusal_With_Explicit_Report_Dir_Writes_No_Report_Artifacts()
    {
        await fixture.SeedEmptyFoundationAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"mutashabihat-missing-report-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedCounts: SyntheticExpected,
            reportOutDir: reportDir);

        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(ImportMutashabihatResult.RefusedExitCode);
        result.ReportOutDir.Should().BeNull();
        MutashabihatReportTestSupport.AssertNoReportArtifacts(reportDir);
    }
}
