using QuranDashboard.Application.Quran.MushafReader.Queries.GetWordAnalysis;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class WordAnalysisIncompleteDataTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetWordAnalysis_returns_incomplete_data_when_required_rows_are_missing()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordAnalysisHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordAnalysisQuery("2:25:1"),
            CancellationToken.None);

        outcome.Should().BeOfType<GetWordAnalysisOutcome.IncompleteData>();
    }
}
