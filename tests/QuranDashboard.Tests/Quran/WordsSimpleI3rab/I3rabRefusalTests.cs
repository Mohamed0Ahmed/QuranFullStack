using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;
using QuranDashboard.Application.Quran.Words.GenerateI3rab;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

[Collection(nameof(I3rabGenerationTestCollection))]
public sealed class I3rabRefusalTests(I3rabGenerationTestFixture fixture)
{
    [Fact]
    public async Task Empty_morphology_refuses_writes_nothing_and_emits_a_refusal_report()
    {
        await fixture.ResetToWordsOnlyAsync();
        var reportDir = CreateReportDir();

        try
        {
            var result = await fixture.RunGenerationAsync(reportOutDir: reportDir);

            result.Succeeded.Should().BeFalse();
            result.ExitCode.Should().Be(GenerateI3rabResult.RefusedExitCode);
            result.Message.Should().Contain("Morphology is missing or empty");
            result.ReportPath.Should().NotBeNull();
            await AssertRefusalReportAsync(reportDir);

            (await fixture.CountPopulatedI3rabSegmentsAsync()).Should().Be(0);
        }
        finally
        {
            DeleteReportDir(reportDir);
        }
    }

    [Fact]
    public async Task Stale_morphology_refuses_writes_nothing_and_emits_a_refusal_report()
    {
        await fixture.ResetToPartialMorphologyAsync();
        var reportDir = CreateReportDir();

        try
        {
            var result = await fixture.RunGenerationAsync(reportOutDir: reportDir);

            result.Succeeded.Should().BeFalse();
            result.ExitCode.Should().Be(GenerateI3rabResult.RefusedExitCode);
            result.Message.Should().Contain(I3rabInvariants.ExpectedSegmentCount.ToString("N0"));
            result.ReportPath.Should().NotBeNull();
            await AssertRefusalReportAsync(reportDir);

            (await fixture.CountPopulatedI3rabSegmentsAsync()).Should().Be(0);
        }
        finally
        {
            DeleteReportDir(reportDir);
        }
    }

    [Fact]
    public async Task Second_run_without_force_refuses_and_leaves_committed_state_unchanged()
    {
        await fixture.ResetToCompleteMorphologyAsync();
        var counts = I3rabGenerationTestFixture.CompleteMorphologyCounts;
        var reportDir = CreateReportDir();

        try
        {
            var first = await fixture.RunGenerationAsync(counts, reportOutDir: reportDir);
            first.Succeeded.Should().BeTrue(first.Message);

            var snapshotBefore = await fixture.CaptureCommittedStateAsync();

            var second = await fixture.RunGenerationAsync(counts, reportOutDir: reportDir);
            second.Succeeded.Should().BeFalse();
            second.ExitCode.Should().Be(GenerateI3rabResult.RefusedExitCode);
            second.Message.Should().Be(I3rabCommandExecutor.AlreadyPopulatedRefusalReason);
            second.ReportPath.Should().NotBeNull();
            await AssertRefusalReportAsync(reportDir);

            var snapshotAfter = await fixture.CaptureCommittedStateAsync();
            snapshotAfter.Should().BeEquivalentTo(snapshotBefore);
        }
        finally
        {
            DeleteReportDir(reportDir);
        }
    }

    [Fact]
    public async Task Second_run_with_force_succeeds_and_overwrites_to_a_fresh_pass_state()
    {
        await fixture.ResetToCompleteMorphologyAsync();
        var counts = I3rabGenerationTestFixture.CompleteMorphologyCounts;
        var reportDir = CreateReportDir();

        try
        {
            var first = await fixture.RunGenerationAsync(counts, reportOutDir: reportDir);
            first.Succeeded.Should().BeTrue(first.Message);

            var forced = await fixture.RunGenerationAsync(counts, force: true, reportOutDir: reportDir);
            forced.Succeeded.Should().BeTrue(forced.Message);
            forced.ExitCode.Should().Be(GenerateI3rabResult.SuccessExitCode);

            var snapshot = await fixture.CaptureCommittedStateAsync();
            snapshot.Segments.Should().HaveCount(6);
            snapshot.Segments.Should().OnlyContain(segment => segment.I3rabStatus == I3rabStatusMapping.Approved);
            snapshot.Rules.Should().NotBeEmpty();
        }
        finally
        {
            DeleteReportDir(reportDir);
        }
    }

    private static string CreateReportDir() =>
        Path.Combine(Path.GetTempPath(), $"i3rab-refusal-{Guid.NewGuid():N}");

    private static void DeleteReportDir(string reportDir)
    {
        if (Directory.Exists(reportDir))
        {
            Directory.Delete(reportDir, true);
        }
    }

    private static async Task AssertRefusalReportAsync(string reportDir)
    {
        File.Exists(Path.Combine(reportDir, "simple-i3rab-generation-report.md")).Should().BeTrue();
        var jsonPath = Path.Combine(reportDir, "simple-i3rab-generation-report.json");
        File.Exists(jsonPath).Should().BeTrue();

        await using var jsonStream = File.OpenRead(jsonPath);
        var report = await JsonSerializer.DeserializeAsync<JsonElement>(jsonStream);

        report.GetProperty("verdict").GetString().Should().Be("REFUSED");
        report.GetProperty("persisted").GetBoolean().Should().BeFalse();
        report.GetProperty("refusalReason").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
