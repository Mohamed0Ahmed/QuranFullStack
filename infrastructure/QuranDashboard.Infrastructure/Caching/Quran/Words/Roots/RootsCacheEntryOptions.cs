using Microsoft.Extensions.Caching.Memory;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Roots;

internal static class RootsCacheEntryOptions
{
    private static readonly TimeSpan DetailSlidingExpiration = TimeSpan.FromMinutes(30);

    public static MemoryCacheEntryOptions SummaryAll() => new();

    public static MemoryCacheEntryOptions GroupedWords() =>
        new() { SlidingExpiration = DetailSlidingExpiration };

    public static MemoryCacheEntryOptions PagedDetail() =>
        new() { SlidingExpiration = DetailSlidingExpiration };

    public static MemoryCacheEntryOptions WholeDetail() =>
        new() { SlidingExpiration = DetailSlidingExpiration };
}
