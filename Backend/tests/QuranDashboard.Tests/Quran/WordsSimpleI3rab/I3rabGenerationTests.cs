using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Application.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

[Collection(nameof(I3rabGenerationTestCollection))]
public sealed class I3rabGenerationTests(I3rabGenerationTestFixture fixture)
{
    [Fact]
    public async Task Refuses_when_morphology_is_missing()
    {
        await fixture.ResetToWordsOnlyAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"i3rab-refusal-{Guid.NewGuid():N}");

        try
        {
            var result = await fixture.RunGenerationAsync(reportOutDir: reportDir);

            result.ExitCode.Should().Be(GenerateI3rabResult.RefusedExitCode);
            result.Message.Should().Contain("Morphology is missing or empty");
            result.ReportPath.Should().NotBeNull();
            File.Exists(Path.Combine(reportDir, "simple-i3rab-generation-report.json")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, true);
            }
        }
    }

    [Fact]
    public async Task Refuses_when_partial_morphology_does_not_meet_production_invariants()
    {
        await fixture.ResetToPartialMorphologyAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"i3rab-refusal-{Guid.NewGuid():N}");

        try
        {
            var result = await fixture.RunGenerationAsync(reportOutDir: reportDir);

            result.ExitCode.Should().Be(GenerateI3rabResult.RefusedExitCode);
            result.Message.Should().Contain(I3rabInvariants.ExpectedSegmentCount.ToString("N0"));
            result.ReportPath.Should().NotBeNull();
            File.Exists(Path.Combine(reportDir, "simple-i3rab-generation-report.json")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, true);
            }
        }
    }

    [Fact]
    public async Task Generates_approved_labels_and_writes_a_pass_report_for_a_representative_fixture()
    {
        await fixture.ResetToCompleteMorphologyAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"i3rab-gen-{Guid.NewGuid():N}");

        var result = await fixture.RunGenerationAsync(
            I3rabGenerationTestFixture.CompleteMorphologyCounts,
            reportOutDir: reportDir);

        result.Succeeded.Should().BeTrue(result.Message);
        result.ExitCode.Should().Be(GenerateI3rabResult.SuccessExitCode);
        result.ReportPath.Should().NotBeNull();

        var reportJson = await File.ReadAllTextAsync(Path.Combine(reportDir, "simple-i3rab-generation-report.json"));
        reportJson.Should().Contain("\"verdict\": \"PASS\"");

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var segments = await dbContext.WordMorphologySegments.AsNoTracking()
            .OrderBy(segment => segment.Id)
            .ToListAsync();
        segments.Should().HaveCount(6);
        segments.Should().AllSatisfy(segment =>
        {
            segment.I3rabStatus.Should().Be(I3rabStatusMapping.Approved);
            segment.I3rabArabic.Should().NotBeNullOrWhiteSpace();
            segment.I3rabRuleId.Should().NotBeNull();
        });

        var ruleIds = await dbContext.QuranI3rabRules.AsNoTracking()
            .Select(rule => rule.Id)
            .ToListAsync();
        segments.Should().OnlyContain(segment => ruleIds.Contains(segment.I3rabRuleId!.Value));

        var allahSegment = segments.Single(segment => segment.QuranWordId == 2);
        allahSegment.I3rabArabic.Should().Be("لفظ الجلالة مجرور");
    }

    [Fact]
    public async Task Generates_approved_labels_for_simplified_particle_corrections()
    {
        await fixture.ResetToParticleLabelMorphologyAsync();

        var result = await fixture.RunGenerationAsync(I3rabGenerationTestFixture.ParticleLabelCounts);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var labelsByPos = await dbContext.WordMorphologySegments.AsNoTracking()
            .Select(segment => new { segment.Pos, segment.I3rabArabic })
            .ToDictionaryAsync(segment => segment.Pos, segment => segment.I3rabArabic);

        labelsByPos["PRO"].Should().Be("لا الناهية");
        labelsByPos["PRO"].Should().NotBe("ضمير منفصل");
        labelsByPos["ACC"].Should().Be("حرف نصب");
        labelsByPos["ACC"].Should().NotContain("أخوات إنّ/النواصب");
    }
}
