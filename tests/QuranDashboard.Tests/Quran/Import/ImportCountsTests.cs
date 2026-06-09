
namespace QuranDashboard.Tests.Quran.Import;

[Collection(nameof(ImportTestCollection))]
public sealed class ImportCountsTests
{
    private readonly ImportTestFixture fixture;

    public ImportCountsTests(ImportTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task ImportIntoEmptyDatabase_PersistsExpectedCounts()
    {
        var handler = await fixture.CreateHandlerAsync();

        var result = await handler.HandleAsync(
            new ImportQuranFoundationCommand(fixture.SourceRoot, ReportOutDir: null),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);
        result.Totals.Should().NotBeNull();
        result.Totals!.Surahs.Should().Be(114);
        result.Totals.Ayahs.Should().Be(6_236);
        result.Totals.Pages.Should().Be(604);
        result.Totals.Lines.Should().Be(9_046);
        result.Totals.Words.Should().Be(83_668);
        result.Totals.AyahMarkers.Should().Be(6_236);
        result.Totals.ReadableWords.Should().Be(77_432);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await dbContext.QuranSurahs.CountAsync()).Should().Be(114);
        (await dbContext.QuranAyahs.CountAsync()).Should().Be(6_236);
        (await dbContext.QuranMushafPages.CountAsync()).Should().Be(604);
        (await dbContext.QuranMushafLines.CountAsync()).Should().Be(9_046);
        (await dbContext.QuranWords.CountAsync()).Should().Be(83_668);
        (await dbContext.QuranWords.CountAsync(word => word.IsAyahMarker)).Should().Be(6_236);
        (await dbContext.QuranWords.CountAsync(word => !word.IsAyahMarker)).Should().Be(77_432);
    }
}

[CollectionDefinition(nameof(ImportTestCollection))]
public sealed class ImportTestCollection : ICollectionFixture<ImportTestFixture>;
