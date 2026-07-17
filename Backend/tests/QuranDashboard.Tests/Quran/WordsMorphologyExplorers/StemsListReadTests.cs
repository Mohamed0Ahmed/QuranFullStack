using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemSummary;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class StemsListReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int SeededStemCount = 6;
    private const int HighFrequencyStemId = 600;
    private const int NullRelationStemId = 601;
    private const int MultiCandidateStemId = 602;
    private const int ExactTieStemId = 604;
    private const int SegmentFanoutStemId = 606;
    private const int UnknownStemId = 999_999;

    [Fact]
    public async Task GetStemsPage_returns_default_page_with_all_seeded_stems()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
        page.TotalCount.Should().Be(SeededStemCount);
        page.Items.Should().HaveCount(SeededStemCount);
    }

    [Fact]
    public async Task GetStemsPage_carries_all_counts_and_dominant_relationships_for_every_stem()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        foreach (var item in page.Items)
        {
            item.StemText.Should().NotBeNullOrWhiteSpace();

            item.OccurrencesCount.Should().BeGreaterThan(0);
            item.AyahsCount.Should().BeGreaterThanOrEqualTo(1);
            item.SurahsCount.Should().BeInRange(1, 114);
            item.SimpleWordsCount.Should().BeGreaterThanOrEqualTo(1);
            item.TashkeelWordsCount.Should().BeGreaterThanOrEqualTo(1);
        }
    }

    [Fact]
    public async Task GetStemsPage_high_frequency_stem_exposes_independent_dominant_lemma_and_root()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        var stem = page.Items.Single(i => i.Id == HighFrequencyStemId);
        stem.LemmaId.Should().Be(500);
        stem.RootId.Should().Be(700);
        stem.LemmaText.Should().Be("كَلِمَة");
        stem.RootText.Should().Be("ك ل م");
        stem.OccurrencesCount.Should().Be(11);
    }

    [Fact]
    public async Task GetStemsPage_multi_candidate_stem_orders_lemma_and_root_independently()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        var stem = page.Items.Single(i => i.Id == MultiCandidateStemId);
        stem.LemmaId.Should().Be(502);
        stem.RootId.Should().Be(701);
        stem.LemmaText.Should().Be("عِلْم");
        stem.RootText.Should().Be("ع ل م");
        stem.OccurrencesCount.Should().Be(4);
    }

    [Fact]
    public async Task GetStemsPage_null_relationship_stem_returns_null_relation_fields()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        var stem = page.Items.Single(i => i.Id == NullRelationStemId);
        stem.LemmaId.Should().BeNull();
        stem.LemmaText.Should().BeNull();
        stem.RootId.Should().BeNull();
        stem.RootText.Should().BeNull();
        stem.OccurrencesCount.Should().Be(1);
        stem.AyahsCount.Should().Be(1);
        stem.SurahsCount.Should().Be(1);
        stem.SimpleWordsCount.Should().Be(1);
        stem.TashkeelWordsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStemsPage_exact_tie_uses_earliest_mushaf_occurrence_for_dominant_type()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        var stem = page.Items.Single(i => i.Id == ExactTieStemId);
    }

    [Fact]
    public async Task GetStemsPage_search_filters_by_stem_text()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery("حكم", null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        page.Items.Should().ContainSingle(i => i.Id == ExactTieStemId);
    }

    [Theory]
    [InlineData("occurrences")]
    [InlineData("alpha")]
    [InlineData("mushaf-order")]
    public async Task GetStemsPage_applies_each_supported_sort_without_error(string sort)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, sort, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        page.Items.Should().HaveCount(SeededStemCount);
    }

    [Fact]
    public async Task GetStemsPage_occurrences_sort_orders_by_count_desc_then_mushaf_order()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, "occurrences", 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.Id).Should().Equal(600, 604, 602, 606, 605, 601);
    }

    [Fact]
    public async Task GetStemsPage_alpha_sort_orders_by_normalized_text_then_identity()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, "alpha", 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.Id).Should().Equal(604, 606, 602, 600, 601, 605);
    }

    // The exact reverse of the ascending pin above: the six seeded stems carry distinct normalized
    // texts, so no alpha tie engages and DESC simply mirrors the sequence.
    [Fact]
    public async Task GetStemsPage_alpha_descending_reverses_the_ascending_order()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, "alpha-desc", 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.Id).Should().Equal(605, 601, 600, 602, 606, 604);
    }

    // Stems allowlists neither `lemmas` nor `stems` — a column another explorer offers is still a 400
    // here — and mushaf-order is bare-only. This suite had no invalid-sort coverage before N8.
    [Theory]
    [InlineData("relevance")]
    [InlineData("bogus")]
    [InlineData("mushaf-order-asc")]
    [InlineData("mushaf-order-desc")]
    [InlineData("lemmas")]
    [InlineData("stems")]
    public async Task GetStemsPage_invalid_sort_returns_validation_outcome(string sort)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, sort, 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemsPageOutcome.InvalidSort>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetStemsPage_blank_sort_defaults_to_mushaf_order(string? sort)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, sort, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.Id).Should().Equal(600, 605, 604, 601, 602, 606);
    }

    // The acceptance bar: a legacy token and its canonical alias are ONE ordering.
    [Theory]
    [InlineData("occurrences", "occurrences-desc")]
    [InlineData("alpha", "alpha-asc")]
    public async Task GetStemsPage_legacy_token_and_its_alias_return_the_identical_sequence(string legacy, string alias)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var legacyOutcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, legacy, 1, 50),
            CancellationToken.None);
        var aliasOutcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, alias, 1, 50),
            CancellationToken.None);

        var legacyIds = legacyOutcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page.Items.Select(i => i.Id);
        var aliasIds = aliasOutcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page.Items.Select(i => i.Id);

        aliasIds.Should().Equal(legacyIds);
    }

    // Sorting is ORDER BY only: it may reorder the page but must never change the scope or the total.
    [Theory]
    [InlineData("mushaf-order")]
    [InlineData("alpha")]
    [InlineData("alpha-desc")]
    [InlineData("occurrences")]
    [InlineData("occurrences-asc")]
    [InlineData("ayahs")]
    [InlineData("ayahs-asc")]
    [InlineData("surahs")]
    [InlineData("surahs-asc")]
    [InlineData("simple")]
    [InlineData("simple-asc")]
    [InlineData("tashkeel")]
    [InlineData("tashkeel-asc")]
    public async Task GetStemsPage_every_sort_token_preserves_total_count_and_row_set(string sort)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, sort, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        page.TotalCount.Should().Be(SeededStemCount);
        page.Items.Select(i => i.Id).Should().BeEquivalentTo([600, 601, 602, 604, 605, 606]);
    }

    [Fact]
    public async Task GetStemsPage_mushaf_order_sort_orders_by_first_word_order_in_mushaf()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, "mushaf-order", 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.Id).Should().Equal(600, 605, 604, 601, 602, 606);
    }

    [Fact]
    public async Task GetStemsPage_pages_the_results()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var first = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 1, 2),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(SeededStemCount);
        firstPage.Items.Should().HaveCount(2);

        var second = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 2, 2),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;
        secondPage.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStemsPage_positive_out_of_range_page_returns_successful_empty_page()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 99, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        page.Page.Should().Be(99);
        page.TotalCount.Should().Be(SeededStemCount);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStemsPage_huge_positive_page_returns_empty_without_skip_overflow()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, int.MaxValue, 1000),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;

        page.Page.Should().Be(int.MaxValue);
        page.TotalCount.Should().Be(SeededStemCount);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStemSummary_returns_full_distribution_for_known_stem()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemSummaryQuery(ExactTieStemId),
            CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetStemSummaryOutcome.Success>().Subject.Summary;

        summary.Id.Should().Be(ExactTieStemId);
        summary.TypeDistribution.Should().HaveCount(2);
        summary.TypeDistribution.Sum(t => t.OccurrencesCount).Should().Be(summary.OccurrencesCount);
        summary.TypeDistribution[0].Code.Should().Be("N");
        summary.RootId.Should().BeNull();
    }

    [Fact]
    public async Task GetStemSummary_segment_fanout_stem_counts_segment_membership_and_type_distribution()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemSummaryQuery(SegmentFanoutStemId),
            CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetStemSummaryOutcome.Success>().Subject.Summary;

        summary.Id.Should().Be(SegmentFanoutStemId);
        summary.OccurrencesCount.Should().Be(4);
        summary.AyahsCount.Should().Be(3);
        summary.SurahsCount.Should().Be(2);
        summary.SimpleWordsCount.Should().Be(3);
        summary.TashkeelWordsCount.Should().Be(3);
        summary.TypeDistribution.Select(t => t.Code).Should().Equal("N", "SUB", "NEG");
        summary.TypeDistribution.Select(t => t.OccurrencesCount).Should().Equal(2, 1, 1);
    }

    [Fact]
    public async Task GetStemSummary_null_relationship_stem_returns_null_relation_fields()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemSummaryQuery(NullRelationStemId),
            CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetStemSummaryOutcome.Success>().Subject.Summary;

        summary.LemmaId.Should().BeNull();
        summary.LemmaText.Should().BeNull();
        summary.RootId.Should().BeNull();
        summary.RootText.Should().BeNull();
    }

    [Fact]
    public async Task GetStemSummary_not_found_for_unknown_id()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemSummaryQuery(UnknownStemId),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemSummaryOutcome.NotFound>();
    }

    [Fact]
    public async Task GetStemsPage_repeated_read_issues_no_new_db_commands_after_cache()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var inner = new EfStemsReader(dbContext);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedStemsReader(inner, cache);

        await reader.GetStemsPageAsync(null, StemSortSpec.Natural(StemSortColumn.MushafOrder), StemsCountFilter.None, StemsAssociationFilter.None, 1, 50, CancellationToken.None);

        interceptor.Reset();
        await reader.GetStemSummaryAsync(ExactTieStemId, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "the whole summary is cached once and reused for both catalogue and summary reads");
    }
}
