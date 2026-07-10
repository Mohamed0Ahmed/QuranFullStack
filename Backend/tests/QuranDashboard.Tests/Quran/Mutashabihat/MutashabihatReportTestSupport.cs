
namespace QuranDashboard.Tests.Quran.Mutashabihat;

internal static class MutashabihatReportTestSupport
{
    public static async Task<IReadOnlyDictionary<string, ReportCheckSnapshot>> ReadChecksAsync(string reportDir)
    {
        var json = await File.ReadAllTextAsync(MutashabihatImportTestFixture.GetJsonReportPath(reportDir));
        using var document = JsonDocument.Parse(json);

        return document.RootElement
            .GetProperty("checks")
            .EnumerateArray()
            .Select(check => new ReportCheckSnapshot(
                check.GetProperty("id").GetString()!,
                check.GetProperty("severity").GetString()!,
                check.GetProperty("expected").GetString()!,
                check.GetProperty("observed").GetString()!,
                check.GetProperty("passed").GetBoolean()))
            .ToDictionary(check => check.Id, StringComparer.Ordinal);
    }

    public static void AssertCheck(
        IReadOnlyDictionary<string, ReportCheckSnapshot> checks,
        string id,
        string severity,
        string observed)
    {
        checks.Should().ContainKey(id);
        var check = checks[id];
        check.Severity.Should().Be(severity);
        check.Observed.Should().Be(observed);
        check.Passed.Should().BeTrue();
    }

    public static void AssertNoReportArtifacts(string reportDir)
    {
        File.Exists(MutashabihatImportTestFixture.GetJsonReportPath(reportDir)).Should().BeFalse();
        File.Exists(MutashabihatImportTestFixture.GetMarkdownReportPath(reportDir)).Should().BeFalse();
    }

    internal sealed record ReportCheckSnapshot(
        string Id,
        string Severity,
        string Expected,
        string Observed,
        bool Passed);
}
