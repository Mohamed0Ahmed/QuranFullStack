namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

/// <summary>
/// Shared ayah-word hydration core for the Words explorers' "ayah matches" reads (Roots, Lemmas,
/// Stems, WordTypes, and WordTypes' grouped root/stem/lemma detail — decision 5, DRY). All five
/// readers diverge on how they find their matched words, on their existence check, on their
/// page-ayahs meta query (some join <c>quran_surahs</c> for the Arabic surah name, some don't), and
/// on their final DTO shape — all of that stays in each caller. What they share byte-for-byte is
/// this: for a given page of ayahs, load every non-marker word ordered by Surah/Ayah/Word number,
/// group the words by ayah, and resolve each ayah's page number from its first readable word. This
/// type owns only that shared core.
/// </summary>
internal static class AyahWordHydration
{
    /// <summary>
    /// Hydrates the marker-filtered, Mushaf-ordered words for <paramref name="pageAyahs"/>, groups them
    /// by ayah, resolves each ayah's page number, and projects one <typeparamref name="TDto"/> per ayah
    /// via <paramref name="project"/> — preserving <paramref name="pageAyahs"/>' order.
    /// </summary>
    /// <typeparam name="TMeta">The caller's own page-ayahs meta row (identity plus whatever else it already joined).</typeparam>
    /// <typeparam name="TDto">The caller's final per-ayah DTO.</typeparam>
    /// <param name="db">The read-only DbContext.</param>
    /// <param name="pageAyahs">The caller's already-paged, already Surah/Ayah-ordered meta rows for this page.</param>
    /// <param name="ayahIdOf">Extracts the ayah id from a meta row.</param>
    /// <param name="project">
    /// Builds the final DTO for one ayah from its meta row, its hydrated words, and the resolved page
    /// number. The caller's own matched-word lookup (isMatched / matched positions / matched ids) and
    /// DTO shape live entirely in this closure.
    /// </param>
    internal static async Task<IReadOnlyList<TDto>> ProjectAyahMatchesAsync<TMeta, TDto>(
        QuranDashboardDbContext db,
        IReadOnlyList<TMeta> pageAyahs,
        Func<TMeta, int> ayahIdOf,
        Func<TMeta, IReadOnlyList<AyahWordRow>, short, TDto> project,
        CancellationToken cancellationToken)
    {
        var pageAyahIds = pageAyahs.Select(ayahIdOf).ToList();

        var wordsByAyah = await db.QuranWords
            .AsNoTracking()
            .Where(w => pageAyahIds.Contains(w.AyahId) && !w.IsAyahMarker)
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

    // First non-marker word's page number; falls back to the first word's, else 0. The hydration query
    // above already filters markers out of `words`, so in practice this only ever falls through to the
    // "no words hydrated for this ayah" case — kept explicit because it is the exact algorithm every
    // caller previously duplicated.
    private static short ResolveAyahPageNumber(IReadOnlyList<AyahWordRow> words)
    {
        var firstReadableWord = words.FirstOrDefault(w => !w.IsAyahMarker);
        if (firstReadableWord is not null)
        {
            return firstReadableWord.PageNumber;
        }

        return words.FirstOrDefault()?.PageNumber ?? 0;
    }

    /// <summary>One hydrated ayah word: identity, Mushaf position, display text, and marker flag.</summary>
    internal sealed record AyahWordRow(
        int AyahId,
        int QuranWordId,
        int WordNumber,
        short PageNumber,
        string TextUthmani,
        bool IsAyahMarker);
}
