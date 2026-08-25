using QuranDashboard.Application.Quran.DataPipelines.Foundation;

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

        var allah = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Location == "1:1:2");
        allah.Id.Should().Be(2);
        allah.TextImlaeiSimple.Should().Be("الله");
        allah.WordKeyImlaeiSimple.Should().Be("الله");

        var iyyaka = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Location == "1:5:1");
        iyyaka.Id.Should().Be(18);
        iyyaka.TextImlaeiSimple.Should().Be("إياك");
        iyyaka.WordKeyImlaeiSimple.Should().Be("إياك");
    }
}
