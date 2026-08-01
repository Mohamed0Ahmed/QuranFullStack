using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

// One indivisible entry for the whole snapshot: any root-affecting write moves every row's xmin, so a
// per-section or live-vs-archive split would be wrong, not merely finer. No expiration — eviction is the
// generation stamp, which changes atomically with the write's bump. No CacheLoadGate: its keys are held
// for the process lifetime and it cannot express "present but stale", which is the only question here.
internal sealed class CachedAbwabTreeReader(
    EfAbwabTreeReader inner,
    IMemoryCache cache,
    AbwabCacheGeneration generations) : IAbwabTreeReader
{
    private const string Key = "abwab:tree";

    private readonly EfAbwabTreeReader _inner = inner;
    private readonly IMemoryCache _cache = cache;
    private readonly AbwabCacheGeneration _generations = generations;

    public async Task<AbwabTreeDto> GetTreeAsync(CancellationToken cancellationToken)
    {
        // Captured BEFORE the load, not after: a write committing mid-load bumps past this value, so the
        // entry is stored already-stale and the next read reloads. The failure direction is an extra
        // query, never a stale hit.
        var generation = _generations.TreeGeneration();

        if (_cache.TryGetValue(Key, out StampedTree? cached) && cached is not null && cached.Generation == generation)
        {
            return cached.Tree;
        }

        var tree = await _inner.GetTreeAsync(cancellationToken);
        _cache.Set(Key, new StampedTree(generation, tree));
        return tree;
    }

    private sealed record StampedTree(long Generation, AbwabTreeDto Tree);
}
