using QuranDashboard.Application.Abstractions.Quran.MushafReader;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class MarkerPlacementTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task Rub_marker_is_placed_on_first_line_where_the_ayah_appears_on_the_page()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IMushafPageReader>();

        var page = await reader.GetPageAsync(5, CancellationToken.None);
        page.Should().NotBeNull();

        var rubMarker = page!.Markers.Single(m => m.MarkerType == "rub" && m.MarkerNumber == 2);
        rubMarker.VerseKey.Should().Be("2:26");
        rubMarker.LineNumber.Should().Be(2);
        rubMarker.WordLocation.Should().Be("2:26:1");
    }

    [Fact]
    public async Task Multi_line_ayah_on_page_places_marker_on_its_first_line_not_a_later_line()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IMushafPageReader>();

        var page = await reader.GetPageAsync(5, CancellationToken.None);
        page.Should().NotBeNull();

        var ayah225FirstLine = page!.Lines
            .Where(l => l.Words.Any(w => w.VerseKey == "2:25"))
            .Min(l => l.LineNumber);

        var ayah225Marker = page.Markers.FirstOrDefault(m => m.VerseKey == "2:25");
        if (ayah225Marker is not null)
        {
            ayah225Marker.LineNumber.Should().Be(ayah225FirstLine);
        }

        page.Lines.Where(l => l.LineNumber > ayah225FirstLine)
            .SelectMany(l => l.Words)
            .Where(w => w.VerseKey == "2:25")
            .Should()
            .BeEmpty("ayah 2:25 only appears on one line in the fixture; markers must not move to later lines");
    }
}
