using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Roots;
using QuranDashboard.Tests.Quran.Words;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;
using QuranDashboard.Tests.TestSupport.Logging;

namespace QuranDashboard.Tests.Quran.WordsRoots;

[Collection(nameof(RootsExplorerCollection))]
public sealed class RootsListReadTests(RootsExplorerTestFixture fixture)
{
    private const int SeededRootCount = 3;

    private const int DivergentRootId = 10;

    [Fact]
    public async Task GetRootsPage_returns_default_page_with_all_seeded_roots()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery(null, null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
        page.TotalCount.Should().Be(SeededRootCount);
        page.Items.Should().HaveCount(SeededRootCount);
    }

    [Fact]
    public async Task GetRootsPage_carries_all_eight_counts_for_every_root()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;

        foreach (var item in page.Items)
        {
            item.RootText.Should().NotBeNullOrWhiteSpace();
            item.OccurrencesCount.Should().BeGreaterThan(0);
            item.AyahsCount.Should().BeGreaterThanOrEqualTo(1);
            item.SurahsCount.Should().BeInRange(1, 114);
            item.SimpleWordsCount.Should().BeGreaterThanOrEqualTo(1);
            item.TashkeelWordsCount.Should().BeGreaterThanOrEqualTo(1);
            item.LemmasCount.Should().BeGreaterThanOrEqualTo(1);
            item.StemsCount.Should().BeGreaterThanOrEqualTo(1);
        }
    }

    [Fact]
    public async Task GetRootsPage_occurrences_equals_quran_roots_words_count()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;

        var r10 = page.Items.Single(i => i.Id == DivergentRootId);
        r10.OccurrencesCount.Should().Be(5);
        r10.RootText.Should().Be("ر ح م");
    }

    [Fact]
    public async Task GetRootsPage_lemmas_use_co_occurrence_not_ownership()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;

        var r10 = page.Items.Single(i => i.Id == DivergentRootId);
        r10.LemmasCount.Should().Be(2);
    }

    [Fact]
    public async Task GetRootsPage_search_filters_by_root_text()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery("رحم", null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;

        page.Items.Should().ContainSingle(i => i.Id == DivergentRootId);
    }

    [Fact]
    public async Task GetRootsPage_search_returns_empty_when_nothing_matches()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery("لا_يوجد_مثل_هذا_الجذر", null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("occurrences")]
    [InlineData("alpha")]
    [InlineData("mushaf-order")]
    public async Task GetRootsPage_applies_each_supported_sort_without_error(string sort)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery(null, sort, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;

        page.Items.Should().HaveCount(SeededRootCount);
    }

    [Fact]
    public async Task GetRootsPage_occurrences_sort_orders_by_count_desc()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery(null, "occurrences", 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.Id).Should().Equal(30, 10, 20);
    }

    [Fact]
    public async Task GetRootsPage_mushaf_order_sort_orders_by_first_word_order_in_mushaf()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery(null, "mushaf-order", 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.Id).Should().Equal(10, 30, 20);
    }

    [Fact]
    public async Task GetRootsPage_pages_the_results()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var first = await handler.HandleAsync(
            new GetRootsPageQuery(null, null, 1, 2),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(SeededRootCount);
        firstPage.Items.Should().HaveCount(2);

        var second = await handler.HandleAsync(
            new GetRootsPageQuery(null, null, 2, 2),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetRootsPageOutcome.Success>().Subject.Page;
        secondPage.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRootsPage_repeated_read_issues_no_new_db_commands_after_cache()
    {

        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var inner = new EfRootsReader(dbContext);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedRootsReader(inner, cache);

        await reader.GetRootsPageAsync(null, RootSort.MushafOrder, 1, 50, CancellationToken.None);

        interceptor.Reset();
        await reader.GetRootsPageAsync(null, RootSort.Occurrences, 2, 2, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "the whole summary is cached once; sort/page are in-memory");
    }

    [Theory]
    [InlineData("relevance")]
    [InlineData("")]
    public async Task GetRootsPage_invalid_sort_returns_validation_outcome(string? sort)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery(null, sort, 1, 50),
            CancellationToken.None);

        if (string.IsNullOrEmpty(sort))
        {
            outcome.Should().BeOfType<GetRootsPageOutcome.Success>();
        }
        else
        {
            outcome.Should().BeOfType<GetRootsPageOutcome.InvalidSort>();
        }
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 1001)]
    public async Task GetRootsPage_invalid_paging_returns_validation_outcome(int page, int pageSize)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootsPageQuery(null, null, page, pageSize),
            CancellationToken.None);

        outcome.Should().BeOfType<GetRootsPageOutcome.InvalidPaging>();
    }

    [Fact]
    public async Task GetRootsPage_log_carries_required_fields_and_no_root_or_search_text()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        await handler.HandleAsync(
            new GetRootsPageQuery("رحم", "occurrences", 1, 50),
            CancellationToken.None);

        var entries = fixture.LoggingProvider.Entries
            .Where(e => e.Message.Contains("GetRootsPage"))
            .ToList();
        entries.Should().NotBeEmpty();

        var completed = entries.Single(e => e.Message.Contains("Completed"));
        completed.Level.Should().Be(LogLevel.Information);

        var fields = completed.StructuredFieldsWithoutOriginalFormat()
            .Select(p => p.Key)
            .ToArray();
        fields.Should().Contain(new[]
        {
            "feature", "operation", "sort", "pageNumber", "pageSize", "totalCount", "itemCount", "hasSearch",
        });

        completed.Message.Should().NotContain("ر ح م");
        completed.Message.Should().NotContain("رحم");
        completed.Message.Should().NotContain("رَحْم");
    }
}
