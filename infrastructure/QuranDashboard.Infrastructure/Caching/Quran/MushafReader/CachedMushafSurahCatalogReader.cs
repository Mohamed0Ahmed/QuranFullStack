using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

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
