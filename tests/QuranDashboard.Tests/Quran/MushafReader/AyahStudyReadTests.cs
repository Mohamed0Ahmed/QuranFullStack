using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahStudy;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class AyahStudyReadTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetAyahStudy_applies_configured_defaults_when_no_sources_specified()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahStudyHandler>();

        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery("2:25", null, null, null),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahStudyOutcome.Success>().Subject.Response;
        response.SelectedSources.TafsirSource.Should().Be("ar-muyassar");
        response.SelectedSources.TranslationSource.Should().Be("en-sahih-international");
        response.SelectedSources.FullI3rabSource.Should().Be("muyassar");
        response.Tafsir.Should().NotBeNull();
        response.Translation.Should().NotBeNull();
        response.FullI3rab.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAyahStudy_loads_all_three_blocks_together_with_explicit_sources()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetAyahStudyHandler>();

        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery(
                "2:25",
                "ar-muyassar",
                "en-sahih-international",
                "muyassar"),
            CancellationToken.None);

        var response = outcome.Should().BeOfType<GetAyahStudyOutcome.Success>().Subject.Response;
        response.Ayah.VerseKey.Should().Be("2:25");
        response.Ayah.SurahNumber.Should().Be(2);
        response.Ayah.AyahNumber.Should().Be(25);
        response.Tafsir!.SourceKey.Should().Be("ar-muyassar");
        response.Translation!.SourceKey.Should().Be("en-sahih-international");
        response.FullI3rab!.SourceKey.Should().Be("muyassar");
        response.Tafsir.DisplayNameAr.Should().NotBeNullOrWhiteSpace();
        response.Translation.DisplayNameEn.Should().NotBeNullOrWhiteSpace();
        response.FullI3rab.DisplayNameAr.Should().NotBeNullOrWhiteSpace();
    }
}
