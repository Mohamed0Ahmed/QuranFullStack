using QuranDashboard.Application.Quran.Words.RebuildDisplayWords;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(WordsDisplayTestCollection))]
public sealed class DisplayWordsSourceUntouchedTests
{
    private readonly WordsDisplayTestFixture fixture;

    public DisplayWordsSourceUntouchedTests(WordsDisplayTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task ForcedRebuild_DoesNotChangeSourceTableRowCounts()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var sourceCountsBefore = await ReadSourceCountsAsync();

        var handler = fixture.CreateHandler();
        var reportDir = Path.Combine(Path.GetTempPath(), $"words-display-source-{Guid.NewGuid():N}");

        var result = await handler.HandleAsync(
            new RebuildDisplayWordsCommand(
                Force: true,
                ReportOutDir: reportDir,
                ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);

        var sourceCountsAfter = await ReadSourceCountsAsync();
        sourceCountsAfter.Should().BeEquivalentTo(sourceCountsBefore);
    }

    private async Task<SourceCounts> ReadSourceCountsAsync()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new SourceCounts(
            await dbContext.QuranWords.CountAsync(),
            await dbContext.QuranAyahs.CountAsync(),
            await dbContext.QuranSurahs.CountAsync());
    }

    private sealed record SourceCounts(int Words, int Ayahs, int Surahs);
}
