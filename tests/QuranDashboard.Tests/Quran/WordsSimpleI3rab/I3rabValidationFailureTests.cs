using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Application.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.SimpleI3rabGeneration;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

[Collection(nameof(I3rabGenerationTestCollection))]
public sealed class I3rabValidationFailureTests(I3rabGenerationTestFixture fixture)
{
    public static TheoryData<string, Action<IServiceCollection>> HardCheckViolationCases { get; } = new()
    {
        {
            I3rabInvariants.CheckSegStatusComplete,
            services => services.AddScoped<II3rabGenerationWriteProbe>(_ =>
                new SqlI3rabGenerationWriteProbe("""
                    ALTER TABLE quran_word_morphology_segments
                        DROP CONSTRAINT "CK_quran_word_morphology_segments_i3rab_status";
                    UPDATE quran_word_morphology_segments
                    SET i3rab_status = 'incomplete'
                    WHERE id = 1;
                    """))
        },
        {
            I3rabInvariants.CheckApprovedConsistent,
            services => services.AddSingleton<II3rabAssembler>(sp =>
                new TamperingI3rabAssembler(
                    new I3rabAssembler(sp.GetRequiredService<II3rabRuleCatalog>()),
                    labels => labels
                        .Select((label, index) => index == 0
                            ? label with { Status = I3rabStatusMapping.Approved, I3rabArabic = null }
                            : label)
                        .ToList()))
        },
        {
            I3rabInvariants.CheckNeedsReviewConsistent,
            services => services.AddSingleton<II3rabAssembler>(sp =>
                new TamperingI3rabAssembler(
                    new I3rabAssembler(sp.GetRequiredService<II3rabRuleCatalog>()),
                    labels => labels
                        .Select((label, index) => index == 0
                            ? label with
                            {
                                Status = I3rabStatusMapping.NeedsReview,
                                ReviewReason = null,
                                I3rabArabic = "اختبار-مراجعة"
                            }
                            : label)
                        .ToList()))
        },
        {
            I3rabInvariants.CheckUnsupportedConsistent,
            services => services.AddSingleton<II3rabAssembler>(sp =>
                new TamperingI3rabAssembler(
                    new I3rabAssembler(sp.GetRequiredService<II3rabRuleCatalog>()),
                    labels => labels
                        .Select((label, index) => index == 0
                            ? label with
                            {
                                Status = I3rabStatusMapping.Unsupported,
                                ReviewReason = null,
                                I3rabArabic = null
                            }
                            : label)
                        .ToList()))
        },
        {
            I3rabInvariants.CheckWordDisplayable,
            services => services.AddSingleton<II3rabAssembler>(sp =>
                new TamperingI3rabAssembler(
                    new I3rabAssembler(sp.GetRequiredService<II3rabRuleCatalog>()),
                    labels => labels
                        .Select(label => label.SegmentId == 4 || label.SegmentId == 5
                            ? label with
                            {
                                Status = I3rabStatusMapping.Unsupported,
                                ReviewReason = "test undisplayable word",
                                I3rabArabic = null,
                                SignatureKey = "TEST:UNDISPLAYABLE"
                            }
                            : label)
                        .ToList()))
        },
        {
            I3rabInvariants.CheckRuleResolves,
            services => services.AddScoped<II3rabGenerationWriteProbe>(_ =>
                new SqlI3rabGenerationWriteProbe("""
                    ALTER TABLE quran_word_morphology_segments
                        DROP CONSTRAINT "FK_quran_word_morphology_segments_quran_i3rab_rules_i3rab_rule~";
                    UPDATE quran_word_morphology_segments
                    SET i3rab_rule_id = 999999
                    WHERE id = 1;
                    """))
        },
        {
            I3rabInvariants.CheckSourceColumnsUnchanged,
            services => services.AddScoped<II3rabGenerationWriteProbe>(_ =>
                new SqlI3rabGenerationWriteProbe(
                    "UPDATE quran_word_morphology_segments SET features_raw = 'TAMPERED' WHERE id = 1"))
        },
        {
            I3rabInvariants.CheckSegmentRowCountStable,
            services => services.AddScoped<II3rabGenerationWriteProbe>(_ =>
                new SqlI3rabGenerationWriteProbe(
                    "DELETE FROM quran_word_morphology_segments WHERE id = 1"))
        },
        {
            I3rabInvariants.CheckNullFormNotInvented,
            services => services.AddScoped<II3rabGenerationWriteProbe>(_ =>
                new SqlI3rabGenerationWriteProbe(
                    "UPDATE quran_word_morphology_segments SET form_arabic_normalized = 'اختبار-مخترع' WHERE form_arabic_normalized IS NULL"))
        }
    };

    [Theory]
    [MemberData(nameof(HardCheckViolationCases))]
    public async Task Hard_check_failure_rolls_back_writes_a_fail_report_and_returns_non_zero(
        string expectedFailedCheckId,
        Action<IServiceCollection> configureTamper)
    {
        await fixture.ResetToCompleteMorphologyAsync();
        var counts = I3rabGenerationTestFixture.CompleteMorphologyCounts;
        var before = await fixture.CaptureCommittedStateAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"i3rab-fail-{Guid.NewGuid():N}");

        try
        {
            var result = await fixture.RunGenerationAsync(
                counts,
                reportOutDir: reportDir,
                configure: services =>
                {
                    configureTamper(services);
                });

            result.Succeeded.Should().BeFalse();
            result.ExitCode.Should().Be(GenerateI3rabResult.FailureExitCode);
            result.ReportPath.Should().NotBeNull();

            await AssertFailureReportAsync(reportDir, expectedFailedCheckId);

            var after = await fixture.CaptureCommittedStateAsync();
            after.Should().BeEquivalentTo(before);
        }
        finally
        {
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, true);
            }
        }
    }

    private static async Task AssertFailureReportAsync(string reportDir, string expectedFailedCheckId)
    {
        File.Exists(Path.Combine(reportDir, "simple-i3rab-generation-report.md")).Should().BeTrue();
        var jsonPath = Path.Combine(reportDir, "simple-i3rab-generation-report.json");
        File.Exists(jsonPath).Should().BeTrue();

        await using var jsonStream = File.OpenRead(jsonPath);
        var report = await JsonSerializer.DeserializeAsync<JsonElement>(jsonStream);

        report.GetProperty("verdict").GetString().Should().Be("FAIL");
        report.GetProperty("persisted").GetBoolean().Should().BeFalse();

        var failedCheck = report.GetProperty("checks").EnumerateArray()
            .Single(check => check.GetProperty("id").GetString() == expectedFailedCheckId);
        failedCheck.GetProperty("passed").GetBoolean().Should().BeFalse();
        failedCheck.GetProperty("severity").GetString().Should().Be("hard");
    }
}
