using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Roots;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsRoots;

/// <summary>
/// Cross-cutting (T072): cache-hit verification across every Roots detail
/// endpoint. Each detail read is exercised twice through <see cref="CachedRootsReader"/>
/// over its own fresh <see cref="MemoryCache"/>; the repeat read must issue zero
/// DB commands. Using a per-test cache instance also proves the decorator caches
/// through the injected <c>IMemoryCache</c> — no global/static cache
/// reconfiguration is involved.
/// </summary>
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
}
