using QuranDashboard.Application.Quran.DataPipelines.Foundation;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Tests.Quran.Import;

[Collection(nameof(ImportTestCollection))]
public sealed class ImlaeiCleanKeyImportTests
{
    private readonly ImportTestFixture fixture;

    public ImlaeiCleanKeyImportTests(ImportTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [FoundationImportSourceFact]
    public async Task Import_BindsExactMasaqTextToBothImlaeiFields()
    {
        var masaqSourceRoot = Path.GetDirectoryName(FoundationImportSourceGate.MasaqSourceFile)!;
        var masaqSource = await new JsonMasaqSearchWordsReader()
            .ReadAsync(masaqSourceRoot, CancellationToken.None);
        var expectedTextAt112 = masaqSource.Words.Single(word => word.Location == "1:1:2").Text;
        var expectedTextAt151 = masaqSource.Words.Single(word => word.Location == "1:5:1").Text;

        var handler = await fixture.CreateHandlerAsync();

        var result = await handler.HandleAsync(
            new ImportQuranFoundationCommand(fixture.SourceRoot, ReportOutDir: ImportSourceTestHelpers.TempReportDir()),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await dbContext.QuranWords.CountAsync(word => word.WordKeyImlaeiSimple == ""))
            .Should().Be(0);

        (await dbContext.QuranWords.CountAsync(word =>
                !word.IsAyahMarker
                && word.TextImlaeiSimple != word.WordKeyImlaeiSimple))
            .Should().Be(0);

        var wordAt112 = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Location == "1:1:2");
        wordAt112.Id.Should().Be(2);
        wordAt112.TextImlaeiSimple.Should().Be(expectedTextAt112);
        wordAt112.WordKeyImlaeiSimple.Should().Be(expectedTextAt112);

        var wordAt151 = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Location == "1:5:1");
        wordAt151.Id.Should().Be(18);
        wordAt151.TextImlaeiSimple.Should().Be(expectedTextAt151);
        wordAt151.WordKeyImlaeiSimple.Should().Be(expectedTextAt151);
    }
}
