using QuranDashboard.Application.Quran.MushafReader.Queries.GetWordAnalysis;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class WordAnalysisSegmentFallbackTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetWordAnalysis_marks_empty_segment_form_as_missing_without_fabricated_text()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordAnalysisHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordAnalysisQuery("2:25:3"),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetWordAnalysisOutcome.Success>().Subject.Response;
        var fallbackSegment = response.RenderedWordSegments.Single(s => s.SegmentNumber == 2);

        fallbackSegment.DisplayTextStatus.Should().Be("missing");
        fallbackSegment.SegmentDisplayText.Should().BeNull();
        response.Word.TextUthmani.Should().NotBeNullOrWhiteSpace();
    }
}
