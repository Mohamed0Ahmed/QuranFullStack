
using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(WordsDisplayTestCollection))]
public sealed class DisplayWordsStatisticsTests
{
    private readonly WordsDisplayTestFixture fixture;

    public DisplayWordsStatisticsTests(WordsDisplayTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Rebuild_ComputesGroupedStatisticsAndTashkeelSimpleDivergence()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var handler = fixture.CreateHandler();
        var reportDir = Path.Combine(Path.GetTempPath(), $"words-display-{Guid.NewGuid():N}");

        var result = await handler.HandleAsync(
            new RebuildDisplayWordsCommand(
                Force: false,
                ReportOutDir: reportDir,
                ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var alphaTashkeelRows = await dbContext.QuranWordsOrderedTashkeel
            .Where(row => row.TextUthmani == DisplayWordsSyntheticSeed.TokenAlphaTashkeel)
            .ToListAsync();

        alphaTashkeelRows.Should().OnlyContain(row =>
            row.OccurrencesCount == DisplayWordsSyntheticSeed.AlphaTashkeelStats.OccurrencesCount
            && row.AyahsCount == DisplayWordsSyntheticSeed.AlphaTashkeelStats.AyahsCount
            && row.SurahsCount == DisplayWordsSyntheticSeed.AlphaTashkeelStats.SurahsCount);

        var alphaSimpleRows = await dbContext.QuranWordsOrderedSimple
            .Where(row => row.WordKeyImlaeiSimple == DisplayWordsSyntheticSeed.KeyAlphaImlaei)
            .ToListAsync();

        alphaSimpleRows.Should().OnlyContain(row =>
            row.OccurrencesCount == DisplayWordsSyntheticSeed.AlphaSimpleStats.OccurrencesCount
            && row.AyahsCount == DisplayWordsSyntheticSeed.AlphaSimpleStats.AyahsCount
            && row.SurahsCount == DisplayWordsSyntheticSeed.AlphaSimpleStats.SurahsCount);

        (await dbContext.QuranWordsUniqueTashkeel.CountAsync())
            .Should()
            .BeGreaterThan(await dbContext.QuranWordsUniqueSimple.CountAsync());
    }
}
