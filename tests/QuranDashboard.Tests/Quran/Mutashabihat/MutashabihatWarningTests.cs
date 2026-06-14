using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

[Collection(nameof(MutashabihatImportTestCollection))]
public sealed class MutashabihatWarningTests(MutashabihatImportTestFixture fixture)
{
    private static readonly MutashabihatExpectedCounts CoverageExpected = new(1, 2, 2, 1, 1, 2);
    private static readonly MutashabihatExpectedCounts DuplicateExpected = new(1, 3, 2, 1, 1, 2);
    private static readonly MutashabihatExpectedCounts SourceKeyAbsentExpected = new(1, 2, 2, 0, 0, 2);
    private static readonly MutashabihatExpectedCounts StaleCounterExpected = new(1, 2, 2, 0, 0, 2);
    private static readonly MutashabihatExpectedCounts WordRangeUpperBoundExpected = new(1, 2, 2, 0, 0, 2);

    [Fact]
    public async Task Coverage_Gt_100_Is_Recorded_But_Does_Not_Block_Commit()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSourceFolderWithCoverageGt100Async();

        var (result, reportDir) = await fixture.RunImportWithReportAsync(
            sourcePath,
            expectedCounts: CoverageExpected);

        result.Succeeded.Should().BeTrue();

        var (severity, observed, passed) = await ReadCheckAsync(reportDir, MutashabihatInvariants.CheckCoverageGt100);
        severity.Should().Be("warning");
        observed.Should().Be("1");
        passed.Should().BeTrue();

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.LinkRows.Should().Be(1);
    }

    [Fact]
    public async Task Duplicate_Occurrence_Is_Recorded_But_Does_Not_Block_Commit()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSourceFolderWithDuplicateOccurrenceAsync();

        var (result, reportDir) = await fixture.RunImportWithReportAsync(
            sourcePath,
            expectedCounts: DuplicateExpected);

        result.Succeeded.Should().BeTrue();

        var (severity, observed, passed) = await ReadCheckAsync(reportDir, MutashabihatInvariants.CheckDuplicateOccurrence);
        severity.Should().Be("warning");
        observed.Should().Be("1");
        passed.Should().BeTrue();

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.OccurrenceRows.Should().Be(2);
    }

    [Fact]
    public async Task Source_Key_Absent_Is_Recorded_But_Does_Not_Block_Commit()
    {
        await fixture.SeedSyntheticAyahsAsync((1, "900:1"), (2, "900:2"), (99, "900:99"));
        var sourcePath = await fixture.WriteSourceFolderWithSourceKeyAbsentAsync();

        var (result, reportDir) = await fixture.RunImportWithReportAsync(
            sourcePath,
            expectedCounts: SourceKeyAbsentExpected);

        result.Succeeded.Should().BeTrue();

        var (severity, observed, passed) = await ReadCheckAsync(reportDir, MutashabihatInvariants.CheckSourceKeyAbsent);
        severity.Should().Be("warning");
        observed.Should().Be("1");
        passed.Should().BeTrue();

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.GroupRows.Should().Be(1);
        snapshot.OccurrenceRows.Should().Be(2);
    }

    [Fact]
    public async Task Stale_Source_Counters_Are_Recorded_With_Recomputed_Values()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSourceFolderWithStaleCountersAsync();

        var (result, reportDir) = await fixture.RunImportWithReportAsync(
            sourcePath,
            expectedCounts: StaleCounterExpected);

        result.Succeeded.Should().BeTrue();

        var (severity, observed, passed) = await ReadCheckAsync(reportDir, MutashabihatInvariants.CheckStaleSourceCounters);
        severity.Should().Be("warning");
        observed.Should().Be("1");
        passed.Should().BeTrue();

        var markdown = await File.ReadAllTextAsync(
            MutashabihatImportTestFixture.GetMarkdownReportPath(reportDir));
        markdown.Should().Contain(MutashabihatInvariants.CheckStaleSourceCounters);
        markdown.Should().Contain("recomputed values win");
    }

    [Fact]
    public async Task Word_Range_Upper_Bound_Is_Recorded_But_Does_Not_Block_Commit()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSourceFolderWithWordRangeUpperBoundAsync();

        var (result, reportDir) = await fixture.RunImportWithReportAsync(
            sourcePath,
            expectedCounts: WordRangeUpperBoundExpected);

        result.Succeeded.Should().BeTrue();

        var checks = await MutashabihatReportTestSupport.ReadChecksAsync(reportDir);
        MutashabihatReportTestSupport.AssertCheck(
            checks,
            MutashabihatInvariants.CheckWordRangeUpperBound,
            "warning",
            "1");

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.OccurrenceRows.Should().Be(2);
    }

    [Fact]
    public async Task Provenance_License_Unknown_Count_Is_Recorded_For_Both_Source_Files()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var (result, reportDir) = await fixture.RunImportWithReportAsync(
            sourcePath,
            expectedCounts: CoverageExpected);

        result.Succeeded.Should().BeTrue();

        var checks = await MutashabihatReportTestSupport.ReadChecksAsync(reportDir);
        MutashabihatReportTestSupport.AssertCheck(
            checks,
            MutashabihatInvariants.CheckProvenanceLicenseUnknown,
            "warning",
            "2");
    }

    private static async Task<(string Severity, string Observed, bool Passed)> ReadCheckAsync(
        string reportDir,
        string checkId)
    {
        var json = await File.ReadAllTextAsync(MutashabihatImportTestFixture.GetJsonReportPath(reportDir));
        using var document = JsonDocument.Parse(json);

        var check = document.RootElement
            .GetProperty("checks")
            .EnumerateArray()
            .Single(element => element.GetProperty("id").GetString() == checkId);

        return (
            check.GetProperty("severity").GetString()!,
            check.GetProperty("observed").GetString()!,
            check.GetProperty("passed").GetBoolean());
    }
}
