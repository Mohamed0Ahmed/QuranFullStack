using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

/// <summary>
/// Caches successful, non-null Mushaf page reads. Never caches missing pages.
/// </summary>
public sealed class CachedMushafPageReader(IMushafPageReader inner, IMemoryCache cache) : IMushafPageReader
{
    public async Task<MushafPageResponse?> GetPageAsync(int pageNumber, CancellationToken ct)
    {
        var key = MushafReaderCacheKeys.Page(pageNumber);

        if (cache.TryGetValue(key, out MushafPageResponse? cached))
        {
            return cached;
        }

        var page = await inner.GetPageAsync(pageNumber, ct);
        if (page is not null)
        {
            cache.Set(key, page);
        }

        return page;
    }
}
