using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafSurahCatalog;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class MushafSurahCatalogTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetCatalog_returns_start_page_for_each_seeded_surah()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetMushafSurahCatalogHandler>();

        var catalog = await handler.HandleAsync(new GetMushafSurahCatalogQuery(), CancellationToken.None);

        catalog.Surahs.Should().HaveCount(3);
        catalog.Surahs.Single(s => s.SurahNumber == 1).StartPageNumber.Should().Be(1);
        catalog.Surahs.Single(s => s.SurahNumber == 2).StartPageNumber.Should().Be(5);
        catalog.Surahs.Single(s => s.SurahNumber == 114).StartPageNumber.Should().Be(604);
    }
}
