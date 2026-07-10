using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafStudySourceCatalog;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class MushafStudySourceCatalogTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetCatalog_returns_seeded_study_sources()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetMushafStudySourceCatalogHandler>();

        var catalog = await handler.HandleAsync(new GetMushafStudySourceCatalogQuery(), CancellationToken.None);

        catalog.TafsirSources.Should().ContainSingle();
        catalog.TafsirSources[0].SourceKey.Should().Be("ar-muyassar");
        catalog.TafsirSources[0].DisplayNameAr.Should().Be("التفسير الميسر");
        catalog.TafsirSources[0].LanguageNameAr.Should().Be("العربية");
        catalog.TafsirSources[0].TafsirKind.Should().NotBeNull();
        catalog.TafsirSources[0].TranslationType.Should().BeNull();

        catalog.TranslationSources.Should().ContainSingle();
        catalog.TranslationSources[0].SourceKey.Should().Be("en-sahih-international");
        catalog.TranslationSources[0].DisplayNameAr.Should().Be("صحيح الدولية");
        catalog.TranslationSources[0].LanguageNameAr.Should().Be("الإنجليزية");
        catalog.TranslationSources[0].TranslationType.Should().Be("simple");
        catalog.TranslationSources[0].TafsirKind.Should().BeNull();

        catalog.FullI3rabSources.Should().ContainSingle();
        catalog.FullI3rabSources[0].SourceKey.Should().Be("muyassar");
        catalog.FullI3rabSources[0].DisplayNameAr.Should().Be("الإعراب الميسر");
        catalog.FullI3rabSources[0].TafsirKind.Should().BeNull();
        catalog.FullI3rabSources[0].TranslationType.Should().BeNull();
    }
}
