using QuranDashboard.Application.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Tests.Quran.Import;

[Collection(nameof(ImportTestCollection))]
public sealed class ImlaeiCleanKeyImportTests
{
    private const string Sajdah = "۩";
    private const string RubElHizb = "۞";
    private const string RightToLeftMark = "‏";

    private readonly ImportTestFixture fixture;

    public ImlaeiCleanKeyImportTests(ImportTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Import_BindsCleanImlaeiKeyAndPreservesRawImlaeiText()
    {
        var handler = await fixture.CreateHandlerAsync();

        var result = await handler.HandleAsync(
            new ImportQuranFoundationCommand(fixture.SourceRoot, ReportOutDir: null),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await dbContext.QuranWords.CountAsync(word => word.WordKeyImlaeiSimple == ""))
            .Should().Be(0);

        (await dbContext.QuranWords.CountAsync(word =>
                !word.IsAyahMarker &&
                (word.WordKeyImlaeiSimple.Contains(Sajdah) ||
                 word.WordKeyImlaeiSimple.Contains(RubElHizb) ||
                 word.WordKeyImlaeiSimple.Contains(RightToLeftMark))))
            .Should().Be(0);

        var allah = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Location == "1:1:2");
        allah.Id.Should().Be(2);
        allah.TextImlaeiSimple.Should().Be("الله");
        allah.WordKeyImlaeiSimple.Should().Be("الله");

        var azim = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Location == "27:26:8");
        azim.Id.Should().Be(51944);
        azim.TextImlaeiSimple.Should().Contain(Sajdah);
        azim.WordKeyImlaeiSimple.Should().Be("العظيم");
        azim.WordKeyImlaeiSimple.Should().NotContain(Sajdah);
        azim.WordKeyImlaeiSimple.Should().NotContain(RightToLeftMark);
    }
}
