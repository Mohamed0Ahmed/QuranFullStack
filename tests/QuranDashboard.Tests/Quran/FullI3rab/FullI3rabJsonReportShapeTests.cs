using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Tests.Quran.FullI3rab;

[Collection(nameof(FullI3rabImportTestCollection))]
public sealed class FullI3rabJsonReportShapeTests(FullI3rabImportTestFixture fixture) : IDisposable
{
    private readonly FullI3rabSyntheticPackage packages = new();

    [Fact]
    public async Task Successful_import_json_report_contains_required_top_level_fields()
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

        var reportJson = await File.ReadAllTextAsync(
            Path.Combine(reportDir, FullI3rabImportConstants.JsonReportFileName));
        using var document = JsonDocument.Parse(reportJson);
        var root = document.RootElement;

        root.TryGetProperty("runAtUtc", out _).Should().BeTrue();
        root.GetProperty("verdict").GetString().Should().Be(FullI3rabImportConstants.PassVerdict);
        root.GetProperty("persisted").GetBoolean().Should().BeTrue();
        root.GetProperty("forced").GetBoolean().Should().BeFalse();
        root.GetProperty("sourcePath").GetString().Should().Be(Path.GetFullPath(packageDir));
        root.GetProperty("licenseStatus").GetString().Should().Be(FullI3rabImportConstants.LicenseStatus);
        root.GetProperty("provenanceStatus").GetString().Should().Be(FullI3rabImportConstants.ProvenanceStatus);
        root.GetProperty("usageScope").GetString().Should().Be(FullI3rabImportConstants.UsageScope);
        root.GetProperty("provenanceWarning").GetString().Should().Contain("internal use only until cleared");

        var totals = root.GetProperty("totals");
        totals.GetProperty("sourceRows").GetInt32().Should().Be(1);
        totals.GetProperty("entryRows").GetInt64().Should().Be(2);
        totals.GetProperty("ayahMappingRows").GetInt64().Should().Be(3);
        totals.GetProperty("distinctAyahs").GetInt32().Should().Be(3);

        var sourceSummaries = root.GetProperty("sourceSummaries").EnumerateArray().ToList();
        sourceSummaries.Should().ContainSingle();
        sourceSummaries[0].GetProperty("sourceKey").GetString().Should().NotBeNullOrWhiteSpace();
        sourceSummaries[0].GetProperty("packageFile").GetString().Should().StartWith("sources/");
        sourceSummaries[0].GetProperty("sha256").GetString().Should().NotBeNullOrWhiteSpace();
        sourceSummaries[0].GetProperty("license").GetString().Should().Be("unknown");
        sourceSummaries[0].GetProperty("provenance").GetString().Should().Be("unknown");
        sourceSummaries[0].GetProperty("usageScope").GetString()
            .Should().Be(FullI3rabImportConstants.UsageScope);

        var hardChecks = root.GetProperty("checks").EnumerateArray()
            .Where(check => check.GetProperty("severity").GetString() == FullI3rabImportConstants.HardSeverity)
            .ToList();
        var hardCheckIds = hardChecks
            .Select(check => check.GetProperty("id").GetString())
            .ToList();

        hardCheckIds.Should().Contain(
        [
            FullI3rabInvariants.CheckPackageShape,
            FullI3rabInvariants.CheckManifestFinal,
            FullI3rabInvariants.CheckSourceCount,
            FullI3rabInvariants.CheckSourceSet,
            FullI3rabInvariants.CheckSourceHash,
            FullI3rabInvariants.CheckCoverageCount,
            FullI3rabInvariants.CheckJsonShape,
            FullI3rabInvariants.CheckAyahKeysResolve,
            FullI3rabInvariants.CheckPointersResolve,
            FullI3rabInvariants.CheckAyahKeysMemberMatch,
            FullI3rabInvariants.CheckNoEmptyText,
            FullI3rabInvariants.CheckBlockPartition,
            FullI3rabInvariants.CheckPostCopySourceRows,
            FullI3rabInvariants.CheckPostCopyAyahMappings,
            FullI3rabInvariants.CheckPostCopyCoverageSum,
            FullI3rabInvariants.CheckPostCopyEntrySource,
            FullI3rabInvariants.CheckPostCopyAyahResolved,
            FullI3rabInvariants.CheckPostCopyNoEmptyHtml,
            FullI3rabInvariants.CheckPostCopyHtmlUnchanged,
            FullI3rabInvariants.CheckPostCopySourceUnchanged,
            FullI3rabInvariants.CheckReportWritten
        ]);
        hardChecks.Should().OnlyContain(check => check.GetProperty("passed").GetBoolean());

        root.TryGetProperty("warnings", out var warnings).Should().BeTrue();
        warnings.EnumerateArray().Should().NotBeEmpty();
        warnings.EnumerateArray().Should().Contain(warning =>
            warning.GetString()!.Contains(FullI3rabInvariants.WarningProvenance, StringComparison.Ordinal));

        root.TryGetProperty("errors", out _).Should().BeTrue();
        root.TryGetProperty("infoNotes", out _).Should().BeTrue();
    }

    public void Dispose() => packages.Dispose();
}
