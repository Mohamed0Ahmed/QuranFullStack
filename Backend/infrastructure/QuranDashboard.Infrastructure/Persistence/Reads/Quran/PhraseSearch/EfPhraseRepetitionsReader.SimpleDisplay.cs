namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseRepetitionsReader
{
    private async Task<IReadOnlyDictionary<long, string>> LoadSimpleDisplayTextsAsync(
        IReadOnlyList<PhraseRepetitionListItemRow> variants,
        CancellationToken cancellationToken)
    {
        if (variants.Count == 0)
        {
            return new Dictionary<long, string>();
        }

        var firstWordIds = variants
            .Select(variant => variant.FirstQuranWordId)
            .Distinct()
            .ToList();
        var firstWords = await db.QuranWords
            .AsNoTracking()
            .Where(word => firstWordIds.Contains(word.Id) && !word.IsAyahMarker)
            .Select(word => new PhraseFirstWordRow(word.Id, word.AyahId, word.WordNumber))
            .ToListAsync(cancellationToken);
        if (firstWords.Count != firstWordIds.Count)
        {
            throw new InvalidDataException("Phrase repetition has no readable first source token.");
        }

        var ayahIds = firstWords.Select(word => word.AyahId).Distinct().ToList();
        var words = await db.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .OrderBy(word => word.AyahId)
            .ThenBy(word => word.WordNumber)
            .Select(word => new PhraseSimpleDisplayWordRow(
                word.AyahId,
                word.WordNumber,
                word.TextImlaeiSimple))
            .ToListAsync(cancellationToken);
        var firstWordsById = firstWords.ToDictionary(word => word.QuranWordId);
        var wordsByAyah = words
            .GroupBy(word => word.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var displays = new Dictionary<long, string>(variants.Count);

        foreach (var variant in variants)
        {
            var firstWord = firstWordsById[variant.FirstQuranWordId];
            var endWordNumber = checked((short)(firstWord.WordNumber + variant.WordCount - 1));
            var displayWords = wordsByAyah[firstWord.AyahId]
                .Where(word => word.WordNumber >= firstWord.WordNumber
                    && word.WordNumber <= endWordNumber)
                .Select(word => word.TextImlaeiSimple)
                .ToList();
            if (displayWords.Count != variant.WordCount || displayWords.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidDataException("Phrase repetition simple display text is incomplete.");
            }

            displays.Add(variant.VariantId, string.Join(' ', displayWords));
        }

        return displays;
    }

    private sealed record PhraseRepetitionListItemRow(
        long VariantId,
        string DisplayText,
        short WordCount,
        long OccurrenceCount,
        int AyahCount,
        short SurahCount,
        int FirstQuranWordId);

    private sealed record PhraseFirstWordRow(
        int QuranWordId,
        int AyahId,
        short WordNumber);

    private sealed record PhraseSimpleDisplayWordRow(
        int AyahId,
        short WordNumber,
        string TextImlaeiSimple);
}
