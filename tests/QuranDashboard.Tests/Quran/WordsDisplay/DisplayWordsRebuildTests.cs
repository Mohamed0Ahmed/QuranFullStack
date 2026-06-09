using QuranDashboard.Application.Quran.Words.RebuildDisplayWords;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(WordsDisplayTestCollection))]
public sealed class DisplayWordsRebuildTests
{
    private readonly WordsDisplayTestFixture fixture;

    public DisplayWordsRebuildTests(WordsDisplayTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task RebuildIntoEmptyDatabase_PopulatesAllFourDerivedTables()
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
        result.ReportOutDir.Should().Be(reportDir);
        File.Exists(Path.Combine(reportDir, "words-display-report.md")).Should().BeTrue();
        File.Exists(Path.Combine(reportDir, "words-display-report.json")).Should().BeTrue();
        result.Totals.Should().NotBeNull();
        result.Totals!.OrderedTashkeelRows.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        result.Totals.OrderedSimpleRows.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        result.Totals.UniqueTashkeelRows.Should().Be(DisplayWordsSyntheticSeed.UniqueTashkeelCount);
        result.Totals.UniqueSimpleRows.Should().Be(DisplayWordsSyntheticSeed.UniqueSimpleCount);
        result.Totals.ReadableWords.Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await dbContext.QuranWordsOrderedTashkeel.CountAsync()).Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        (await dbContext.QuranWordsOrderedSimple.CountAsync()).Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        (await dbContext.QuranWordsUniqueTashkeel.CountAsync()).Should().Be(DisplayWordsSyntheticSeed.UniqueTashkeelCount);
        (await dbContext.QuranWordsUniqueSimple.CountAsync()).Should().Be(DisplayWordsSyntheticSeed.UniqueSimpleCount);

        var markerMappedRows = await dbContext.QuranWordsOrderedTashkeel
            .Join(
                dbContext.QuranWords,
                ordered => ordered.QuranWordId,
                source => source.Id,
                (_, source) => source.IsAyahMarker)
            .CountAsync(marker => marker);

        markerMappedRows.Should().Be(0);
    }

    [Fact]
    public async Task Rebuild_ExcludesAyahMarkersFromDerivedTablesAndCounts()
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

        (await dbContext.QuranWords.CountAsync(word => word.IsAyahMarker)).Should().Be(1);

        var orderedTashkeelIds = await dbContext.QuranWordsOrderedTashkeel
            .Select(row => row.QuranWordId)
            .ToListAsync();
        var orderedSimpleIds = await dbContext.QuranWordsOrderedSimple
            .Select(row => row.QuranWordId)
            .ToListAsync();

        orderedTashkeelIds.Should().NotContain(DisplayWordsSyntheticSeed.AyahMarkerWord.Id);
        orderedSimpleIds.Should().NotContain(DisplayWordsSyntheticSeed.AyahMarkerWord.Id);

        (await dbContext.QuranWordsOrderedTashkeel.CountAsync()).Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        (await dbContext.QuranWordsOrderedSimple.CountAsync()).Should().Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        (await dbContext.QuranWordsUniqueTashkeel.CountAsync()).Should().Be(DisplayWordsSyntheticSeed.UniqueTashkeelCount);
        (await dbContext.QuranWordsUniqueSimple.CountAsync()).Should().Be(DisplayWordsSyntheticSeed.UniqueSimpleCount);

        (await dbContext.QuranWordsOrderedTashkeel.AnyAsync(row => row.TextUthmani == DisplayWordsSyntheticSeed.AyahMarkerToken))
            .Should()
            .BeFalse();
        (await dbContext.QuranWordsOrderedSimple.AnyAsync(row => row.TextUthmaniSimple == DisplayWordsSyntheticSeed.AyahMarkerToken))
            .Should()
            .BeFalse();
        (await dbContext.QuranWordsUniqueTashkeel.AnyAsync(row => row.TextUthmani == DisplayWordsSyntheticSeed.AyahMarkerToken))
            .Should()
            .BeFalse();
        (await dbContext.QuranWordsUniqueSimple.AnyAsync(row => row.TextUthmaniSimple == DisplayWordsSyntheticSeed.AyahMarkerToken))
            .Should()
            .BeFalse();
    }
}

[CollectionDefinition(nameof(WordsDisplayTestCollection))]
public sealed class WordsDisplayTestCollection : ICollectionFixture<WordsDisplayTestFixture>;
