using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

public sealed class CachedMushafStudySourceCatalogReader(IMushafStudySourceCatalogReader inner, IMemoryCache cache)
    : IMushafStudySourceCatalogReader
{
    public async Task<MushafStudySourceCatalogResponse> GetCatalogAsync(CancellationToken ct)
    {
        var key = MushafReaderCacheKeys.StudySourceCatalog;

        if (cache.TryGetValue(key, out MushafStudySourceCatalogResponse? cached))
        {
            return cached!;
        }

        var response = await inner.GetCatalogAsync(ct);
        cache.Set(key, response, MushafReaderCacheEntryOptions.Catalog());

        return response;
    }
}
