using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafPage;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class MushafPageReadTests(MushafReaderTestFixture fixture)
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(604)]
    public async Task GetPage_returns_ordered_lines_and_words_for_fixture_pages(int pageNumber)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetMushafPageHandler>();

        var outcome = await handler.HandleAsync(new GetMushafPageQuery(pageNumber), CancellationToken.None);

        var response = outcome.Should().BeOfType<GetMushafPageOutcome.Success>().Subject.Response;
        response.PageNumber.Should().Be(pageNumber);
        response.Lines.Should().NotBeEmpty();
        response.Lines.Select(l => l.LineNumber).Should().BeInAscendingOrder();

        foreach (var line in response.Lines)
        {
            line.Words.Select(w => w.LineWordOrder).Should().BeInAscendingOrder();
            line.Words.Should().OnlyContain(w => !string.IsNullOrWhiteSpace(w.TextUthmani));
        }

        response.PreviousPageNumber.Should().Be(pageNumber > 1 ? pageNumber - 1 : null);
        response.NextPageNumber.Should().Be(pageNumber < 604 ? pageNumber + 1 : null);
    }

    [Fact]
    public async Task GetPage_payload_is_lean_without_study_fields()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetMushafPageHandler>();

        var outcome = await handler.HandleAsync(new GetMushafPageQuery(5), CancellationToken.None);
        var json = System.Text.Json.JsonSerializer.Serialize(
            outcome.Should().BeOfType<GetMushafPageOutcome.Success>().Subject.Response);

        json.Should().NotContain("tafsir");
        json.Should().NotContain("translation");
        json.Should().NotContain("morphology");
        json.Should().NotContain("segment");
        json.Should().NotContain("similaritySummary");
        json.Should().NotContain("similarAyahCount");
        json.Should().NotContain("mutashabihatGroupCount");
        json.Should().NotContain("mutashabihatOccurrenceCount");
    }

    [Fact]
    public async Task GetPage_line_types_match_fixture()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IMushafPageReader>();

        var page = await reader.GetPageAsync(5, CancellationToken.None);
        page.Should().NotBeNull();
        page!.Lines.Should().OnlyContain(l =>
            l.LineType == "ayah" || l.LineType == "surah_name" || l.LineType == "basmallah");
        page.Lines.Should().HaveCount(2);
        page.Lines[0].Words.Should().HaveCount(5);
        page.Lines[0].Words[^1].IsAyahMarker.Should().BeTrue();
    }
}
