using System.Globalization;

namespace QuranDashboard.Application.Abstractions.Linking;

public sealed record LinkingManualAyahWord(int WordNumber, string Location, bool IsAyahMarker);

public sealed record LinkingManualAyah(
    string VerseKey,
    int SurahNumber,
    int AyahNumber,
    int WordsCountReal,
    IReadOnlyList<LinkingManualAyahWord> Words);

public static class LinkingManualAyahCompleteness
{
    public const string ManualAyahsField = "manualAyahs";

    public static LinkingDescriptorViolation? Verify(string requestedVerseKey, LinkingManualAyah? ayah)
    {
        ArgumentNullException.ThrowIfNull(requestedVerseKey);

        if (ayah is null || !string.Equals(ayah.VerseKey, requestedVerseKey, StringComparison.Ordinal))
        {
            return Failure(requestedVerseKey);
        }

        var readableWords = ayah.Words
            .Where(word => !word.IsAyahMarker)
            .OrderBy(word => word.WordNumber)
            .ToList();

        if (ayah.WordsCountReal < 1 || readableWords.Count != ayah.WordsCountReal)
        {
            return Failure(requestedVerseKey);
        }

        var expectedPrefix = string.Create(
            CultureInfo.InvariantCulture,
            $"{ayah.SurahNumber}:{ayah.AyahNumber}:");
        var seenLocations = new HashSet<string>(readableWords.Count, StringComparer.Ordinal);

        for (var index = 0; index < readableWords.Count; index++)
        {
            var word = readableWords[index];

            if (word.WordNumber != index + 1
                || !word.Location.StartsWith(expectedPrefix, StringComparison.Ordinal)
                || !seenLocations.Add(word.Location))
            {
                return Failure(requestedVerseKey);
            }
        }

        return null;
    }

    private static LinkingDescriptorViolation Failure(string verseKey) =>
        new(LinkingDescriptorViolationCode.ManualAyahCompletenessFailed, ManualAyahsField, verseKey);
}
