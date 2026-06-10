namespace QuranDashboard.Infrastructure.Files.Quran.Morphology;

internal static class MorphologySourceValidation
{
    internal static void ValidateCorpusCoverage(
        IReadOnlyList<AlignedCorpusWord> corpusWords,
        IReadOnlyDictionary<string, int> readableWordIdsByLocation)
    {
        var corpusLocations = corpusWords
            .Select(word => word.QpcLocation)
            .ToHashSet(StringComparer.Ordinal);

        var extraCorpusLocations = corpusLocations
            .Where(location => !readableWordIdsByLocation.ContainsKey(location))
            .OrderBy(location => location, StringComparer.Ordinal)
            .ToList();

        if (extraCorpusLocations.Count > 0)
        {
            throw new InvalidDataException(
                $"Aligned corpus contains locations that do not map to readable quran_words: {string.Join(", ", extraCorpusLocations)}.");
        }

        var missingReadableLocations = readableWordIdsByLocation.Keys
            .Where(location => !corpusLocations.Contains(location))
            .OrderBy(location => location, StringComparer.Ordinal)
            .ToList();

        if (missingReadableLocations.Count > 0)
        {
            throw new InvalidDataException(
                $"Aligned corpus is missing entries for readable quran_words locations: {string.Join(", ", missingReadableLocations)}.");
        }
    }
}
