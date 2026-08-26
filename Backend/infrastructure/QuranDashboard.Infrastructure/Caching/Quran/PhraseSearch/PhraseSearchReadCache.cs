namespace QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

public sealed class PhraseSearchReadCache : IDisposable
{
    private const long CacheSizeLimit = 256;
    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromMinutes(15);
    private readonly MemoryCache cache = new(new MemoryCacheOptions
    {
        SizeLimit = CacheSizeLimit,
    });

    public bool TryGet<T>(string key, out T value)
        where T : class
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            value = cached;
            return true;
        }

        value = null!;
        return false;
    }

    public void Set<T>(string key, T value, int weight = 1)
        where T : class => cache.Set(
            key,
            value,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AbsoluteExpiration,
                Size = Math.Clamp(weight, 1, 40),
            });

    public void Dispose() => cache.Dispose();
}
