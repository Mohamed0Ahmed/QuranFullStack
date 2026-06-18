using FluentAssertions;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetWordAnalysis;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class WordAnalysisReadTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetWordAnalysis_returns_morphology_identity_and_ordered_segments_with_color_slots()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordAnalysisHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordAnalysisQuery("2:25:3"),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetWordAnalysisOutcome.Success>().Subject.Response;

        response.Word.WordLocation.Should().Be("2:25:3");
        response.Word.VerseKey.Should().Be("2:25");
        response.Word.TextUthmani.Should().NotBeNullOrWhiteSpace();

        response.Morphology.HeadPos.Should().Be("V");
        response.Morphology.HeadPosLabel.Ar.Should().Be("فعل");
        response.Morphology.IsVerb.Should().BeTrue();

        response.Identity.OrderedTashkeel.OccurrencesCount.Should().BeGreaterThan(0);
        response.Identity.UniqueTashkeel.Id.Should().BeGreaterThan(0);
        response.Identity.UniqueSimple.WordKeyImlaeiSimple.Should().NotBeNullOrWhiteSpace();

        response.RenderedWordSegments.Should().HaveCount(2);
        response.RenderedWordSegments.Select(s => s.SegmentNumber).Should().BeInAscendingOrder();
        response.RenderedWordSegments[0].SegmentColorSlot.Should().Be(1);
        response.RenderedWordSegments[1].SegmentColorSlot.Should().Be(2);
        response.RenderedWordSegments[0].DisplayTextStatus.Should().Be("available");
        response.RenderedWordSegments[0].SegmentDisplayText.Should().NotBeNullOrWhiteSpace();
    }
}
