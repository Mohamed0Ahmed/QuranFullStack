using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(WordsDisplayTestCollection))]
public sealed class DisplayWordsDeterministicIdTests : IDisposable
{
    private readonly WordsDisplayTestFixture fixture;
    private readonly List<string> tempReportDirs = [];

    public DisplayWordsDeterministicIdTests(WordsDisplayTestFixture fixture)
    {
        this.fixture = fixture;
    }

    private string CreateTempReportDir(string label)
    {
        var reportDir = Path.Combine(Path.GetTempPath(), $"words-display-{label}-{Guid.NewGuid():N}");
        tempReportDirs.Add(reportDir);
        return reportDir;
    }

    public void Dispose()
    {
        foreach (var reportDir in tempReportDirs)
        {
            try
            {
                if (Directory.Exists(reportDir))
                {
                    Directory.Delete(reportDir, recursive: true);
                }
            }
            catch (IOException)
            {

            }
            catch (UnauthorizedAccessException)
            {

            }
        }
    }

    [Fact]
    public async Task Rebuild_AssignsDeterministicUniqueIdsEqualToFirstQuranWordId()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var handler = fixture.CreateHandler();
        var reportDir = CreateTempReportDir("detid");

        var result = await handler.HandleAsync(
            new RebuildDisplayWordsCommand(
                Force: false,
                ReportOutDir: reportDir,
                ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var tashkeelViolations = await dbContext.QuranWordsUniqueTashkeel
            .AsNoTracking()
            .CountAsync(row => row.Id != row.FirstQuranWordId);
        tashkeelViolations.Should().Be(0);

        var simpleViolations = await dbContext.QuranWordsUniqueSimple
            .AsNoTracking()
            .CountAsync(row => row.Id != row.FirstQuranWordId);
        simpleViolations.Should().Be(0);

        var anchorTashkeel = await dbContext.QuranWordsUniqueTashkeel
            .AsNoTracking()
            .OrderBy(row => row.FirstWordOrderInMushaf)
            .FirstAsync();
        var firstTashkeelOccurrence = await dbContext.QuranWords
            .AsNoTracking()
            .Where(word => !word.IsAyahMarker && word.TextUthmani == anchorTashkeel.TextUthmani)
            .OrderBy(word => word.Id)
            .FirstAsync();
        anchorTashkeel.Id.Should().Be(firstTashkeelOccurrence.Id);
        anchorTashkeel.FirstQuranWordId.Should().Be(firstTashkeelOccurrence.Id);

        var anchorSimple = await dbContext.QuranWordsUniqueSimple
            .AsNoTracking()
            .OrderBy(row => row.FirstWordOrderInMushaf)
            .FirstAsync();
        var firstSimpleOccurrence = await dbContext.QuranWords
            .AsNoTracking()
            .Where(word => !word.IsAyahMarker && word.WordKeyImlaeiSimple == anchorSimple.WordKeyImlaeiSimple)
            .OrderBy(word => word.Id)
            .FirstAsync();
        anchorSimple.Id.Should().Be(firstSimpleOccurrence.Id);
        anchorSimple.FirstQuranWordId.Should().Be(firstSimpleOccurrence.Id);
    }

    [Fact]
    public async Task Rebuild_ProducesUniqueIdsWithNoDuplicates()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var handler = fixture.CreateHandler();
        var reportDir = CreateTempReportDir("detid-uniq");

        var result = await handler.HandleAsync(
            new RebuildDisplayWordsCommand(
                Force: false,
                ReportOutDir: reportDir,
                ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var tashkeelRows = await dbContext.QuranWordsUniqueTashkeel.AsNoTracking().ToListAsync();
        tashkeelRows.Select(row => row.Id).Distinct().Count()
            .Should().Be(tashkeelRows.Count);

        var simpleRows = await dbContext.QuranWordsUniqueSimple.AsNoTracking().ToListAsync();
        simpleRows.Select(row => row.Id).Distinct().Count()
            .Should().Be(simpleRows.Count);
    }

    [Fact]
    public async Task Rebuild_RepublishesDeterministicIdsIntoQuranWordLinks()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var handler = fixture.CreateHandler();
        var reportDir = CreateTempReportDir("detid-links");

        var result = await handler.HandleAsync(
            new RebuildDisplayWordsCommand(
                Force: false,
                ReportOutDir: reportDir,
                ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var readableWords = await dbContext.QuranWords
            .AsNoTracking()
            .Where(word => !word.IsAyahMarker)
            .Select(word => new
            {
                word.Id,
                word.TextUthmani,
                word.WordKeyImlaeiSimple,
                word.UniqueTashkeelWordId,
                word.UniqueSimpleWordId
            })
            .ToListAsync();

        var tashkeelById = await dbContext.QuranWordsUniqueTashkeel
            .AsNoTracking()
            .ToDictionaryAsync(row => row.Id, row => row.TextUthmani);
        var simpleById = await dbContext.QuranWordsUniqueSimple
            .AsNoTracking()
            .ToDictionaryAsync(row => row.Id, row => row.WordKeyImlaeiSimple);

        foreach (var word in readableWords)
        {
            word.UniqueTashkeelWordId.Should().NotBeNull();
            word.UniqueSimpleWordId.Should().NotBeNull();

            var tashkeelId = word.UniqueTashkeelWordId!.Value;
            tashkeelById[tashkeelId].Should().Be(word.TextUthmani,
                $"tashkeel link {tashkeelId} on quran word {word.Id} must resolve to the matching unique row");

            var simpleId = word.UniqueSimpleWordId!.Value;
            simpleById[simpleId].Should().Be(word.WordKeyImlaeiSimple,
                $"simple link {simpleId} on quran word {word.Id} must resolve to the matching unique row");
        }
    }

    [Fact]
    public async Task ForcedRebuildTwice_ProducesIdenticalDeterministicIdMappings()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var command = new RebuildDisplayWordsCommand(
            Force: true,
            ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount);

        var firstResult = await fixture.CreateHandler().HandleAsync(
            command with { ReportOutDir = CreateTempReportDir("detid-idem-1") },
            CancellationToken.None);
        firstResult.Succeeded.Should().BeTrue(firstResult.Message);

        var firstMapping = await CaptureIdMappingAsync();

        var secondResult = await fixture.CreateHandler().HandleAsync(
            command with { ReportOutDir = CreateTempReportDir("detid-idem-2") },
            CancellationToken.None);
        secondResult.Succeeded.Should().BeTrue(secondResult.Message);

        var secondMapping = await CaptureIdMappingAsync();
        secondMapping.Should().BeEquivalentTo(firstMapping);
    }

    private async Task<DeterministicIdMapping> CaptureIdMappingAsync()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var tashkeel = await dbContext.QuranWordsUniqueTashkeel
            .AsNoTracking()
            .OrderBy(row => row.FirstWordOrderInMushaf)
            .ThenBy(row => row.TextUthmani)
            .Select(row => new UniqueIdMappingRow(
                row.Id,
                row.TextUthmani,
                row.FirstQuranWordId,
                row.FirstWordOrderInMushaf))
            .ToListAsync();

        var simple = await dbContext.QuranWordsUniqueSimple
            .AsNoTracking()
            .OrderBy(row => row.FirstWordOrderInMushaf)
            .ThenBy(row => row.WordKeyImlaeiSimple)
            .Select(row => new UniqueIdMappingRow(
                row.Id,
                row.WordKeyImlaeiSimple,
                row.FirstQuranWordId,
                row.FirstWordOrderInMushaf))
            .ToListAsync();

        var links = await dbContext.QuranWords
            .AsNoTracking()
            .OrderBy(word => word.Id)
            .Select(word => new WordLinkProjection(
                word.Id,
                word.UniqueTashkeelWordId,
                word.UniqueSimpleWordId))
            .ToListAsync();

        return new DeterministicIdMapping(tashkeel, simple, links);
    }

    private sealed record DeterministicIdMapping(
        IReadOnlyList<UniqueIdMappingRow> Tashkeel,
        IReadOnlyList<UniqueIdMappingRow> Simple,
        IReadOnlyList<WordLinkProjection> Links);

    private sealed record UniqueIdMappingRow(
        int Id,
        string Identity,
        int FirstQuranWordId,
        int FirstWordOrderInMushaf);

    private sealed record WordLinkProjection(
        int Id,
        int? UniqueTashkeelWordId,
        int? UniqueSimpleWordId);
}
