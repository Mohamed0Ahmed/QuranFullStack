
using QuranDashboard.Application.Quran.Words.RebuildDisplayWords;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(WordsDisplayTestCollection))]
public sealed class DisplayWordsOrderingTests
{
    private readonly WordsDisplayTestFixture fixture;

    public DisplayWordsOrderingTests(WordsDisplayTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Rebuild_AssignsContiguousMushafSurahAndAyahOrders()
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

        var orderedRows = await dbContext.QuranWordsOrderedTashkeel
            .OrderBy(row => row.WordOrderInMushaf)
            .ToListAsync();

        orderedRows.Should().HaveCount(DisplayWordsSyntheticSeed.ReadableWordCount);
        orderedRows.Min(row => row.WordOrderInMushaf).Should().Be(1);
        orderedRows.Max(row => row.WordOrderInMushaf).Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        orderedRows.Select(row => row.WordOrderInMushaf).Distinct().Should().HaveCount(DisplayWordsSyntheticSeed.ReadableWordCount);

        foreach (var surahGroup in orderedRows.GroupBy(row => row.SurahNumber))
        {
            var surahRows = surahGroup.OrderBy(row => row.WordOrderInSurah).ToList();
            surahRows.Min(row => row.WordOrderInSurah).Should().Be(1);
            surahRows.Max(row => row.WordOrderInSurah).Should().Be((short)surahRows.Count);
            surahRows.Select(row => row.WordOrderInSurah).Distinct().Should().HaveCount(surahRows.Count);
        }

        var sourceWords = await dbContext.QuranWords
            .Where(word => !word.IsAyahMarker)
            .ToDictionaryAsync(word => word.Id);

        foreach (var orderedRow in orderedRows)
        {
            var sourceWord = sourceWords[orderedRow.QuranWordId];
            orderedRow.WordOrderInAyah.Should().Be(sourceWord.WordNumber);
        }
    }
}
