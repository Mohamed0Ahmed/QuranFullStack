using FluentAssertions;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetWordAnalysis;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class WordAnalysisMorphologyIdentityTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetWordAnalysis_maps_present_lemma_and_stem_ids_without_changing_other_fields()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordAnalysisHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordAnalysisQuery("2:26:1"),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetWordAnalysisOutcome.Success>().Subject.Response;

        response.Word.WordLocation.Should().Be("2:26:1");
        response.Word.TextUthmani.Should().NotBeNullOrWhiteSpace();

        response.Morphology.Root.Should().NotBeNull();
        response.Morphology.Root!.Id.Should().Be(9001);
        response.Morphology.Lemma.Should().NotBeNull();
        response.Morphology.Lemma!.Id.Should().Be(9101);
        response.Morphology.Lemma.Text.Should().Be("لِمَة-تجريبية");
        response.Morphology.Stem.Should().NotBeNull();
        response.Morphology.Stem!.Id.Should().Be(9201);
        response.Morphology.Stem.Text.Should().Be("سِتَم-تجريبي");

        response.Identity.UniqueTashkeel.Id.Should().Be(2006);
        response.Identity.UniqueSimple.Id.Should().Be(2006);
        response.RenderedWordSegments.Should().HaveCount(1);
        response.RenderedWordSegments[0].SegmentDisplayText.Should().NotBeNullOrWhiteSpace();
    }
}
