using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

/// <summary>
/// Resolves each surah's Mushaf start page from the first ayah's <c>page_from</c>,
/// falling back to the minimum <c>page_from</c> for that surah when ayah 1 is absent
/// in the current database slice.
/// </summary>
public sealed class EfMushafSurahCatalogReader(QuranDashboardDbContext db) : IMushafSurahCatalogReader
{
    public async Task<MushafSurahCatalogResponse> GetCatalogAsync(CancellationToken ct)
    {
        var surahs = await db.QuranSurahs
            .AsNoTracking()
            .OrderBy(s => s.SurahNumber)
            .ToListAsync(ct);

        var firstAyahPages = await db.QuranAyahs
            .AsNoTracking()
            .Where(a => a.AyahNumber == 1)
            .ToDictionaryAsync(a => a.SurahNumber, a => (int)a.PageFrom, ct);

        // The min-page GROUP-BY fallback only matters for surahs whose ayah 1 is
        // missing from the current slice. In a complete DB that set is empty, so the
        // aggregate is only run for the (typically empty) gap rather than for every
        // surah on every catalog call.
        var surahNumbersMissingAyahOne = surahs
            .Select(s => s.SurahNumber)
            .Where(surahNumber => !firstAyahPages.ContainsKey(surahNumber))
            .ToList();

        var minAyahPages = surahNumbersMissingAyahOne.Count == 0
            ? new Dictionary<short, int>()
            : await db.QuranAyahs
                .AsNoTracking()
                .Where(a => surahNumbersMissingAyahOne.Contains(a.SurahNumber))
                .GroupBy(a => a.SurahNumber)
                .Select(g => new { SurahNumber = g.Key, MinPage = g.Min(a => a.PageFrom) })
                .ToDictionaryAsync(x => x.SurahNumber, x => (int)x.MinPage, ct);

        var items = new List<MushafSurahCatalogItem>(surahs.Count);
        foreach (var surah in surahs)
        {
            if (firstAyahPages.TryGetValue(surah.SurahNumber, out var firstPage))
            {
                items.Add(new MushafSurahCatalogItem(surah.SurahNumber, surah.NameArabic, firstPage));
                continue;
            }

            if (minAyahPages.TryGetValue(surah.SurahNumber, out var minPage))
            {
                items.Add(new MushafSurahCatalogItem(surah.SurahNumber, surah.NameArabic, minPage));
            }
        }

        return new MushafSurahCatalogResponse(items);
    }
}
