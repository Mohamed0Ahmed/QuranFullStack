using FluentAssertions;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafPage;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class MushafPageValidationTests(MushafReaderTestFixture fixture)
{
    [Theory]
    [InlineData(0)]
    [InlineData(605)]
    [InlineData(-1)]
    public async Task GetPage_out_of_range_returns_invalid_page_number(int pageNumber)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetMushafPageHandler>();

        var outcome = await handler.HandleAsync(new GetMushafPageQuery(pageNumber), CancellationToken.None);

        outcome.Should().BeOfType<GetMushafPageOutcome.InvalidPageNumber>();
    }

    [Fact]
    public async Task GetPage_missing_page_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetMushafPageHandler>();

        var outcome = await handler.HandleAsync(new GetMushafPageQuery(2), CancellationToken.None);

        outcome.Should().BeOfType<GetMushafPageOutcome.NotFound>();
    }
}
