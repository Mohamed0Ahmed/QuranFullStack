using System.Collections.Concurrent;

namespace QuranDashboard.Infrastructure.Caching;

// Single-flight gate: serializes concurrent cache-miss loads for the same key so a cold identity is
// materialized once instead of once per concurrent caller. A null load is returned but never cached, so
// a later call retries. Gates are held per key for the process lifetime, so the key space must stay
// bounded (the finite lemma/stem catalogue); do not reuse this for unbounded or caller-supplied keys
// without adding eviction. Cancellation is per-caller: a cancelled waiter leaves the in-flight load
// alone, and if the gate holder cancels, the next waiter re-runs the load.
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
