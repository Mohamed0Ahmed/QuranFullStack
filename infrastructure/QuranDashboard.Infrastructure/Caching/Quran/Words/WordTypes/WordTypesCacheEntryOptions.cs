
namespace QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;

public static class WordTypesCacheEntryOptions
{
    public static MemoryCacheEntryOptions Tree() =>
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

    public static MemoryCacheEntryOptions PagedRows() =>
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

    public static MemoryCacheEntryOptions Detail() =>
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(15));
}
