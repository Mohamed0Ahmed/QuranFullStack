using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootAyahs;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Roots;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsRoots;

[Collection(nameof(RootsExplorerCollection))]
public sealed class RootsAyahsReadTests(RootsExplorerTestFixture fixture)
{

    private const int HighFrequencyRootId = 10;

    [Fact]
    public async Task GetRootAyahs_excludes_ayah_markers_and_keeps_page_number()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootAyahsQuery(HighFrequencyRootId, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetRootAyahsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(3);
        page.Items.Should().HaveCount(3);

        var byVerse = page.Items.ToDictionary(i => i.VerseKey);

        byVerse["1:1"].PageNumber.Should().Be(1);
        byVerse["1:1"].Words.Should().HaveCount(4);
        byVerse["1:1"].Words.Count(w => w.IsMatched).Should().Be(2);
        byVerse["1:1"].Words.Should().OnlyContain(w => w.TextUthmani != "۞");

        byVerse["1:3"].Words.Should().HaveCount(2);
        byVerse["1:3"].Words.Count(w => w.IsMatched).Should().Be(2);

        byVerse["2:25"].Words.Should().HaveCount(1);
        byVerse["2:25"].Words.Count(w => w.IsMatched).Should().Be(1);

        foreach (var item in page.Items)
        {
            item.Words.Should().NotBeEmpty();
            item.Words.Should().OnlyContain(w => !string.IsNullOrWhiteSpace(w.TextUthmani));
        }
    }

    [Fact]
    public async Task GetRootAyahs_pages_the_results()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootAyahsHandler>();

        var first = await handler.HandleAsync(
            new GetRootAyahsQuery(HighFrequencyRootId, 1, 2),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetRootAyahsOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(3);
        firstPage.Items.Should().HaveCount(2);
        firstPage.Items[0].VerseKey.Should().Be("1:1");

        var second = await handler.HandleAsync(
            new GetRootAyahsQuery(HighFrequencyRootId, 2, 2),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetRootAyahsOutcome.Success>().Subject.Page;
        secondPage.Items.Should().HaveCount(1);
        secondPage.Items[0].VerseKey.Should().Be("2:25");
    }

    [Fact]
    public async Task GetRootAyahs_unknown_root_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetRootAyahsQuery(999_999, 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetRootAyahsOutcome.NotFound>();
    }

    [Fact]
    public async Task GetRootAyahs_uses_bounded_query_count_without_per_ayah_n_plus_one()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfRootsReader(dbContext);

        await reader.GetRootAyahMatchesAsync(HighFrequencyRootId, 1, 2, CancellationToken.None);
        var firstLoadCount = interceptor.CommandCount;
        firstLoadCount.Should().BeInRange(1, 6, "ayah page load must stay bounded (no per-ayah N+1)");

        interceptor.Reset();
        await reader.GetRootAyahMatchesAsync(HighFrequencyRootId, 1, 2, CancellationToken.None);
        interceptor.CommandCount.Should().Be(firstLoadCount);
    }

    [Fact]
    public async Task GetRootAyahs_repeated_read_issues_no_new_db_commands_after_cache()
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

        await reader.GetRootAyahMatchesAsync(HighFrequencyRootId, 1, 50, CancellationToken.None);

        interceptor.Reset();
        await reader.GetRootAyahMatchesAsync(HighFrequencyRootId, 1, 50, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0);
    }
}
