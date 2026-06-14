using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirJsonReportShapeTests(TafsirImportTestFixture fixture)
{
    [Fact]
    public async Task Successful_import_json_report_contains_required_top_level_fields()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.TwoSourceIntegrationSources,
            excludedSourceKeys: ["ar-wajiz"]);
        var reportDir = Path.Combine(Path.GetTempPath(), $"tafsir-json-shape-{Guid.NewGuid():N}");

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.TwoSourceTestExpectedCounts with { ExcludedSources = 1 },
            reportDir);

        result.Succeeded.Should().BeTrue(result.Message);

        var reportJson = await File.ReadAllTextAsync(
            Path.Combine(reportDir, TafsirImportConstants.JsonReportFileName));
        using var document = JsonDocument.Parse(reportJson);
        var root = document.RootElement;

        root.TryGetProperty("runAtUtc", out _).Should().BeTrue();
        root.GetProperty("verdict").GetString().Should().Be(TafsirImportConstants.PassVerdict);
        root.GetProperty("persisted").GetBoolean().Should().BeTrue();
        root.GetProperty("forced").GetBoolean().Should().BeFalse();
        root.GetProperty("sourcePath").GetString().Should().Be(Path.GetFullPath(packageDir));

        var totals = root.GetProperty("totals");
        totals.GetProperty("sourceRows").GetInt32().Should().Be(2);
        totals.GetProperty("tafsirTextBlockRows").GetInt64().Should().Be(4);
        totals.GetProperty("ayahMappingRows").GetInt64().Should().Be(6);
        totals.GetProperty("approvedSources").GetInt32().Should().Be(2);
        totals.GetProperty("excludedSources").GetInt32().Should().Be(1);
        totals.GetProperty("arabicSources").GetInt32().Should().Be(1);
        totals.GetProperty("nonArabicSources").GetInt32().Should().Be(1);
        totals.GetProperty("languageCount").GetInt32().Should().Be(2);
        totals.GetProperty("distinctAyahs").GetInt32().Should().Be(3);

        var sourceSummaries = root.GetProperty("sourceSummaries").EnumerateArray().ToList();
        sourceSummaries.Should().HaveCount(2);
        sourceSummaries[0].GetProperty("sourceKey").GetString().Should().NotBeNullOrWhiteSpace();
        sourceSummaries[0].GetProperty("languageCode").GetString().Should().NotBeNullOrWhiteSpace();
        sourceSummaries[0].GetProperty("direction").GetString().Should().NotBeNullOrWhiteSpace();
        sourceSummaries[0].GetProperty("displayNameEn").GetString().Should().NotBeNullOrWhiteSpace();
        sourceSummaries[0].GetProperty("packageFile").GetString().Should().StartWith("sources/");
        sourceSummaries[0].GetProperty("sha256").GetString().Should().NotBeNullOrWhiteSpace();
        sourceSummaries[0].GetProperty("license").GetString().Should().Be("unknown");
        sourceSummaries[0].GetProperty("provenance").GetString().Should().Be("unknown");

        var excludedSummaries = root.GetProperty("excludedSourceSummaries").EnumerateArray().ToList();
        excludedSummaries.Should().ContainSingle();
        excludedSummaries[0].GetProperty("sourceKey").GetString().Should().Be("ar-wajiz");
        excludedSummaries[0].GetProperty("status").GetString().Should().NotBeNullOrWhiteSpace();
        excludedSummaries[0].GetProperty("reason").GetString().Should().NotBeNullOrWhiteSpace();

        root.GetProperty("checks").EnumerateArray().Should().NotBeEmpty();

        // US4 AS1 / FR-027: a passing report must enumerate every hard check, all passed.
        var hardChecks = root.GetProperty("checks").EnumerateArray()
            .Where(check => check.GetProperty("severity").GetString() == TafsirImportConstants.HardSeverity)
            .ToList();
        var hardCheckIds = hardChecks
            .Select(check => check.GetProperty("id").GetString())
            .ToList();
        hardCheckIds.Should().Contain(
        [
            TafsirInvariants.CheckPackageShape,
            TafsirInvariants.CheckManifestFinal,
            TafsirInvariants.CheckSourceCount,
            TafsirInvariants.CheckExcludedCount,
            TafsirInvariants.CheckArabicSourceCount,
            TafsirInvariants.CheckNonArabicSourceCount,
            TafsirInvariants.CheckSourceSet,
            TafsirInvariants.CheckSourceHash,
            TafsirInvariants.CheckNoExcludedSources,
            TafsirInvariants.CheckCoverageCount,
            TafsirInvariants.CheckJsonShape,
            TafsirInvariants.CheckAyahKeysResolve,
            TafsirInvariants.CheckPointersResolve,
            TafsirInvariants.CheckNoEmptyText,
            TafsirInvariants.CheckNoDuplicateAyahEntry,
            TafsirInvariants.CheckTextUnchanged,
            TafsirInvariants.CheckNoQuranTextCopy,
            TafsirInvariants.CheckPostCopySourceRows,
            TafsirInvariants.CheckPostCopyAyahMappings,
            TafsirInvariants.CheckSourceUnchanged,
            TafsirInvariants.CheckReportWritten
        ]);
        hardChecks.Should().OnlyContain(check => check.GetProperty("passed").GetBoolean());

        root.TryGetProperty("warnings", out _).Should().BeTrue();
        root.TryGetProperty("errors", out _).Should().BeTrue();
        root.TryGetProperty("infoNotes", out _).Should().BeTrue();
    }
}
