using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed class PhraseSimilarityOccurrenceHydrator(QuranDashboardDbContext db)
{
    public async Task<IReadOnlyDictionary<long, PhraseSimilarityOccurrenceSeed>> LoadFirstAsync(
        Guid buildId,
        IReadOnlyList<long> variantIds,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return new Dictionary<long, PhraseSimilarityOccurrenceSeed>();
        }

        var occurrenceRows = await (
            from occurrence in db.QuranPhraseOccurrences.AsNoTracking()
            join variant in db.QuranPhraseVariants.AsNoTracking()
                on new { occurrence.BuildId, Id = occurrence.VariantId }
                equals new { variant.BuildId, variant.Id }
            join ayah in db.QuranAyahs.AsNoTracking()
                on occurrence.AyahId equals ayah.Id
            join surah in db.QuranSurahs.AsNoTracking()
                on ayah.SurahNumber equals surah.SurahNumber
            where occurrence.BuildId == buildId
                && variantIds.Contains(occurrence.VariantId)
                && occurrence.FirstQuranWordId == variant.FirstQuranWordId
            select new SimilarityOccurrenceRow(
                occurrence.VariantId,
                occurrence.Id,
                ayah.Id,
                ayah.VerseKey,
                ayah.SurahNumber,
                surah.NameArabic,
                ayah.AyahNumber,
                ayah.PageFrom,
                ayah.PageTo,
                occurrence.StartWordNumber,
                occurrence.EndWordNumber))
            .ToListAsync(cancellationToken);
        if (occurrenceRows.Count != variantIds.Distinct().Count())
        {
            throw new InvalidDataException("PhraseSearch similarity variants do not have one first occurrence each.");
        }

        var wordsByAyah = await LoadAyahWordsAsync(
            occurrenceRows.Select(row => row.AyahId).Distinct().ToList(),
            cancellationToken);

        return occurrenceRows.ToDictionary(
            row => row.VariantId,
            row => CreateSeed(row, wordsByAyah.GetValueOrDefault(row.AyahId, [])));
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<PhraseAyahWordDto>>> LoadAyahWordsAsync(
        IReadOnlyList<int> ayahIds,
        CancellationToken cancellationToken)
    {
        if (ayahIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<PhraseAyahWordDto>>();
        }

        var wordRows = await db.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .OrderBy(word => word.SurahNumber)
            .ThenBy(word => word.AyahNumber)
            .ThenBy(word => word.WordNumber)
            .Select(word => new SimilarityAyahWordRow(
                word.AyahId,
                word.Id,
                word.WordNumber,
                word.PageNumber,
                word.TextUthmani))
            .ToListAsync(cancellationToken);
        return wordRows
            .GroupBy(row => row.AyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PhraseAyahWordDto>)group
                    .Select(row => new PhraseAyahWordDto(
                        row.QuranWordId,
                        row.WordNumber,
                        row.PageNumber,
                        row.TextUthmani))
                    .ToList());
    }

    public PhraseSimilarityOccurrenceDto WithoutScore(PhraseSimilarityOccurrenceSeed occurrence) =>
        CreateOccurrence(occurrence, [], []);

    public PhraseSimilarityOccurrencePreviewDto ToPreview(PhraseSimilarityOccurrenceSeed occurrence)
    {
        var phraseWords = occurrence.Words
            .Where(word => word.WordNumber >= occurrence.StartWordNumber
                && word.WordNumber <= occurrence.EndWordNumber)
            .ToList();
        if (phraseWords.Count != occurrence.PhraseQuranWordIds.Count)
        {
            throw new InvalidDataException("PhraseSearch similarity preview is not a contiguous Quran window.");
        }

        return new PhraseSimilarityOccurrencePreviewDto(
            occurrence.OccurrenceId,
            occurrence.AyahId,
            occurrence.VerseKey,
            occurrence.SurahNumber,
            occurrence.SurahNameArabic,
            occurrence.AyahNumber,
            occurrence.PageFrom,
            occurrence.PageTo,
            occurrence.StartWordNumber,
            occurrence.EndWordNumber,
            phraseWords);
    }

    public PhraseSimilarityOccurrenceDto ApplyScore(
        PhraseSimilarityOccurrenceSeed occurrence,
        PhraseHammingScore score) => CreateOccurrence(
            occurrence,
            ResolvePositionWordIds(occurrence.PhraseQuranWordIds, score.MatchedPositions),
            ResolvePositionWordIds(occurrence.PhraseQuranWordIds, score.DifferingPositions));

    private static PhraseSimilarityOccurrenceSeed CreateSeed(
        SimilarityOccurrenceRow occurrence,
        IReadOnlyList<PhraseAyahWordDto> words)
    {
        var phraseWordIds = words
            .Where(word => word.WordNumber >= occurrence.StartWordNumber
                && word.WordNumber <= occurrence.EndWordNumber)
            .Select(word => word.QuranWordId)
            .ToList();
        if (phraseWordIds.Count != occurrence.EndWordNumber - occurrence.StartWordNumber + 1)
        {
            throw new InvalidDataException("PhraseSearch similarity occurrence is not a contiguous Quran window.");
        }

        return new PhraseSimilarityOccurrenceSeed(
            occurrence.OccurrenceId,
            occurrence.AyahId,
            occurrence.VerseKey,
            occurrence.SurahNumber,
            occurrence.SurahNameArabic,
            occurrence.AyahNumber,
            occurrence.PageFrom,
            occurrence.PageTo,
            occurrence.StartWordNumber,
            occurrence.EndWordNumber,
            words,
            phraseWordIds);
    }

    private static PhraseSimilarityOccurrenceDto CreateOccurrence(
        PhraseSimilarityOccurrenceSeed occurrence,
        IReadOnlyList<int> matchedWordIds,
        IReadOnlyList<int> differingWordIds) => new(
        occurrence.OccurrenceId,
        occurrence.AyahId,
        occurrence.VerseKey,
        occurrence.SurahNumber,
        occurrence.SurahNameArabic,
        occurrence.AyahNumber,
        occurrence.PageFrom,
        occurrence.PageTo,
        occurrence.StartWordNumber,
        occurrence.EndWordNumber,
        occurrence.Words,
        new PhraseSimilarityHighlightsDto(
            occurrence.PhraseQuranWordIds,
            matchedWordIds,
            differingWordIds));

    private static IReadOnlyList<int> ResolvePositionWordIds(
        IReadOnlyList<int> phraseWordIds,
        IReadOnlyList<short> positions)
    {
        var result = new List<int>(positions.Count);
        foreach (var position in positions)
        {
            if (position <= 0 || position > phraseWordIds.Count)
            {
                throw new InvalidDataException("PhraseSearch similarity position is outside its Quran window.");
            }

            result.Add(phraseWordIds[position - 1]);
        }

        return result;
    }

    private sealed record SimilarityOccurrenceRow(
        long VariantId,
        long OccurrenceId,
        int AyahId,
        string VerseKey,
        short SurahNumber,
        string SurahNameArabic,
        short AyahNumber,
        short PageFrom,
        short PageTo,
        short StartWordNumber,
        short EndWordNumber);

    private sealed record SimilarityAyahWordRow(
        int AyahId,
        int QuranWordId,
        short WordNumber,
        short PageNumber,
        string TextUthmani);
}

public sealed record PhraseSimilarityOccurrenceSeed(
    long OccurrenceId,
    int AyahId,
    string VerseKey,
    short SurahNumber,
    string SurahNameArabic,
    short AyahNumber,
    short PageFrom,
    short PageTo,
    short StartWordNumber,
    short EndWordNumber,
    IReadOnlyList<PhraseAyahWordDto> Words,
    IReadOnlyList<int> PhraseQuranWordIds);
