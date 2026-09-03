using QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

namespace QuranDashboard.Tests.Quran.MushafReader;

// quran-safety rule 3 (never hide invalid data): corrupt features_json (valid JSON, wrong shape)
// still returns an empty list (unchanged contract), but must now log a Warning naming the segment
// instead of swallowing it silently as "segment has no features".
public sealed class WordAnalysisCorruptFeaturesJsonTests
{
    private const string SegmentLocation = "test-segment:1";

    [Fact]
    public void GetWordAnalysis_corrupt_features_json_returns_empty_list_and_logs_warning_naming_segment()
    {
        var recordingProvider = new RecordingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(recordingProvider));
        var logger = loggerFactory.CreateLogger<EfWordAnalysisReader>();
        var segment = MushafReaderValueMapper.MapSegments(
            [new WordAnalysisSegmentValue(
                SegmentLocation,
                1,
                "STEM",
                "اختبار",
                "V",
                "فعل",
                "Verb",
                null,
                null,
                null,
                null,
                "approved",
                "POS=V",
                "{\"not\":\"an array\"}")],
            logger).Single();
        segment.SegmentFeatures.Should().NotBeNull(
            "features_raw is present even when features_json is corrupt, so the block itself is not dropped");
        segment.SegmentFeatures!.Json.Should().BeEmpty(
            "corrupt features_json must still surface as an empty list, not throw or crash the response");

        var warning = recordingProvider.Entries.Should()
            .ContainSingle(entry => entry.Level == LogLevel.Warning)
            .Subject;
        warning.Exception.Should().BeOfType<JsonException>();
        warning.Message.Should().Contain(SegmentLocation);
    }
}
