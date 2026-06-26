using Microsoft.Extensions.Caching.Memory;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;

/// <summary>
/// Bounded cache entry option factories for the Stems Explorer. Mirrors Roots:
/// whole-summary entries have no expiration; detail entries use a 30-minute
/// sliding expiration. No global cache configuration is applied here.
/// </summary>
internal static class StemsCacheEntryOptions
{
    private static readonly TimeSpan DetailSlidingExpiration = TimeSpan.FromMinutes(30);

    public static MemoryCacheEntryOptions SummaryAll() => new();

    public static MemoryCacheEntryOptions GroupedWords() =>
        new() { SlidingExpiration = DetailSlidingExpiration };

    public static MemoryCacheEntryOptions PagedWords() =>
        new() { SlidingExpiration = DetailSlidingExpiration };

    public static MemoryCacheEntryOptions PagedDetail() =>
        new() { SlidingExpiration = DetailSlidingExpiration };

    public static MemoryCacheEntryOptions WholeDetail() =>
        new() { SlidingExpiration = DetailSlidingExpiration };
}
