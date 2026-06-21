using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

/// <summary>
/// Caches the study-source dimension catalog under a single key. Source rows are
/// rewritten only by imports, so a cache-forever policy is correct (see
/// <see cref="MushafReaderCacheKeys"/> for the restart-after-import assumption).
/// </summary>
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
        cache.Set(key, response);

        return response;
    }
}
