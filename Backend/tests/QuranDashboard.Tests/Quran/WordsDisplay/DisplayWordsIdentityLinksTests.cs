using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Tests.Quran.WordsDisplay;

[Collection(nameof(WordsDisplayTestCollection))]
public sealed class DisplayWordsIdentityLinksTests
{
    private readonly WordsDisplayTestFixture fixture;

    public DisplayWordsIdentityLinksTests(WordsDisplayTestFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Rebuild_GroupsTashkeelByIgnoredMarksAndKeepsRetainedMarksDistinct()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var handler = fixture.CreateHandler();
        var reportDir = Path.Combine(Path.GetTempPath(), $"words-display-links-{Guid.NewGuid():N}");

        var result = await handler.HandleAsync(
            new RebuildDisplayWordsCommand(
                Force: false,
                ReportOutDir: reportDir,
                ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        (await dbContext.QuranWordsOrderedTashkeel.CountAsync())
            .Should()
            .Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        (await dbContext.QuranWordsOrderedSimple.CountAsync())
            .Should()
            .Be(DisplayWordsSyntheticSeed.ReadableWordCount);
        (await dbContext.QuranWordsUniqueTashkeel.CountAsync())
            .Should()
            .Be(DisplayWordsSyntheticSeed.UniqueTashkeelCount);
        (await dbContext.QuranWordsUniqueSimple.CountAsync())
            .Should()
            .Be(DisplayWordsSyntheticSeed.UniqueSimpleCount);

        var distinctImlaeiKeys = await dbContext.QuranWords
            .Where(word => !word.IsAyahMarker)
            .Select(word => word.WordKeyImlaeiSimple)
            .Distinct()
            .CountAsync();
        distinctImlaeiKeys.Should().Be(DisplayWordsSyntheticSeed.UniqueSimpleCount);

        var distinctTashkeelForms = await dbContext.QuranWords
            .Where(word => !word.IsAyahMarker)
            .Select(word => word.TextUthmani)
            .Distinct()
            .CountAsync();
        distinctTashkeelForms.Should().Be(DisplayWordsSyntheticSeed.ExactTashkeelCount);

        var ignoredMarksWord = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Id == 2);
        var decoratedIgnoredMarksWord = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Id == 5);

        ignoredMarksWord.TextUthmani.Should().NotBe(decoratedIgnoredMarksWord.TextUthmani);
        ignoredMarksWord.UniqueTashkeelWordId.Should().Be(decoratedIgnoredMarksWord.UniqueTashkeelWordId);
        ignoredMarksWord.UniqueTashkeelWordId.Should().NotBeNull();

        var ignoredMarksIdentity = await dbContext.QuranWordsUniqueTashkeel
            .AsNoTracking()
            .SingleAsync(row => row.Id == ignoredMarksWord.UniqueTashkeelWordId);
        ignoredMarksIdentity.TextUthmani.Should().Be(ignoredMarksWord.TextUthmani);
        ignoredMarksIdentity.FirstQuranWordId.Should().Be(ignoredMarksWord.Id);

        var retainedMarkIdentityIds = await dbContext.QuranWords
            .AsNoTracking()
            .Where(word => word.Id >= 7 && word.Id <= 10)
            .Select(word => word.UniqueTashkeelWordId)
            .Distinct()
            .ToListAsync();
        retainedMarkIdentityIds.Should().HaveCount(4);
        retainedMarkIdentityIds.Should().NotContainNulls();

        var divergenceWordA = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Id == 7);
        var divergenceWordB = await dbContext.QuranWords.AsNoTracking().SingleAsync(word => word.Id == 8);

        divergenceWordA.TextUthmaniSimple.Should().NotBe(divergenceWordB.TextUthmaniSimple);
        divergenceWordA.WordKeyImlaeiSimple.Should().Be(divergenceWordB.WordKeyImlaeiSimple);
        divergenceWordA.UniqueSimpleWordId.Should().Be(divergenceWordB.UniqueSimpleWordId);
        divergenceWordA.UniqueSimpleWordId.Should().NotBeNull();
        divergenceWordA.UniqueTashkeelWordId.Should().NotBe(divergenceWordB.UniqueTashkeelWordId);

        await DisplayWordsLinkTestHelpers.AssertIdentityLinksAreCompleteAndValidAsync(dbContext);
    }

    [Fact]
    public async Task ForcedRebuildTwice_ProducesIdenticalIdentityLinks()
    {
        await fixture.SeedDefaultSyntheticDataAsync();
        var command = new RebuildDisplayWordsCommand(
            Force: true,
            ReportOutDir: Path.Combine(Path.GetTempPath(), $"words-display-links-idem-0-{Guid.NewGuid():N}"),
            ExpectedReadableWords: DisplayWordsSyntheticSeed.ReadableWordCount);

        var firstResult = await fixture.CreateHandler().HandleAsync(
            command with { ReportOutDir = Path.Combine(Path.GetTempPath(), $"words-display-links-idem-1-{Guid.NewGuid():N}") },
            CancellationToken.None);
        firstResult.Succeeded.Should().BeTrue(firstResult.Message);

        var linksAfterFirstRun = await CaptureLinkSnapshotAsync();

        var secondResult = await fixture.CreateHandler().HandleAsync(
            command with { ReportOutDir = Path.Combine(Path.GetTempPath(), $"words-display-links-idem-2-{Guid.NewGuid():N}") },
            CancellationToken.None);
        secondResult.Succeeded.Should().BeTrue(secondResult.Message);

        var linksAfterSecondRun = await CaptureLinkSnapshotAsync();
        linksAfterSecondRun.Should().BeEquivalentTo(linksAfterFirstRun);
    }

    private async Task<IReadOnlyList<WordLinkProjection>> CaptureLinkSnapshotAsync()
    {
        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return await dbContext.QuranWords
            .AsNoTracking()
            .OrderBy(word => word.Id)
            .Select(word => new WordLinkProjection(
                word.Id,
                word.IsAyahMarker,
                word.UniqueTashkeelWordId,
                word.UniqueSimpleWordId))
            .ToListAsync();
    }

    private sealed record WordLinkProjection(
        int Id,
        bool IsAyahMarker,
        int? UniqueTashkeelWordId,
        int? UniqueSimpleWordId);
}
