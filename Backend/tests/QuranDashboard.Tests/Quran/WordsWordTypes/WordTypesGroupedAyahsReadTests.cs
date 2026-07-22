using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedAyahs;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesGroupedAyahsReadTests(WordTypesTestFixture fixture)
{
    private const int FatihaAyah1Id = 190011;
    private const int FatihaAyah2Id = 190012;
    private const int MarkerWordId = 1903004;
    private const int KalimaNounWordId = 1903001;
    private const int KalimaProperNounWordId = 1903002;

    private static readonly WordTypeFilter NounScope = new("noun", null, null, null, null);
    private static readonly WordTypeFilter VerbScope = new("verb", null, null, null, null);

    [Theory]
    [InlineData(WordTypeGroupedDimensionKind.Root, 190700)]
    [InlineData(WordTypeGroupedDimensionKind.Stem, 190600)]
    [InlineData(WordTypeGroupedDimensionKind.Lemma, 190500)]
    public async Task GroupedAyahs_RootStemLemma_ReturnOnlySameScopeMatches(
        WordTypeGroupedDimensionKind kind,
        int dimensionId)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var page = await AyahsAsync(reader, kind, dimensionId, NounScope);

        page.Should().NotBeNull();
        page!.TotalCount.Should().Be(2);
        page.Items.Select(item => item.VerseKey).Should().Equal("1:1", "1:2");
    }

    [Fact]
    public async Task GroupedAyahs_ActiveCaseTenseAndVoiceFiltersPropagateToBase()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var allTenses = await AyahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190701, VerbScope);
        allTenses!.TotalCount.Should().Be(2);

        var nominative = await AyahsAsync(
            reader, WordTypeGroupedDimensionKind.Root, 190700, new WordTypeFilter("noun", null, "nominative", null, null));
        nominative!.Items.Select(item => item.VerseKey).Should().Equal("1:1");

        var past = await AyahsAsync(
            reader, WordTypeGroupedDimensionKind.Root, 190701, new WordTypeFilter("verb", null, null, "past", null));
        past!.Items.Select(item => item.VerseKey).Should().Equal("2:25");

        var passive = await AyahsAsync(
            reader, WordTypeGroupedDimensionKind.Root, 190701, new WordTypeFilter("verb", null, null, null, "passive"));
        passive!.Items.Select(item => item.VerseKey).Should().Equal("2:25");
    }

    [Fact]
    public async Task GroupedAyahs_PaginatesDistinctAyahsBeforeHydratingWords()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var page1 = await AyahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope, 1, 1);
        var page2 = await AyahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope, 2, 1);
        var page3 = await AyahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope, 3, 1);

        new[] { page1, page2, page3 }.Should().OnlyContain(page => page!.TotalCount == 2);
        page1!.Items.Single().VerseKey.Should().Be("1:1");
        page2!.Items.Single().VerseKey.Should().Be("1:2");
        page3!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GroupedAyahs_UsesQuranWordsUthmaniTextAndMatchedWordIdsForHighlighting()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var page = await AyahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope);
        var firstAyah = page!.Items.Single(item => item.VerseKey == "1:1");

        var expectedWords = await db.QuranWords.AsNoTracking()
            .Where(word => word.AyahId == FatihaAyah1Id && !word.IsAyahMarker)
            .OrderBy(word => word.WordNumber)
            .Select(word => new { word.Id, word.TextUthmani })
            .ToListAsync();

        firstAyah.Words.Select(word => word.QuranWordId).Should().Equal(expectedWords.Select(word => word.Id));
        firstAyah.Words.Select(word => word.TextUthmani).Should().Equal(expectedWords.Select(word => word.TextUthmani));
        firstAyah.MatchedWordIds.Should().Equal(KalimaNounWordId, KalimaProperNounWordId);
        firstAyah.MatchedWordPositions.Should().Equal(1, 2);
    }

    [Fact]
    public async Task GroupedAyahs_ExcludesMarkersAndOutOfScopeDimensions()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var page = await AyahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope);
        var secondAyah = page!.Items.Single(item => item.VerseKey == "1:2");
        secondAyah.Words.Should().NotContain(word => word.QuranWordId == MarkerWordId);
        secondAyah.Words.Should().OnlyContain(word => !word.IsAyahMarker);

        (await AyahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190701, NounScope)).Should().BeNull();
        (await AyahsAsync(reader, WordTypeGroupedDimensionKind.Lemma, 190502, NounScope)).Should().BeNull();
        (await AyahsAsync(reader, WordTypeGroupedDimensionKind.Stem, 190602, NounScope)).Should().BeNull();
    }

    [Fact]
    public async Task GroupedAyahs_HeadDimensionIgnoresSecondarySegmentDimension()
    {
        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await using var transaction = await WordTypesSyntheticStructuralData.BeginSeededTransactionAsync(dbContext);
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();
        var syntheticScope = new WordTypeFilter(
            "noun", WordTypesSyntheticStructuralData.NounChildCode, null, null, null);

        var segmentAyahs = await AyahsAsync(
            reader,
            WordTypeGroupedDimensionKind.Lemma,
            WordTypesSyntheticStructuralData.SegmentOnlyLemmaId,
            syntheticScope);
        var headAyahs = await AyahsAsync(
            reader,
            WordTypeGroupedDimensionKind.Lemma,
            WordTypesSyntheticStructuralData.MushafFirstLemmaId,
            syntheticScope);

        segmentAyahs.Should().BeNull();
        headAyahs.Should().NotBeNull();
        headAyahs!.TotalCount.Should().Be(1);
        headAyahs.Items.Should().ContainSingle()
            .Which.MatchedWordIds.Should().Equal(WordTypesSyntheticStructuralData.MushafFirstWordId);
    }

    [Fact]
    public async Task GroupedAyahs_OutOfRangePageIsEmptyButExistingSelectionIsNotNotFound()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var outOfRange = await AyahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190700, NounScope, 5, 25);
        outOfRange.Should().NotBeNull();
        outOfRange!.Items.Should().BeEmpty();
        outOfRange.TotalCount.Should().Be(2);

        var absent = await AyahsAsync(reader, WordTypeGroupedDimensionKind.Root, 190701, NounScope);
        absent.Should().BeNull();
    }

    [Fact]
    public async Task GroupedAyahs_PageHydrationUsesBoundedCommandCount()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfWordTypesReader(dbContext);

        interceptor.Reset();
        var twoAyahs = await reader.GetGroupedAyahMatchesAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Root, 190700, NounScope), 1, 25, CancellationToken.None);
        var twoAyahCommandCount = interceptor.CommandCount;

        interceptor.Reset();
        var oneAyah = await reader.GetGroupedAyahMatchesAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Root, 190700, new WordTypeFilter("noun", null, "nominative", null, null)),
            1, 25, CancellationToken.None);
        var oneAyahCommandCount = interceptor.CommandCount;

        twoAyahs!.TotalCount.Should().Be(2);
        oneAyah!.TotalCount.Should().Be(1);
        twoAyahCommandCount.Should().Be(3);
        oneAyahCommandCount.Should().Be(twoAyahCommandCount);
    }

    [Fact]
    public async Task GroupedAyahsHandler_InvalidPagingAndMissingGroup_ReturnControlledOutcomes()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeGroupedAyahsHandler>();

        (await handler.HandleAsync(
            new GetWordTypeGroupedAyahsQuery("roots", 190700, "noun", null, null, null, null, 0, 25),
            CancellationToken.None)).Should().BeOfType<GetWordTypeGroupedAyahsOutcome.InvalidPaging>();

        (await handler.HandleAsync(
            new GetWordTypeGroupedAyahsQuery("roots", 190700, "noun", null, null, null, null, 1, 101),
            CancellationToken.None)).Should().BeOfType<GetWordTypeGroupedAyahsOutcome.InvalidPaging>();

        (await handler.HandleAsync(
            new GetWordTypeGroupedAyahsQuery("bogus", 190700, "noun", null, null, null, null, 1, 25),
            CancellationToken.None)).Should().BeOfType<GetWordTypeGroupedAyahsOutcome.InvalidKind>();

        (await handler.HandleAsync(
            new GetWordTypeGroupedAyahsQuery("roots", 0, "noun", null, null, null, null, 1, 25),
            CancellationToken.None)).Should().BeOfType<GetWordTypeGroupedAyahsOutcome.InvalidId>();

        (await handler.HandleAsync(
            new GetWordTypeGroupedAyahsQuery("roots", 999_999, "noun", null, null, null, null, 1, 25),
            CancellationToken.None)).Should().BeOfType<GetWordTypeGroupedAyahsOutcome.NotFound>();
    }

    private static Task<PagedResult<WordTypeAyahMatchDto>?> AyahsAsync(
        IWordTypesReader reader,
        WordTypeGroupedDimensionKind kind,
        int dimensionId,
        WordTypeFilter filter,
        int page = 1,
        int pageSize = 25) =>
        reader.GetGroupedAyahMatchesAsync(
            new WordTypeGroupedSelection(kind, dimensionId, filter),
            page,
            pageSize,
            CancellationToken.None);
}
