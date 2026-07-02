using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

public sealed class CachedAyahSimilaritiesReader(IAyahSimilaritiesReader inner, IMemoryCache cache)
    : IAyahSimilaritiesReader
{
    public async Task<SimilarAyahsResponse?> GetSimilarAyahsAsync(string verseKey, CancellationToken ct)
    {
        var key = MushafReaderCacheKeys.SimilarAyahs(verseKey);

        if (cache.TryGetValue(key, out SimilarAyahsResponse? cached))
        {
            return cached;
        }

        var response = await inner.GetSimilarAyahsAsync(verseKey, ct);

        if (response is not null)
        {
            cache.Set(key, response, MushafReaderCacheEntryOptions.Detail());
        }

        return response;
    }
}
