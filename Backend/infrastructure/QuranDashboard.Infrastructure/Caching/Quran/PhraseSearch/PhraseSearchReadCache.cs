using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

public sealed class PhraseSearchReadCache : IDisposable
{
    private const long CacheSizeLimit = 256;
    private const int CapabilitiesBuildLimit = 2;
    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromMinutes(15);
    private readonly SemaphoreSlim capabilitiesGate = new(1, 1);
    private readonly Dictionary<Guid, PhraseSearchCapabilitiesResponse> capabilitiesByBuild = [];
    private readonly LinkedList<Guid> capabilityBuildOrder = [];
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

    public async Task<PhraseSearchCapabilitiesResponse> GetOrLoadCapabilitiesAsync(
        Guid buildId,
        Func<CancellationToken, Task<PhraseSearchCapabilitiesResponse>> load,
        CancellationToken cancellationToken)
    {
        await capabilitiesGate.WaitAsync(cancellationToken);
        try
        {
            if (capabilitiesByBuild.TryGetValue(buildId, out var cached))
            {
                TouchCapabilitiesBuild(buildId);
                return cached;
            }

            var loaded = await load(cancellationToken);
            capabilitiesByBuild[buildId] = loaded;
            capabilityBuildOrder.AddFirst(buildId);
            while (capabilityBuildOrder.Count > CapabilitiesBuildLimit)
            {
                var expiredBuildId = capabilityBuildOrder.Last!.Value;
                capabilityBuildOrder.RemoveLast();
                capabilitiesByBuild.Remove(expiredBuildId);
            }

            return loaded;
        }
        finally
        {
            capabilitiesGate.Release();
        }
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

    public void Dispose()
    {
        capabilitiesGate.Dispose();
        cache.Dispose();
    }

    private void TouchCapabilitiesBuild(Guid buildId)
    {
        capabilityBuildOrder.Remove(buildId);
        capabilityBuildOrder.AddFirst(buildId);
    }
}
