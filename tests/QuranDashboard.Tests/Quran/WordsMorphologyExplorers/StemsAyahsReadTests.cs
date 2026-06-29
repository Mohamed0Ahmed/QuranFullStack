using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemAyahs;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class StemsAyahsReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int ExactTieStemId = 604;
    private const int SegmentFanoutStemId = 606;
    private const int UnknownStemId = 999_999;

    [Fact]
    public async Task GetStemAyahs_returns_exact_matched_quran_word_ids_for_seeded_stem()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemAyahsQuery(ExactTieStemId, 1, 50, null),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetStemAyahsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(2);

        var byVerse = page.Items.ToDictionary(i => i.VerseKey);
        byVerse["1:2"].Words.Should().Contain(w => w.IsMatched);
        byVerse["1:2"].Words.Should().NotContain(w => w.TextUthmani == "MARKER_FIXTURE");
        byVerse["1:3"].Words.Should().Contain(w => w.IsMatched);
        byVerse["1:3"].Words.Should().NotContain(w => w.TextUthmani == "MARKER_FIXTURE");
    }

    [Fact]
    public async Task GetStemAyahs_pages_the_results()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemAyahsHandler>();

        var first = await handler.HandleAsync(
            new GetStemAyahsQuery(ExactTieStemId, 1, 1, null),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetStemAyahsOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(2);
        firstPage.Items.Select(i => i.VerseKey).Should().Equal("1:2");

        var second = await handler.HandleAsync(
            new GetStemAyahsQuery(ExactTieStemId, 2, 1, null),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetStemAyahsOutcome.Success>().Subject.Page;
        secondPage.Items.Select(i => i.VerseKey).Should().Equal("1:3");
    }

    [Fact]
    public async Task GetStemAyahs_positive_out_of_range_page_returns_successful_empty_page()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemAyahsQuery(ExactTieStemId, 99, 50, null),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemAyahsOutcome.Success>().Subject.Page;

        page.Page.Should().Be(99);
        page.TotalCount.Should().Be(2);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStemAyahs_huge_positive_page_returns_empty_without_skip_overflow()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemAyahsQuery(ExactTieStemId, int.MaxValue, 1000, null),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemAyahsOutcome.Success>().Subject.Page;

        page.Page.Should().Be(int.MaxValue);
        page.TotalCount.Should().Be(2);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStemAyahs_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemAyahsQuery(UnknownStemId, 1, 50, null),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemAyahsOutcome.NotFound>();
    }

    [Fact]
    public async Task GetStemAyahs_invalid_id_returns_validation_outcome()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemAyahsQuery(0, 1, 50, null),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemAyahsOutcome.InvalidId>();
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1001)]
    public async Task GetStemAyahs_invalid_paging_returns_validation_outcome(int page, int pageSize)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemAyahsQuery(ExactTieStemId, page, pageSize, null),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemAyahsOutcome.InvalidPaging>();
    }

    [Theory]
    [InlineData("N", "2:1")]
    [InlineData("SUB", "2:25")]
    [InlineData("NEG", "3:1")]
    public async Task GetStemAyahs_filters_by_segment_pos_without_affecting_other_ayahs(
        string typeCode,
        string expectedVerseKey)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemAyahsQuery(SegmentFanoutStemId, 1, 50, typeCode),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetStemAyahsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.Items[0].VerseKey.Should().Be(expectedVerseKey);
        page.Items[0].Words.Should().Contain(w => w.IsMatched);
        page.Items[0].Words.Should().NotContain(w => w.TextUthmani == "MARKER_FIXTURE");
    }

    [Fact]
    public async Task GetStemAyahs_unknown_type_returns_successful_empty_page()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemAyahsQuery(SegmentFanoutStemId, 1, 50, "XYZ"),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetStemAyahsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStemAyahs_uses_bounded_query_count_without_per_ayah_n_plus_one()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfStemsReader(dbContext);

        var page = await reader.GetStemAyahMatchesAsync(ExactTieStemId, 1, 1, null, CancellationToken.None);

        page.Should().NotBeNull();
        page!.Items.Should().HaveCount(1);
        interceptor.CommandCount.Should().BeLessThanOrEqualTo(6, "the ayah flow must stay bounded (no per-ayah N+1)");
    }

    [Fact]
    public async Task GetStemAyahs_repeated_read_issues_no_new_db_commands_after_cache()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var inner = new EfStemsReader(dbContext);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedStemsReader(inner, cache);

        await reader.GetStemAyahMatchesAsync(ExactTieStemId, 1, 50, null, CancellationToken.None);

        interceptor.Reset();
        await reader.GetStemAyahMatchesAsync(ExactTieStemId, 1, 50, null, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0);
    }
}
