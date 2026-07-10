namespace QuranDashboard.Infrastructure.Caching.Quran.Words;

internal static class UniqueWordsCacheEntryOptions
{
    private static readonly TimeSpan ListAbsoluteExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DetailSlidingExpiration = TimeSpan.FromMinutes(30);

    public static MemoryCacheEntryOptions List() =>
        new() { AbsoluteExpirationRelativeToNow = ListAbsoluteExpiration };

    public static MemoryCacheEntryOptions Detail() =>
        new() { SlidingExpiration = DetailSlidingExpiration };
}
