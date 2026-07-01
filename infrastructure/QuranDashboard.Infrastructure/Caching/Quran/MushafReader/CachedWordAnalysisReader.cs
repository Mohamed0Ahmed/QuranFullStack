using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

public sealed class CachedWordAnalysisReader(IWordAnalysisReader inner, IMemoryCache cache) : IWordAnalysisReader
{
    public async Task<WordAnalysisOutcome> GetWordAnalysisAsync(string wordLocation, CancellationToken ct)
    {
        var key = MushafReaderCacheKeys.WordAnalysis(wordLocation);

        if (cache.TryGetValue(key, out WordAnalysisOutcome? cached))
        {
            return cached!;
        }

        var outcome = await inner.GetWordAnalysisAsync(wordLocation, ct);

        if (outcome is WordAnalysisOutcome.Found)
        {
            cache.Set(key, outcome);
        }

        return outcome;
    }
}
