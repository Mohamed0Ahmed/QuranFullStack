using QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

namespace QuranDashboard.Tests.Quran.MushafReader;

public sealed class WordAnalysisSegmentFallbackTests
{
    [Fact]
    public void GetWordAnalysis_marks_empty_segment_form_as_missing_without_fabricated_text()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var fallbackSegment = MushafReaderValueMapper.MapSegments(
            [new WordAnalysisSegmentValue(
                "test-segment:2",
                2,
                "SUFFIX",
                null,
                "PRON",
                "ضمير",
                "Pronoun",
                null,
                null,
                null,
                null,
                "unsupported",
                "POS=PRON",
                "[]")],
            loggerFactory.CreateLogger<EfWordAnalysisReader>()).Single();

        fallbackSegment.DisplayTextStatus.Should().Be("missing");
        fallbackSegment.SegmentDisplayText.Should().BeNull();
    }
}
