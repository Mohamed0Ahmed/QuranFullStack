using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesRedundancyReadTests(WordTypesTestFixture fixture)
{
    private const int KalimaTashkeelId = 191001;
    private const int AlimaPastTashkeelId = 191003;

    private static readonly WordTypeFilter NounScope = new("noun", null, null, null, null);

    [Fact]
    public async Task ListWordsPage_NormalPage_IssuesSingleScopedCommand()
    {
        await using var dbContext = InterceptedContext(out var interceptor);
        var reader = new EfWordTypesReader(dbContext);

        interceptor.Reset();
        var page = await reader.GetRowsAsync(NounScope, WordTypeSortSpec.Default, 1, 25, CancellationToken.None);

        interceptor.CommandCount.Should().Be(1);
        page.TotalCount.Should().Be(4);
        page.Items.Should().HaveCount(4);
    }

    [Fact]
    public async Task GroupedTablePage_NormalPage_IssuesSingleScopedCommand()
    {
        await using var dbContext = InterceptedContext(out var interceptor);
        var reader = new EfWordTypesReader(dbContext);

        interceptor.Reset();
        var page = await reader.GetTableRowsAsync(
            NounScope, WordTypeTableView.Roots, WordTypeSortSpec.Default, 1, 25, CancellationToken.None);

        interceptor.CommandCount.Should().Be(1);
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task ListWordsPage_WindowCount_IsStableTotalAcrossPages()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var page1 = await reader.GetRowsAsync(NounScope, WordTypeSortSpec.Default, 1, 2, CancellationToken.None);
        var page2 = await reader.GetRowsAsync(NounScope, WordTypeSortSpec.Default, 2, 2, CancellationToken.None);

        page1.TotalCount.Should().Be(4);
        page2.TotalCount.Should().Be(4);
        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page2.Items.Select(row => (row.TashkeelWordId, row.ContextCode))
            .Should().NotIntersectWith(page1.Items.Select(row => (row.TashkeelWordId, row.ContextCode)));
    }

    [Fact]
    public async Task ListWordsPage_OutOfRangePage_ReturnsScopeTotalWithEmptyItems()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var page = await reader.GetRowsAsync(NounScope, WordTypeSortSpec.Default, 5, 25, CancellationToken.None);

        page.TotalCount.Should().Be(4);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListWordsPage_EmptyScope_ReturnsZeroTotalWithoutError()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var page = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, "مثل", HasRoot: true),
            WordTypeSortSpec.Default, 1, 25, CancellationToken.None);

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GroupedTablePage_EmptyScope_ReturnsZeroTotal()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var page = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, "مثل"),
            WordTypeTableView.Roots, WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListWordsPage_PreservesWinnersAndFirstOccurrenceOrder()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var page = await reader.GetRowsAsync(NounScope, WordTypeSortSpec.Natural(WordTypeSortColumn.MushafOrder), 1, 25, CancellationToken.None);

        var kalimaNoun = page.Items.Single(row => row.TashkeelWordId == KalimaTashkeelId && row.ContextCode == "N");
        kalimaNoun.RootText.Should().Be("ك ل م");
        kalimaNoun.LemmaText.Should().NotBeNull();
        kalimaNoun.StemText.Should().NotBeNull();
        page.Items.Select(row => (row.TashkeelWordId, row.ContextCode)).Should().OnlyHaveUniqueItems(
            "MushafOrder over the words view stays row-per-(identity,context)");
    }

    [Fact]
    public async Task FlatAyahMatches_KnownIdentity_UsesFourCommandsWithoutExistenceProbe()
    {
        await using var dbContext = InterceptedContext(out var interceptor);
        var reader = new EfWordTypesReader(dbContext);

        interceptor.Reset();
        var page = await reader.GetAyahMatchesAsync(
            new WordTypeRowIdentity(KalimaTashkeelId, "N", null, null, null), 1, 25, CancellationToken.None);

        interceptor.CommandCount.Should().Be(4);
        page.Should().NotBeNull();
        page!.TotalCount.Should().Be(1);
        page.Items.Single().VerseKey.Should().Be("1:1");
        page.Items.Single().MatchedWordPositions.Should().Equal([1]);
    }

    [Fact]
    public async Task FlatAyahMatches_UnknownIdentity_ReturnsNull()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var page = await reader.GetAyahMatchesAsync(
            new WordTypeRowIdentity(999_999, "N", null, null, null), 1, 25, CancellationToken.None);

        page.Should().BeNull();
    }

    [Fact]
    public async Task FlatSurahs_KnownIdentity_UsesTwoCommandsAndPreservesMentionedAndMissing()
    {
        await using var dbContext = InterceptedContext(out var interceptor);
        var reader = new EfWordTypesReader(dbContext);

        interceptor.Reset();
        var surahs = await reader.GetSurahsAsync(
            new WordTypeRowIdentity(AlimaPastTashkeelId, "past", null, null, null), CancellationToken.None);

        interceptor.CommandCount.Should().Be(2);
        surahs.Should().NotBeNull();
        surahs!.Surahs.Should().ContainSingle(item => item.SurahNumber == 2);
        surahs.MissingSurahs.Select(item => item.SurahNumber).Should().BeEquivalentTo([1, 3]);
    }

    [Fact]
    public async Task FlatSurahs_UnknownIdentity_ReturnsNull()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var surahs = await reader.GetSurahsAsync(
            new WordTypeRowIdentity(999_999, "N", null, null, null), CancellationToken.None);

        surahs.Should().BeNull();
    }

    private QuranDashboardDbContext InterceptedContext(out SqlCommandCountInterceptor interceptor)
    {
        interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        return new QuranDashboardDbContext(options);
    }
}
