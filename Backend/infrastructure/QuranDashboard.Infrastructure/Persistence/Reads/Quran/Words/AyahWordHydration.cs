namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

internal static class AyahWordHydration
{
    internal static async Task<IReadOnlyList<TDto>> ProjectAyahMatchesAsync<TMeta, TDto>(
        QuranDashboardDbContext db,
        IReadOnlyList<TMeta> pageAyahs,
        Func<TMeta, int> ayahIdOf,
        Func<TMeta, IReadOnlyList<AyahWordRow>, short, TDto> project,
        CancellationToken cancellationToken,
        bool includeAyahMarkers = false)
    {
        var pageAyahIds = pageAyahs.Select(ayahIdOf).ToList();

        var wordsQuery = db.QuranWords
            .AsNoTracking()
            .Where(w => pageAyahIds.Contains(w.AyahId));

        if (!includeAyahMarkers)
        {
            wordsQuery = wordsQuery.Where(w => !w.IsAyahMarker);
        }

        var wordsByAyah = await wordsQuery
            .OrderBy(w => w.SurahNumber)
            .ThenBy(w => w.AyahNumber)
            .ThenBy(w => w.WordNumber)
            .Select(w => new AyahWordRow(
                w.AyahId,
                w.Id,
                w.WordNumber,
                w.PageNumber,
                w.TextUthmani,
                w.IsAyahMarker))
            .ToListAsync(cancellationToken);

        var wordsGrouped = wordsByAyah
            .GroupBy(w => w.AyahId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return pageAyahs
            .Select(meta =>
            {
                var words = wordsGrouped.GetValueOrDefault(ayahIdOf(meta), []);
                var pageNumber = ResolveAyahPageNumber(words);
                return project(meta, words, pageNumber);
            })
            .ToList();
    }

    private static short ResolveAyahPageNumber(IReadOnlyList<AyahWordRow> words)
    {
        var firstReadableWord = words.FirstOrDefault(w => !w.IsAyahMarker);
        if (firstReadableWord is not null)
        {
            return firstReadableWord.PageNumber;
        }

        return words.FirstOrDefault()?.PageNumber ?? 0;
    }

    internal sealed record AyahWordRow(
        int AyahId,
        int QuranWordId,
        int WordNumber,
        short PageNumber,
        string TextUthmani,
        bool IsAyahMarker);
}
