using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

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
