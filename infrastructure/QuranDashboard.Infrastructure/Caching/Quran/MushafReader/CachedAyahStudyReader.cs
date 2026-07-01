using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

public sealed class CachedAyahStudyReader(IAyahStudyReader inner, IMemoryCache cache) : IAyahStudyReader
{
    public async Task<AyahStudyResponse?> GetAyahStudyAsync(
        string verseKey,
        string? tafsirSourceKey,
        string? translationSourceKey,
        string? fullI3rabSourceKey,
        CancellationToken ct)
    {
        var key = MushafReaderCacheKeys.AyahStudy(
            verseKey,
            tafsirSourceKey,
            translationSourceKey,
            fullI3rabSourceKey);

        if (cache.TryGetValue(key, out AyahStudyResponse? cached))
        {
            return cached;
        }

        var study = await inner.GetAyahStudyAsync(
            verseKey,
            tafsirSourceKey,
            translationSourceKey,
            fullI3rabSourceKey,
            ct);

        if (study is not null)
        {
            cache.Set(key, study);
        }

        return study;
    }
}
