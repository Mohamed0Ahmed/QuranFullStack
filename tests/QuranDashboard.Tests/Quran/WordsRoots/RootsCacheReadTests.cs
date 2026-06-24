using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Roots;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsRoots;

[Collection(nameof(RootsExplorerCollection))]
public sealed class RootsCacheReadTests(RootsExplorerTestFixture fixture)
{
    private const int RootId = 10;

    [Fact]
    public Task Ayahs_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetRootAyahMatchesAsync(RootId, 1, 50, ct));

    [Fact]
    public Task Words_simple_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetRootWordsAsync(RootId, RootWordKind.Simple, 1, 50, ct));

    [Fact]
    public Task Words_tashkeel_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetRootWordsAsync(RootId, RootWordKind.Tashkeel, 1, 50, ct));

    [Fact]
    public Task Words_simple_page_two_after_page_one_hits_grouped_cache() =>
        AssertSecondPageHitsGroupedWordsCache(
            (reader, ct) => reader.GetRootWordsAsync(RootId, RootWordKind.Simple, 1, 1, ct),
            (reader, ct) => reader.GetRootWordsAsync(RootId, RootWordKind.Simple, 2, 1, ct));

    [Fact]
    public Task MentionedSurahs_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetRootMentionedSurahsAsync(RootId, ct));

    [Fact]
    public Task MissingSurahs_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetRootMissingSurahsAsync(RootId, ct));

    [Fact]
    public Task Lemmas_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetRootLemmasAsync(RootId, ct));

    [Fact]
    public Task Stems_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetRootStemsAsync(RootId, ct));

    private async Task AssertSecondReadHitsCache(Func<CachedRootsReader, CancellationToken, Task> read)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedRootsReader(new EfRootsReader(dbContext), cache);

        await read(reader, CancellationToken.None);
        interceptor.CommandCount.Should().BeGreaterThan(0, "the first read must reach the database");

        interceptor.Reset();
        await read(reader, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "the repeat read must be served entirely from cache");
    }

    private async Task AssertSecondPageHitsGroupedWordsCache(
        Func<CachedRootsReader, CancellationToken, Task> firstPageRead,
        Func<CachedRootsReader, CancellationToken, Task> secondPageRead)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedRootsReader(new EfRootsReader(dbContext), cache);

        await firstPageRead(reader, CancellationToken.None);
        interceptor.CommandCount.Should().BeGreaterThan(0, "the first page must load the grouped whole from the database");

        interceptor.Reset();
        await secondPageRead(reader, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "the second page must slice the cached grouped whole without re-querying");
    }
}
