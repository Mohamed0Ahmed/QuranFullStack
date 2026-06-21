using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

/// <summary>
/// Caches the surah start-page catalog under a single key. The catalog is derived
/// from immutable Quran navigation data, so a cache-forever policy is correct
/// (see <see cref="MushafReaderCacheKeys"/> for the restart-after-import assumption).
/// </summary>
public sealed class CachedMushafSurahCatalogReader(IMushafSurahCatalogReader inner, IMemoryCache cache)
    : IMushafSurahCatalogReader
{
    public async Task<MushafSurahCatalogResponse> GetCatalogAsync(CancellationToken ct)
    {
        var key = MushafReaderCacheKeys.SurahCatalog;

        if (cache.TryGetValue(key, out MushafSurahCatalogResponse? cached))
        {
            return cached!;
        }

        var response = await inner.GetCatalogAsync(ct);
        cache.Set(key, response);

        return response;
    }
}
