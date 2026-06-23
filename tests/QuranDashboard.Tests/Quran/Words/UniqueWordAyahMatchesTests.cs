using Microsoft.EntityFrameworkCore;
using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordAyahs;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Tests.Quran.Words;

/// <summary>
/// US3 ayah-match drill-down: paged distinct ayahs, exact matched IDs,
/// multiple matches per ayah, marker exclusion, and batched read shape.
/// </summary>
[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordAyahMatchesTests(UniqueWordsTestFixture fixture)
{
    [Fact]
    public async Task GetAyahMatches_returns_paged_distinct_ayahs_with_all_words()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordAyahsQuery("tashkeel", 2003, 1, 20),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordAyahsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(i => i.Words.Count > 0);
        page.Items.Should().OnlyContain(i => i.MatchedQuranWordIds.Count > 0);
        page.Items.Should().OnlyContain(i => i.PageNumber > 0);
    }

    [Fact]
    public async Task GetAyahMatches_returns_mushaf_page_from_first_readable_word()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordAyahsQuery("tashkeel", 2003, 1, 20),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordAyahsOutcome.Success>().Subject.Page;
        var ayah225 = page.Items.Single(i => i.VerseKey == "2:25");
        var ayah12 = page.Items.Single(i => i.VerseKey == "1:2");

        ayah225.PageNumber.Should().Be(5);
        ayah12.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetAyahMatches_includes_all_matching_ids_in_one_ayah()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordAyahsQuery("tashkeel", 2003, 1, 20),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordAyahsOutcome.Success>().Subject.Page;
        var ayah225 = page.Items.Single(i => i.VerseKey == "2:25");

        ayah225.MatchedQuranWordIds.Should().BeEquivalentTo([2003, 20032]);
        ayah225.MatchedQuranWordIds.Should().NotContain(25999);
    }

    [Fact]
    public async Task GetAyahMatches_excludes_ayah_markers_from_matched_ids()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordAyahsQuery("tashkeel", 2003, 1, 20),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordAyahsOutcome.Success>().Subject.Page;
        var ayah225 = page.Items.Single(i => i.VerseKey == "2:25");
        var marker = ayah225.Words.SingleOrDefault(w => w.IsAyahMarker);

        marker.Should().NotBeNull();
        ayah225.MatchedQuranWordIds.Should().NotContain(marker!.QuranWordId);
    }

    [Fact]
    public async Task GetAyahMatches_pages_distinct_ayahs_not_individual_word_rows()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordAyahsHandler>();

        var page1 = await handler.HandleAsync(
            new GetUniqueWordAyahsQuery("tashkeel", 2003, 1, 1),
            CancellationToken.None);
        var page2 = await handler.HandleAsync(
            new GetUniqueWordAyahsQuery("tashkeel", 2003, 2, 1),
            CancellationToken.None);

        var first = page1.Should().BeOfType<GetUniqueWordAyahsOutcome.Success>().Subject.Page;
        var second = page2.Should().BeOfType<GetUniqueWordAyahsOutcome.Success>().Subject.Page;

        first.TotalCount.Should().Be(2);
        first.Items.Should().HaveCount(1);
        second.Items.Should().HaveCount(1);
        first.Items[0].AyahId.Should().NotBe(second.Items[0].AyahId);
    }

    [Fact]
    public async Task GetAyahMatches_returns_full_ayah_word_lists_for_batched_highlight_rendering()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordAyahsQuery("tashkeel", 2003, 1, 20),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordAyahsOutcome.Success>().Subject.Page;
        var ayah225 = page.Items.Single(i => i.VerseKey == "2:25");

        ayah225.Words.Select(w => w.WordNumber).Should().BeInAscendingOrder();
        ayah225.Words.Should().Contain(w => w.QuranWordId == 2003);
        ayah225.Words.Should().Contain(w => w.IsAyahMarker);
    }

    [Fact]
    public async Task GetAyahMatches_uses_bounded_batched_queries_not_per_ayah_n_plus_one()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfUniqueWordsReader(dbContext);

        interceptor.Reset();
        var page = await reader.GetAyahMatchesAsync(
            UniqueWordKind.Tashkeel,
            2003,
            page: 1,
            pageSize: 2,
            CancellationToken.None);

        page.Should().NotBeNull();
        page!.Items.Should().HaveCount(2);
        interceptor.CommandCount.Should().BeLessThanOrEqualTo(
            6,
            "the current batched flow is header, count, paged ayahs, matched IDs, ayah metadata, and all page words; per-ayah word queries would exceed this bound");
    }
}
