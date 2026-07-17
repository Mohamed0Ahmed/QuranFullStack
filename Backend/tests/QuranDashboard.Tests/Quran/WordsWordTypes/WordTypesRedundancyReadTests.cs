using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

// B1/B3 performance-review regressions: the list/table reads must build the expensive scoped relation
// exactly ONCE per normal page (window count, not a separate COUNT command), and the flat Word Type
// detail reads must stop repeating the existence probe / duplicate catalogue work. Every assertion also
// pins the counts, order, and zero/out-of-range semantics the single-statement path must preserve.
[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesRedundancyReadTests(WordTypesTestFixture fixture)
{
    private const int KalimaTashkeelId = 191001;      // كَلِمَة identity (N/PN/ADJ noun contexts)
    private const int AlimaPastTashkeelId = 191003;   // عَلِمَ, past verb context

    private static readonly WordTypeFilter NounScope = new("noun", null, null, null, null);

    // B1: a normal words page returns page + total from ONE scoped command (previously a COUNT command
    // then a page command rebuilt the same base/grouped CTE).
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

    // B1: a normal grouped (roots) table page also collapses to a single scoped command.
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

    // The window count must be the whole scoped total on every page, independent of LIMIT — paging stays stable.
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
        // Rows key on (identity, context) — identity 191001 spans N/PN/ADJ — so compare the composite key.
        page2.Items.Select(row => (row.TashkeelWordId, row.ContextCode))
            .Should().NotIntersectWith(page1.Items.Select(row => (row.TashkeelWordId, row.ContextCode)));
    }

    // Out-of-range page keeps the scope TotalCount with an empty page (the window count would otherwise be
    // lost on the empty page — the count-only fallback must recover it).
    [Fact]
    public async Task ListWordsPage_OutOfRangePage_ReturnsScopeTotalWithEmptyItems()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var page = await reader.GetRowsAsync(NounScope, WordTypeSortSpec.Default, 5, 25, CancellationToken.None);

        page.TotalCount.Should().Be(4);
        page.Items.Should().BeEmpty();
    }

    // A genuinely empty scope reports TotalCount 0 with an empty page (search "مثل" keeps only the rootless
    // مُثَل identity, which hasRoot=true then excludes — zero rows).
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

    // An empty grouped scope reports TotalCount 0 (search "مثل" keeps only the rootless identity, so the
    // roots grouping collapses to zero).
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

    // The single-statement path must not disturb the winner selection or first-occurrence order: the
    // كَلِمَة "N" noun row still resolves its real root/lemma/stem winners in deterministic Mushaf order.
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

    // B3: flat ayah matches drop the preliminary EXISTS probe and reuse one matched-word projection —
    // the same known identity now costs one fewer command while returning the identical page.
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

    // B3: a missing identity is still a null (404) result, now derived from the distinct count rather than
    // a separate AnyAsync probe.
    [Fact]
    public async Task FlatAyahMatches_UnknownIdentity_ReturnsNull()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var page = await reader.GetAyahMatchesAsync(
            new WordTypeRowIdentity(999_999, "N", null, null, null), 1, 25, CancellationToken.None);

        page.Should().BeNull();
    }

    // B3: flat surahs mirror the grouped implementation — one server-side occurrence aggregate plus one
    // projected catalogue read (two commands), deriving mentioned/missing in memory.
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

    // B3: a missing identity still returns null for surahs (existence now comes from the aggregate).
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
