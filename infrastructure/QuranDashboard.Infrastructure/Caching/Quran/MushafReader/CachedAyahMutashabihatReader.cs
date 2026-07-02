using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

public sealed class CachedAyahMutashabihatReader(IAyahMutashabihatReader inner, IMemoryCache cache)
    : IAyahMutashabihatReader
{
    public async Task<AyahMutashabihatResponse?> GetAyahMutashabihatAsync(string verseKey, CancellationToken ct)
    {
        var key = MushafReaderCacheKeys.AyahMutashabihat(verseKey);

        if (cache.TryGetValue(key, out AyahMutashabihatResponse? cached))
        {
            return cached;
        }

        var response = await inner.GetAyahMutashabihatAsync(verseKey, ct);

        if (response is not null)
        {
            cache.Set(key, response, MushafReaderCacheEntryOptions.Detail());
        }

        return response;
    }
}
