namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

internal static class MushafReaderCacheEntryOptions
{
    private static readonly TimeSpan DetailSlidingExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CatalogAbsoluteExpiration = TimeSpan.FromHours(12);

    public static MemoryCacheEntryOptions Detail() =>
        new() { SlidingExpiration = DetailSlidingExpiration };

    public static MemoryCacheEntryOptions Catalog() =>
        new() { AbsoluteExpirationRelativeToNow = CatalogAbsoluteExpiration };
}
