using System.Collections.Concurrent;

namespace QuranDashboard.Infrastructure.Caching;

// Per-key gate held for the process lifetime: only use with a BOUNDED key space (finite lemma/stem
// catalogue), never caller-supplied keys, or add eviction.
internal static class CacheLoadGate
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.Ordinal);

    internal static async Task<T?> GetOrLoadAsync<T>(
        IMemoryCache cache,
        string key,
        Func<CancellationToken, Task<T?>> load,
        Func<MemoryCacheEntryOptions> entryOptions,
        CancellationToken cancellationToken)
        where T : class
    {
        if (cache.TryGetValue(key, out T? cached))
        {
            return cached;
        }

        var gate = Gates.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(key, out cached))
            {
                return cached;
            }

            var loaded = await load(cancellationToken);
            if (loaded is not null)
            {
                cache.Set(key, loaded, entryOptions());
            }

            return loaded;
        }
        finally
        {
            gate.Release();
        }
    }
}
