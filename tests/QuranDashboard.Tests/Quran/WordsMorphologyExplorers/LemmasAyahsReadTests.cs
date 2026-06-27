using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaAyahs;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class LemmasAyahsReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int HighFrequencyLemmaId = 500;
    private const int UnknownLemmaId = 999_999;

    [Fact]
    public async Task GetLemmaAyahs_returns_exact_matched_quran_word_ids_for_seeded_lemma()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaAyahsQuery(HighFrequencyLemmaId, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetLemmaAyahsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(7);
        page.Items.Should().HaveCount(7);

        var byVerse = page.Items.ToDictionary(i => i.VerseKey);
        byVerse["1:1"].Words.Should().HaveCount(4);
        byVerse["1:1"].Words.Should().OnlyContain(w => w.IsMatched);
        byVerse["1:1"].Words.Select(w => w.TextUthmani).Should().Contain("كَلِمَة");
        byVerse["1:1"].Words.Select(w => w.TextUthmani).Should().NotContain("ۚ");
        byVerse["3:8"].Words.Should().HaveCount(3);
        byVerse["3:8"].Words.Should().ContainSingle(w => !w.IsMatched);
        byVerse["3:8"].Words.Should().Contain(w => w.TextUthmani == "عَلِمَ" && !w.IsMatched);
    }

    [Fact]
    public async Task GetLemmaAyahs_type_code_filters_ayahs_and_highlights_selected_type_words()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaAyahsQuery(HighFrequencyLemmaId, 1, 50, "V"),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetLemmaAyahsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();

        var ayah = page.Items.Single();
        ayah.VerseKey.Should().Be("1:2");
        ayah.Words.Should().Contain(w => w.IsMatched);
        ayah.Words.Should().Contain(w => !w.IsMatched);
        ayah.Words.Select(w => w.TextUthmani).Should().NotContain("ۚ");
    }

    [Fact]
    public async Task GetLemmaAyahs_type_code_pages_filtered_results()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        var first = await handler.HandleAsync(
            new GetLemmaAyahsQuery(HighFrequencyLemmaId, 1, 2, "N"),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetLemmaAyahsOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(6);
        firstPage.Items.Select(i => i.VerseKey).Should().Equal("1:1", "1:3");

        var second = await handler.HandleAsync(
            new GetLemmaAyahsQuery(HighFrequencyLemmaId, 2, 2, "N"),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetLemmaAyahsOutcome.Success>().Subject.Page;
        secondPage.Items.Select(i => i.VerseKey).Should().Equal("2:1", "2:25");
    }

    [Fact]
    public async Task GetLemmaAyahs_unknown_type_code_returns_successful_empty_page()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaAyahsQuery(HighFrequencyLemmaId, 1, 50, "ZZZ"),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmaAyahsOutcome.Success>().Subject.Page;

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLemmaAyahs_pages_the_results()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        var first = await handler.HandleAsync(
            new GetLemmaAyahsQuery(HighFrequencyLemmaId, 1, 2),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetLemmaAyahsOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(7);
        firstPage.Items.Select(i => i.VerseKey).Should().Equal("1:1", "1:2");

        var second = await handler.HandleAsync(
            new GetLemmaAyahsQuery(HighFrequencyLemmaId, 2, 2),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetLemmaAyahsOutcome.Success>().Subject.Page;
        secondPage.Items.Select(i => i.VerseKey).Should().Equal("1:3", "2:1");
    }

    [Fact]
    public async Task GetLemmaAyahs_positive_out_of_range_page_returns_successful_empty_page()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaAyahsQuery(HighFrequencyLemmaId, 99, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmaAyahsOutcome.Success>().Subject.Page;

        page.Page.Should().Be(99);
        page.TotalCount.Should().Be(7);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLemmaAyahs_huge_positive_page_returns_empty_without_skip_overflow()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaAyahsQuery(HighFrequencyLemmaId, int.MaxValue, 1000),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmaAyahsOutcome.Success>().Subject.Page;

        page.Page.Should().Be(int.MaxValue);
        page.TotalCount.Should().Be(7);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLemmaAyahs_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaAyahsQuery(UnknownLemmaId, 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmaAyahsOutcome.NotFound>();
    }

    [Fact]
    public async Task GetLemmaAyahs_invalid_id_returns_validation_outcome()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaAyahsQuery(0, 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmaAyahsOutcome.InvalidId>();
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1001)]
    public async Task GetLemmaAyahs_invalid_paging_returns_validation_outcome(int page, int pageSize)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaAyahsQuery(HighFrequencyLemmaId, page, pageSize),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmaAyahsOutcome.InvalidPaging>();
    }

    [Fact]
    public async Task GetLemmaAyahs_uses_bounded_query_count_without_per_ayah_n_plus_one()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfLemmasReader(dbContext);

        var page = await reader.GetLemmaAyahMatchesAsync(HighFrequencyLemmaId, 1, 2, null, CancellationToken.None);

        page.Should().NotBeNull();
        page!.Items.Should().HaveCount(2);
        interceptor.CommandCount.Should().BeLessThanOrEqualTo(6, "the ayah flow must stay bounded (no per-ayah N+1)");
    }

    [Fact]
    public async Task GetLemmaAyahs_repeated_read_issues_no_new_db_commands_after_cache()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var inner = new EfLemmasReader(dbContext);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedLemmasReader(inner, cache);

        await reader.GetLemmaAyahMatchesAsync(HighFrequencyLemmaId, 1, 50, null, CancellationToken.None);

        interceptor.Reset();
        await reader.GetLemmaAyahMatchesAsync(HighFrequencyLemmaId, 1, 50, null, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0);
    }
}
