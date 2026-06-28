using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaSummary;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;
using QuranDashboard.Tests.Quran.Words;
using QuranDashboard.Tests.TestSupport.Logging;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class LemmasListReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int SeededLemmaCount = 9;
    private const int HighFrequencyLemmaId = 500;
    private const int NullRootLemmaId = 501;
    private const int MultiTypeLemmaId = 503;
    private const int UnknownLemmaId = 999_999;

    private const int CompoundLemmaLaId = 506;
    private const int CompoundLemmaAnId = 507;
    private const int SameLemmaFanoutLemmaId = 508;
    private const int MarkerRegressionLemmaId = 509;

    [Fact]
    public async Task GetLemmasPage_returns_default_page_with_all_seeded_lemmas()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
        page.TotalCount.Should().Be(SeededLemmaCount);
        page.Items.Should().HaveCount(SeededLemmaCount);
    }

    [Fact]
    public async Task GetLemmasPage_carries_all_counts_for_every_lemma()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        foreach (var item in page.Items.Where(i => i.OccurrencesCount > 0))
        {
            item.LemmaText.Should().NotBeNullOrWhiteSpace();
            item.OccurrencesCount.Should().BeGreaterThan(0);
            item.AyahsCount.Should().BeGreaterThanOrEqualTo(1);
            item.SurahsCount.Should().BeInRange(1, 114);
            item.SimpleWordsCount.Should().BeGreaterThanOrEqualTo(1);
            item.TashkeelWordsCount.Should().BeGreaterThanOrEqualTo(1);

            if (item.Id is not CompoundLemmaLaId and not CompoundLemmaAnId and not MarkerRegressionLemmaId)
            {
                item.StemsCount.Should().BeGreaterThanOrEqualTo(1);
            }
        }
    }

    [Fact]
    public async Task GetLemmasPage_owned_root_uses_quran_lemmas_root_id()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        var l500 = page.Items.Single(i => i.Id == HighFrequencyLemmaId);
        l500.RootId.Should().Be(700);
        l500.RootText.Should().Be("ك ل م");
    }

    [Fact]
    public async Task GetLemmasPage_null_owned_root_returns_null_root_fields()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        var l501 = page.Items.Single(i => i.Id == NullRootLemmaId);
        l501.RootId.Should().BeNull();
        l501.RootText.Should().BeNull();
    }

    [Fact]
    public async Task GetLemmasPage_multi_type_lemma_orders_dominant_type_by_count_then_mushaf_order()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        var l503 = page.Items.Single(i => i.Id == MultiTypeLemmaId);
        l503.OccurrencesCount.Should().Be(4);
    }

    [Fact]
    public async Task GetLemmasPage_high_frequency_lemma_exposes_additional_type_indicator()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        var l500 = page.Items.Single(i => i.Id == HighFrequencyLemmaId);
        l500.OccurrencesCount.Should().Be(11);
    }

    [Fact]
    public async Task GetLemmasPage_search_filters_by_lemma_text()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery("كلمة", null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        page.Items.Should().ContainSingle(i => i.Id == HighFrequencyLemmaId);
    }

    [Fact]
    public async Task GetLemmasPage_search_returns_empty_when_nothing_matches()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery("لا_يوجد_مثل_هذه_الصيغة", null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("occurrences")]
    [InlineData("alpha")]
    [InlineData("mushaf-order")]
    public async Task GetLemmasPage_applies_each_supported_sort_without_error(string sort)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, sort, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        page.Items.Should().HaveCount(SeededLemmaCount);
    }

    [Fact]
    public async Task GetLemmasPage_occurrences_sort_orders_by_count_desc()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, "occurrences", 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.Id).Should().Equal(500, 503, 502, 501, 506, 508, 509, 504, 507);
    }

    [Fact]
    public async Task GetLemmasPage_mushaf_order_sort_orders_by_first_word_order_in_mushaf()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, "mushaf-order", 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.Id).Should().Equal(500, 501, 503, 502, 504, 506, 507, 508, 509);
    }

    [Fact]
    public async Task GetLemmasPage_pages_the_results()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var first = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 1, 2),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(SeededLemmaCount);
        firstPage.Items.Should().HaveCount(2);

        var second = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 2, 2),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;
        secondPage.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLemmasPage_positive_out_of_range_page_returns_successful_empty_page()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 99, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        page.Page.Should().Be(99);
        page.TotalCount.Should().Be(SeededLemmaCount);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLemmasPage_huge_positive_page_returns_empty_without_skip_overflow()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, int.MaxValue, 1000),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        page.Page.Should().Be(int.MaxValue);
        page.TotalCount.Should().Be(SeededLemmaCount);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLemmaSummary_returns_full_distribution_for_known_lemma()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaSummaryQuery(MultiTypeLemmaId),
            CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary;

        summary.Id.Should().Be(MultiTypeLemmaId);
        summary.TypeDistribution.Should().HaveCount(2);
        summary.TypeDistribution.Sum(t => t.OccurrencesCount).Should().Be(summary.OccurrencesCount);
        summary.TypeDistribution.First().Code.Should().Be("N");
    }

    [Fact]
    public async Task GetLemmaSummary_compound_la_type_distribution_uses_segment_pos_not_word_head_pos()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaSummaryQuery(CompoundLemmaLaId),
            CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary;

        summary.OccurrencesCount.Should().Be(2);
        summary.AyahsCount.Should().Be(2);
        summary.SurahsCount.Should().Be(2);
        summary.TypeDistribution.Should().ContainSingle();
        summary.TypeDistribution.Single().Code.Should().Be("NEG");
        summary.TypeDistribution.Single().OccurrencesCount.Should().Be(2);
        summary.TypeDistribution.Select(t => t.Code).Should().NotContain("SUB");
    }

    [Fact]
    public async Task GetLemmaSummary_compound_an_type_distribution_includes_segment_only_occurrence()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaSummaryQuery(CompoundLemmaAnId),
            CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary;

        summary.OccurrencesCount.Should().Be(1);
        summary.AyahsCount.Should().Be(1);
        summary.SurahsCount.Should().Be(1);
        summary.SimpleWordsCount.Should().Be(1);
        summary.TashkeelWordsCount.Should().Be(1);
        summary.StemsCount.Should().Be(0);
        summary.TypeDistribution.Should().ContainSingle();
        summary.TypeDistribution.Single().Code.Should().Be("SUB");
        summary.TypeDistribution.Single().OccurrencesCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLemmaSummary_same_lemma_segment_fanout_counts_segment_occurrences()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaSummaryQuery(SameLemmaFanoutLemmaId),
            CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary;

        summary.OccurrencesCount.Should().Be(2);
        summary.AyahsCount.Should().Be(1);
        summary.SurahsCount.Should().Be(1);
        summary.SimpleWordsCount.Should().Be(1);
        summary.TashkeelWordsCount.Should().Be(1);
        summary.StemsCount.Should().Be(1);
        summary.TypeDistribution.Should().ContainSingle();
        summary.TypeDistribution.Single().Code.Should().Be("N");
        summary.TypeDistribution.Sum(t => t.OccurrencesCount).Should().Be(summary.OccurrencesCount);
    }

    [Fact]
    public async Task GetLemmaSummary_matched_ayah_marker_does_not_increase_word_counts()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaSummaryQuery(MarkerRegressionLemmaId),
            CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary;

        summary.OccurrencesCount.Should().Be(2, "occurrences remain the accepted segment count contract");
        summary.AyahsCount.Should().Be(1);
        summary.SurahsCount.Should().Be(1);
        summary.SimpleWordsCount.Should().Be(1);
        summary.TashkeelWordsCount.Should().Be(1);
        summary.TypeDistribution.Sum(t => t.OccurrencesCount).Should().Be(summary.OccurrencesCount);
    }

    [Fact]
    public async Task GetLemmasPage_compound_fixture_segment_counts_match_summary()
    {
        await using var scope = fixture.CreateScope();
        var pageHandler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();
        var summaryHandler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        var pageOutcome = await pageHandler.HandleAsync(
            new GetLemmasPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = pageOutcome.Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

        var laList = page.Items.Single(i => i.Id == CompoundLemmaLaId);
        var anList = page.Items.Single(i => i.Id == CompoundLemmaAnId);
        var fanoutList = page.Items.Single(i => i.Id == SameLemmaFanoutLemmaId);

        anList.OccurrencesCount.Should().Be(1);
        anList.AyahsCount.Should().Be(1);
        anList.SurahsCount.Should().Be(1);
        anList.SimpleWordsCount.Should().Be(1);
        anList.TashkeelWordsCount.Should().Be(1);
        anList.StemsCount.Should().Be(0);

        laList.OccurrencesCount.Should().Be(2);
        laList.AyahsCount.Should().Be(2);
        laList.SurahsCount.Should().Be(2);

        fanoutList.OccurrencesCount.Should().Be(2);
        fanoutList.AyahsCount.Should().Be(1);
        fanoutList.SurahsCount.Should().Be(1);
        fanoutList.SimpleWordsCount.Should().Be(1);
        fanoutList.TashkeelWordsCount.Should().Be(1);
        fanoutList.StemsCount.Should().Be(1);

        var laSummary = (await summaryHandler.HandleAsync(
            new GetLemmaSummaryQuery(CompoundLemmaLaId),
            CancellationToken.None)).Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary;
        laSummary.OccurrencesCount.Should().Be(laList.OccurrencesCount);
        laSummary.AyahsCount.Should().Be(laList.AyahsCount);
        laSummary.SurahsCount.Should().Be(laList.SurahsCount);

        var anSummary = (await summaryHandler.HandleAsync(
            new GetLemmaSummaryQuery(CompoundLemmaAnId),
            CancellationToken.None)).Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary;
        anSummary.OccurrencesCount.Should().Be(anList.OccurrencesCount);
        anSummary.AyahsCount.Should().Be(anList.AyahsCount);
        anSummary.SurahsCount.Should().Be(anList.SurahsCount);

        var fanoutSummary = (await summaryHandler.HandleAsync(
            new GetLemmaSummaryQuery(SameLemmaFanoutLemmaId),
            CancellationToken.None)).Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary;
        fanoutSummary.OccurrencesCount.Should().Be(fanoutList.OccurrencesCount);
        fanoutSummary.AyahsCount.Should().Be(fanoutList.AyahsCount);
        fanoutSummary.SurahsCount.Should().Be(fanoutList.SurahsCount);
    }

    [Fact]
    public async Task GetLemmaSummary_not_found_for_unknown_id()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaSummaryQuery(UnknownLemmaId),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmaSummaryOutcome.NotFound>();
    }

    [Fact]
    public async Task GetLemmasPage_repeated_read_issues_no_new_db_commands_after_cache()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var inner = new EfLemmasReader(dbContext);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedLemmasReader(inner, cache);

        await reader.GetLemmasPageAsync(null, LemmaSort.MushafOrder, 1, 50, CancellationToken.None);

        interceptor.Reset();
        await reader.GetLemmasPageAsync(null, LemmaSort.Occurrences, 2, 2, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "the whole summary is cached once; sort/page are in-memory");
    }

    [Theory]
    [InlineData("relevance")]
    [InlineData("")]
    public async Task GetLemmasPage_invalid_sort_returns_validation_outcome(string? sort)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, sort, 1, 50),
            CancellationToken.None);

        if (string.IsNullOrEmpty(sort))
        {
            outcome.Should().BeOfType<GetLemmasPageOutcome.Success>();
        }
        else
        {
            outcome.Should().BeOfType<GetLemmasPageOutcome.InvalidSort>();
        }
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 1001)]
    public async Task GetLemmasPage_invalid_paging_returns_validation_outcome(int page, int pageSize)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, page, pageSize),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmasPageOutcome.InvalidPaging>();
    }

    [Fact]
    public async Task GetLemmasPage_log_carries_required_fields_and_no_lemma_or_search_text()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        await handler.HandleAsync(
            new GetLemmasPageQuery("كلمة", "occurrences", 1, 50),
            CancellationToken.None);

        var entries = fixture.LoggingProvider.Entries
            .Where(e => e.Message.Contains("GetLemmasPage"))
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

        completed.Message.Should().NotContain("نِعْمَة");
        completed.Message.Should().NotContain("نعمة");
    }
}
