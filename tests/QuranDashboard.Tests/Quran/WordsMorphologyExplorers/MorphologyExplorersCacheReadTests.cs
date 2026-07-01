using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;
using QuranDashboard.Infrastructure.ServiceRegistration;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

/// <summary>
/// Consolidated Phase 11 (T116) cache + SQL command-count audit for Feature 016.
/// Every bounded cache entry must be hit on repeat reads (zero new SQL commands),
/// the catalogue must reuse the cached whole-summary list across search/sort/page
/// changes, no cache key may embed raw free-text search, and the Lemmas/Stems DI
/// must not register any global <see cref="IMemoryCache"/> configuration.
/// </summary>
[Collection(nameof(MorphologyExplorersCollection))]
public sealed class MorphologyExplorersCacheReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int LemmaId = 500;
    private const int StemId = 602;
    private const int SegmentFanoutStemId = 606;

    [Fact]
    public Task Lemma_ayahs_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetLemmaAyahMatchesAsync(LemmaId, 1, 50, null, ct));

    [Fact]
    public async Task Lemma_ayahs_type_filter_uses_separate_cache_entry()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedLemmasReader(new EfLemmasReader(dbContext), cache);

        await reader.GetLemmaAyahMatchesAsync(LemmaId, 1, 50, "N", CancellationToken.None);
        interceptor.CommandCount.Should().BeGreaterThan(0, "the first filtered read must reach the database");

        interceptor.Reset();
        await reader.GetLemmaAyahMatchesAsync(LemmaId, 1, 50, "N", CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "the repeat filtered read must be served entirely from cache");

        interceptor.Reset();
        await reader.GetLemmaAyahMatchesAsync(LemmaId, 1, 50, null, CancellationToken.None);
        interceptor.CommandCount.Should().BeGreaterThan(0, "all-filter and type-filter cache entries must stay separate");
    }

    [Fact]
    public Task Lemma_ayahs_distinct_pages_cache_independently() =>
        AssertSecondDistinctPageStillQueriesDb(
            (reader, ct) => reader.GetLemmaAyahMatchesAsync(LemmaId, 1, 50, null, ct),
            (reader, ct) => reader.GetLemmaAyahMatchesAsync(LemmaId, 2, 50, null, ct));

    [Theory]
    [InlineData(LemmaWordKind.Simple)]
    [InlineData(LemmaWordKind.Tashkeel)]
    public Task Lemma_words_repeat_read_hits_cache(LemmaWordKind kind) =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetLemmaWordsAsync(LemmaId, kind, 1, 50, ct));

    [Fact]
    public Task Lemma_words_out_of_range_pages_are_not_cached() =>
        AssertSecondOutOfRangePageStillQueriesDb(
            (db, cache, ct) => new CachedLemmasReader(new EfLemmasReader(db), cache)
                .GetLemmaWordsAsync(LemmaId, LemmaWordKind.Simple, 999, 50, ct));

    [Fact]
    public Task Lemma_ayahs_out_of_range_pages_are_not_cached() =>
        AssertSecondOutOfRangePageStillQueriesDb(
            (db, cache, ct) => new CachedLemmasReader(new EfLemmasReader(db), cache)
                .GetLemmaAyahMatchesAsync(LemmaId, 999, 50, null, ct));

    [Fact]
    public Task Lemma_mentioned_surahs_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetLemmaMentionedSurahsAsync(LemmaId, ct));

    [Fact]
    public Task Lemma_missing_surahs_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetLemmaMissingSurahsAsync(LemmaId, ct));

    [Fact]
    public Task Lemma_stems_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetLemmaStemsAsync(LemmaId, ct));

    [Fact]
    public async Task Lemma_catalogue_reuses_summary_all_cache_across_sort_and_page_changes()
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

        await reader.GetLemmasPageAsync(null, LemmaSort.MushafOrder, 1, 50, CancellationToken.None);
        var initialCommandCount = interceptor.CommandCount;
        initialCommandCount.Should().BeGreaterThan(0, "the first catalogue read must reach the database");

        interceptor.Reset();
        await reader.GetLemmasPageAsync("كلم", LemmaSort.Occurrences, 2, 10, CancellationToken.None);
        await reader.GetLemmasPageAsync(null, LemmaSort.Alpha, 3, 5, CancellationToken.None);
        await reader.GetLemmaSummaryAsync(LemmaId, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "search/sort/page/summary changes must slice the cached whole summary without re-querying");
    }

    [Fact]
    public Task Stem_ayahs_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetStemAyahMatchesAsync(StemId, 1, 50, null, ct));

    [Fact]
    public async Task Stem_ayahs_type_filter_uses_separate_cache_entry()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedStemsReader(new EfStemsReader(dbContext), cache);

        await reader.GetStemAyahMatchesAsync(SegmentFanoutStemId, 1, 50, "SUB", CancellationToken.None);
        interceptor.CommandCount.Should().BeGreaterThan(0, "the first filtered read must reach the database");

        interceptor.Reset();
        await reader.GetStemAyahMatchesAsync(SegmentFanoutStemId, 1, 50, "SUB", CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "the repeat filtered read must be served entirely from cache");

        interceptor.Reset();
        await reader.GetStemAyahMatchesAsync(SegmentFanoutStemId, 1, 50, null, CancellationToken.None);
        interceptor.CommandCount.Should().BeGreaterThan(0, "all-filter and type-filter cache entries must stay separate");
    }

    [Fact]
    public async Task Stem_ayahs_blank_type_uses_same_cache_entry_as_null_type()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedStemsReader(new EfStemsReader(dbContext), cache);

        await reader.GetStemAyahMatchesAsync(SegmentFanoutStemId, 1, 50, "   ", CancellationToken.None);
        interceptor.CommandCount.Should().BeGreaterThan(0, "the first blank filtered read must reach the database");

        interceptor.Reset();
        await reader.GetStemAyahMatchesAsync(SegmentFanoutStemId, 1, 50, null, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "blank and null type codes must share the same cache entry");
    }

    [Theory]
    [InlineData(StemWordKind.Simple)]
    [InlineData(StemWordKind.Tashkeel)]
    public Task Stem_words_repeat_read_hits_cache(StemWordKind kind) =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetStemWordsAsync(StemId, kind, 1, 50, ct));

    [Fact]
    public Task Stem_words_out_of_range_pages_are_not_cached() =>
        AssertSecondOutOfRangePageStillQueriesDb(
            (db, cache, ct) => new CachedStemsReader(new EfStemsReader(db), cache)
                .GetStemWordsAsync(StemId, StemWordKind.Simple, 999, 50, ct));

    [Fact]
    public Task Stem_ayahs_out_of_range_pages_are_not_cached() =>
        AssertSecondOutOfRangePageStillQueriesDb(
            (db, cache, ct) => new CachedStemsReader(new EfStemsReader(db), cache)
                .GetStemAyahMatchesAsync(StemId, 999, 50, null, ct));

    [Fact]
    public Task Stem_mentioned_surahs_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetStemMentionedSurahsAsync(StemId, ct));

    [Fact]
    public Task Stem_missing_surahs_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetStemMissingSurahsAsync(StemId, ct));

    [Fact]
    public Task Stem_lemmas_repeat_read_hits_cache() =>
        AssertSecondReadHitsCache((reader, ct) => reader.GetStemLemmasAsync(StemId, ct));

    [Fact]
    public async Task Stem_summary_repeat_read_hits_per_id_cache()
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

        var first = await reader.GetStemSummaryAsync(StemId, CancellationToken.None);
        first.Should().NotBeNull();
        interceptor.CommandCount.Should().BeGreaterThan(0);

        interceptor.Reset();
        var second = await reader.GetStemSummaryAsync(StemId, CancellationToken.None);
        second.Should().NotBeNull();
        interceptor.CommandCount.Should().Be(0, "the repeat summary read must be served entirely from cache");
    }

    [Fact]
    public async Task Stem_catalogue_reuses_summary_all_cache_across_sort_and_page_changes()
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

        await reader.GetStemsPageAsync(null, StemSort.MushafOrder, 1, 50, CancellationToken.None);
        interceptor.CommandCount.Should().BeGreaterThan(0);

        interceptor.Reset();
        await reader.GetStemsPageAsync("علم", StemSort.Occurrences, 2, 10, CancellationToken.None);
        await reader.GetStemsPageAsync(null, StemSort.Alpha, 3, 5, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "search/sort/page changes must slice the cached whole summary without re-querying");
    }

    [Fact]
    public void No_lemma_or_stem_cache_key_method_accepts_raw_search_text()
    {
        var forbidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "search", "searchText", "query", "q", "term", "rawSearch", "text", "filter"
        };

        var lemmaSearchParameters = typeof(LemmasCacheKeys)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(m => m.GetParameters())
            .Where(p => forbidden.Contains(p.Name!))
            .ToList();

        var stemSearchParameters = typeof(StemsCacheKeys)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(m => m.GetParameters())
            .Where(p => forbidden.Contains(p.Name!))
            .ToList();

        lemmaSearchParameters.Should().BeEmpty("no Lemmas cache key may embed raw search text");
        stemSearchParameters.Should().BeEmpty("no Stems cache key may embed raw search text");
    }

    [Fact]
    public void All_lemma_and_stem_cache_keys_use_bounded_namespaces()
    {
        var id = 42;
        var keys = new[]
        {
            LemmasCacheKeys.SummaryAll,
            LemmasCacheKeys.Summary(id),
            LemmasCacheKeys.WordsAll(id, LemmaWordKind.Simple),
            LemmasCacheKeys.Words(id, LemmaWordKind.Tashkeel, 2, 25),
            LemmasCacheKeys.Ayahs(id, 1, 50, null),
            LemmasCacheKeys.Surahs(id),
            LemmasCacheKeys.Missing(id),
            LemmasCacheKeys.Stems(id),
            StemsCacheKeys.SummaryAll,
            StemsCacheKeys.Summary(id),
            StemsCacheKeys.WordsAll(id, StemWordKind.Simple),
            StemsCacheKeys.Words(id, StemWordKind.Tashkeel, 2, 25),
            StemsCacheKeys.Ayahs(id, 1, 50, null),
            StemsCacheKeys.Ayahs(id, 1, 50, "SUB"),
            StemsCacheKeys.Surahs(id),
            StemsCacheKeys.Missing(id),
            StemsCacheKeys.Lemmas(id),
        };

        keys.Should().NotContainNulls();
        keys.Should().OnlyContain(key => key.StartsWith("lemmas:", StringComparison.Ordinal) || key.StartsWith("stems:", StringComparison.Ordinal),
            "every retained cache key must live under the bounded Lemmas/Stems namespace");
    }

    [Fact]
    public void AddLemmas_and_AddStems_register_no_global_memory_cache_configuration()
    {
        var services = new ServiceCollection();
        services.AddLemmas();
        services.AddStems();

        services.Should().NotContain(d => d.ServiceType == typeof(IMemoryCache),
            "AddLemmas/AddStems must reuse the shared IMemoryCache and never register a global one");
        services.Should().NotContain(d => d.ServiceType == typeof(IOptions<MemoryCacheOptions>),
            "AddLemmas/AddStems must not configure global MemoryCacheOptions");
    }

    private async Task AssertSecondReadHitsCache(Func<CachedLemmasReader, CancellationToken, Task> read)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedLemmasReader(new EfLemmasReader(dbContext), cache);

        await read(reader, CancellationToken.None);
        interceptor.CommandCount.Should().BeGreaterThan(0, "the first read must reach the database");

        interceptor.Reset();
        await read(reader, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "the repeat read must be served entirely from cache");
    }

    private async Task AssertSecondReadHitsCache(Func<CachedStemsReader, CancellationToken, Task> read)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedStemsReader(new EfStemsReader(dbContext), cache);

        await read(reader, CancellationToken.None);
        interceptor.CommandCount.Should().BeGreaterThan(0, "the first read must reach the database");

        interceptor.Reset();
        await read(reader, CancellationToken.None);
        interceptor.CommandCount.Should().Be(0, "the repeat read must be served entirely from cache");
    }

    private async Task AssertSecondDistinctPageStillQueriesDb(
        Func<CachedLemmasReader, CancellationToken, Task> firstPageRead,
        Func<CachedLemmasReader, CancellationToken, Task> secondPageRead)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedLemmasReader(new EfLemmasReader(dbContext), cache);

        await firstPageRead(reader, CancellationToken.None);
        interceptor.Reset();
        await secondPageRead(reader, CancellationToken.None);
        interceptor.CommandCount.Should().BeGreaterThan(0,
            "a cache miss for a different page must reach the database (paged detail cache)");
    }

    private async Task AssertSecondOutOfRangePageStillQueriesDb<TItem>(
        Func<QuranDashboardDbContext, IMemoryCache, CancellationToken, Task<PagedResult<TItem>?>> read)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var first = await read(dbContext, cache, CancellationToken.None);
        first.Should().NotBeNull();
        first!.Items.Should().BeEmpty();
        interceptor.CommandCount.Should().BeGreaterThan(0,
            "the first out-of-range page must still reach the database");

        interceptor.Reset();

        var second = await read(dbContext, cache, CancellationToken.None);
        second.Should().NotBeNull();
        second!.Items.Should().BeEmpty();
        interceptor.CommandCount.Should().BeGreaterThan(0,
            "empty paged detail results must not be cached");
    }
}
