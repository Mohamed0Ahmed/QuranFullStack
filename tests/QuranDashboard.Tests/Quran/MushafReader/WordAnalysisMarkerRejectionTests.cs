using FluentAssertions;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetWordAnalysis;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class WordAnalysisMarkerRejectionTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetWordAnalysis_rejects_ayah_end_marker_as_not_analyzable()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordAnalysisHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordAnalysisQuery("2:25:5"),
            CancellationToken.None);

        outcome.Should().BeOfType<GetWordAnalysisOutcome.NotAnalyzable>();
    }
}
