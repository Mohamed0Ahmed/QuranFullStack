using System.Collections.Concurrent;

namespace QuranDashboard.Infrastructure.Caching;

/// <summary>
/// Serializes concurrent cache-miss loads for the same key ("single flight"), so that a cold
/// identity is materialized once instead of once per concurrent caller.
/// </summary>
/// <remarks>
/// Plain cache-aside (<c>TryGetValue</c> / load / <c>Set</c>) is not atomic: concurrent cold
/// callers for the same key each run the loader, which defeats a "load the whole identity once"
/// cache. This gate closes that window for the callers that opt in; it does not change what is
/// cached, the cached value's lifetime, or the null-result contract (a null load is returned but
/// never cached, so a later call retries).
///
/// Gates are held per key for the process lifetime. The key space here is bounded by the finite
/// lemma/stem catalogue, so the retained <see cref="SemaphoreSlim"/> instances are bounded too;
/// do not reuse this gate for unbounded or caller-supplied key spaces without adding eviction.
///
/// Cancellation stays per-caller: a waiter that cancels leaves the in-flight load alone, and if
/// the caller holding the gate cancels, the next waiter re-runs the load rather than inheriting
/// the cancellation.
/// </remarks>
internal static class CacheLoadGate
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.Ordinal);

    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, or runs <paramref name="load"/> once
    /// across concurrent callers and caches a non-null result.
    /// </summary>
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
