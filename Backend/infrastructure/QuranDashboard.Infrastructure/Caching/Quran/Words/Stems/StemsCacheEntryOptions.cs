
namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;

internal static class StemsCacheEntryOptions
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
