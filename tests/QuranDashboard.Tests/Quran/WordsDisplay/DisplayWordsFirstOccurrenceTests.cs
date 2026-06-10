
using QuranDashboard.Application.Quran.Words.RebuildDisplayWords;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(WordsDisplayTestCollection))]
public sealed class DisplayWordsFirstOccurrenceTests
{
    private readonly WordsDisplayTestFixture fixture;

    public DisplayWordsFirstOccurrenceTests(WordsDisplayTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Rebuild_RecordsFirstOccurrenceAndSumsOccurrencesToReadableCount()
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

        var orderedTashkeelRows = await dbContext.QuranWordsOrderedTashkeel.ToListAsync();

        foreach (var uniqueRow in await dbContext.QuranWordsUniqueTashkeel.ToListAsync())
        {
            var groupRows = orderedTashkeelRows
                .Where(row => row.TextUthmani == uniqueRow.TextUthmani)
                .ToList();

            var earliest = groupRows.MinBy(row => row.WordOrderInMushaf)!;

            uniqueRow.FirstWordOrderInMushaf.Should().Be(earliest.WordOrderInMushaf);
            uniqueRow.FirstQuranWordId.Should().Be(earliest.QuranWordId);
            uniqueRow.FirstLocation.Should().Be(earliest.Location);
            uniqueRow.FirstSurahNumber.Should().Be(earliest.SurahNumber);
            uniqueRow.FirstAyahNumber.Should().Be(earliest.AyahNumber);
            uniqueRow.FirstPageNumber.Should().Be(earliest.PageNumber);
            uniqueRow.FirstLineNumber.Should().Be(earliest.LineNumber);
        }

        var orderedSimpleRows = await dbContext.QuranWordsOrderedSimple.ToListAsync();

        foreach (var uniqueRow in await dbContext.QuranWordsUniqueSimple.ToListAsync())
        {
            var groupRows = orderedSimpleRows
                .Where(row => row.WordKeyImlaeiSimple == uniqueRow.WordKeyImlaeiSimple)
                .ToList();

            var earliest = groupRows.MinBy(row => row.WordOrderInMushaf)!;

            uniqueRow.FirstWordOrderInMushaf.Should().Be(earliest.WordOrderInMushaf);
            uniqueRow.FirstQuranWordId.Should().Be(earliest.QuranWordId);
            uniqueRow.FirstLocation.Should().Be(earliest.Location);
            uniqueRow.FirstSurahNumber.Should().Be(earliest.SurahNumber);
            uniqueRow.FirstAyahNumber.Should().Be(earliest.AyahNumber);
            uniqueRow.FirstPageNumber.Should().Be(earliest.PageNumber);
            uniqueRow.FirstLineNumber.Should().Be(earliest.LineNumber);
        }

        (await dbContext.QuranWordsUniqueTashkeel.SumAsync(row => row.OccurrencesCount))
            .Should()
            .Be(DisplayWordsSyntheticSeed.ReadableWordCount);

        (await dbContext.QuranWordsUniqueSimple.SumAsync(row => row.OccurrencesCount))
            .Should()
            .Be(DisplayWordsSyntheticSeed.ReadableWordCount);
    }
}
