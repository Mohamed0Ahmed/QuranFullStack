using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

internal static class LinkingAyahHydration
{
    internal static async Task<IReadOnlyList<LinkingAyahMetaRow>> LoadByIdsAsync(
        QuranDashboardDbContext db,
        IReadOnlyList<int> ayahIds,
        CancellationToken cancellationToken) =>
        await OrderedAyahs(db.QuranAyahs.AsNoTracking().Where(ayah => ayahIds.Contains(ayah.Id)), db)
            .ToListAsync(cancellationToken);

    internal static async Task<IReadOnlyList<LinkingAyahMetaRow>> LoadByVerseKeysAsync(
        QuranDashboardDbContext db,
        IReadOnlyList<string> verseKeys,
        CancellationToken cancellationToken) =>
        await OrderedAyahs(db.QuranAyahs.AsNoTracking().Where(ayah => verseKeys.Contains(ayah.VerseKey)), db)
            .ToListAsync(cancellationToken);

    internal static async Task<IReadOnlyList<LinkingResolvedAyahDto>> ProjectAsync(
        QuranDashboardDbContext db,
        IReadOnlyList<LinkingAyahMetaRow> orderedAyahs,
        IReadOnlyDictionary<int, IReadOnlyList<int>> matchedWordIdsByAyah,
        bool includeAyahMarkers,
        CancellationToken cancellationToken) =>
        await AyahWordHydration.ProjectAyahMatchesAsync(
            db,
            orderedAyahs,
            ayah => ayah.AyahId,
            (ayah, words, _) => new LinkingResolvedAyahDto(
                ayah.AyahId,
                ayah.VerseKey,
                ayah.SurahNumber,
                ayah.AyahNumber,
                ayah.SurahNameArabic,
                ayah.PageFrom,
                ayah.PageTo,
                matchedWordIdsByAyah.GetValueOrDefault(ayah.AyahId, []),
                [
                    .. words.Select(word => new LinkingResolvedWordDto(
                        word.QuranWordId,
                        word.WordNumber,
                        word.TextUthmani,
                        word.IsAyahMarker))
                ]),
            cancellationToken,
            includeAyahMarkers);

    private static IQueryable<LinkingAyahMetaRow> OrderedAyahs(
        IQueryable<Domain.Quran.Ayahs.Ayah> ayahs,
        QuranDashboardDbContext db) =>
        from ayah in ayahs
        join surah in db.QuranSurahs.AsNoTracking() on ayah.SurahNumber equals surah.SurahNumber
        orderby ayah.SurahNumber, ayah.AyahNumber
        select new LinkingAyahMetaRow(
            ayah.Id,
            ayah.VerseKey,
            ayah.SurahNumber,
            ayah.AyahNumber,
            surah.NameArabic,
            ayah.PageFrom,
            ayah.PageTo,
            ayah.WordsCountReal);

    internal sealed record LinkingAyahMetaRow(
        int AyahId,
        string VerseKey,
        int SurahNumber,
        int AyahNumber,
        string SurahNameArabic,
        short PageFrom,
        short PageTo,
        int WordsCountReal);
}
