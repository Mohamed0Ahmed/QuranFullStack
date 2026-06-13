using System.Globalization;
using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

namespace QuranDashboard.Infrastructure.Files.Quran.Mutashabihat;

public sealed class MutashabihatAssembler
{
    private static readonly Regex VerseKeyPattern = new(@"^\d+:\d+$", RegexOptions.Compiled);

    public MutashabihatSourceData AssembleGroups(
        PhrasesReadResult phrases,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey)
    {
        ArgumentNullException.ThrowIfNull(phrases);
        ArgumentNullException.ThrowIfNull(ayahIdsByVerseKey);

        var groups = new List<PhraseGroupDto>(phrases.Groups.Count);

        foreach (var parsedGroup in phrases.Groups)
        {
            ValidateVerseKeyFormat(parsedGroup.Source.Key);

            var representativeAyahId = ResolveAyahId(
                parsedGroup.Source.Key, ayahIdsByVerseKey, parsedGroup.SourceGroupId, "source.key");

            var occurrences = BuildOccurrences(
                parsedGroup,
                ayahIdsByVerseKey,
                representativeAyahId,
                parsedGroup.Source);

            var distinctAyahIds = occurrences.Select(occurrence => occurrence.AyahId).Distinct().ToList();
            var distinctSurahCount = CountDistinctSurahs(parsedGroup.OccurrencesByVerseKey);

            groups.Add(new PhraseGroupDto(
                parsedGroup.SourceGroupId,
                representativeAyahId,
                parsedGroup.Source.From,
                parsedGroup.Source.To,
                (short)occurrences.Count,
                (short)distinctAyahIds.Count,
                (short)distinctSurahCount,
                parsedGroup.RawSourceCountsJson,
                occurrences));
        }

        return new MutashabihatSourceData(groups, Links: []);
    }

    public IReadOnlyList<SimilarLinkDto> AssembleLinks(
        IReadOnlyList<ParsedSimilarSource> similarSources,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey)
    {
        ArgumentNullException.ThrowIfNull(similarSources);
        ArgumentNullException.ThrowIfNull(ayahIdsByVerseKey);

        var links = new List<SimilarLinkDto>();

        foreach (var source in similarSources)
        {
            ValidateVerseKeyFormat(source.SourceVerseKey);
            var sourceAyahId = ResolveSimilarAyahId(
                source.SourceVerseKey, ayahIdsByVerseKey, "similar source verse_key");

            foreach (var item in source.Links)
            {
                ValidateVerseKeyFormat(item.MatchedAyahKey);
                var targetAyahId = ResolveSimilarAyahId(
                    item.MatchedAyahKey, ayahIdsByVerseKey, "similar matched_ayah_key");

                links.Add(new SimilarLinkDto(
                    sourceAyahId,
                    targetAyahId,
                    item.Score,
                    item.Coverage,
                    item.MatchedWordsCount,
                    item.MatchWordsJson));
            }
        }

        return links;
    }

    private static List<OccurrenceDto> BuildOccurrences(
        ParsedPhraseGroup parsedGroup,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        int representativeAyahId,
        PhraseSourceBlock source)
    {
        var uniqueOccurrences = new Dictionary<(int AyahId, short From, short To), OccurrenceDto>();

        foreach (var (verseKey, ranges) in parsedGroup.OccurrencesByVerseKey)
        {
            ValidateVerseKeyFormat(verseKey);
            var ayahId = ResolveAyahId(
                verseKey, ayahIdsByVerseKey, parsedGroup.SourceGroupId, "occurrence verse_key");

            foreach (var range in ranges)
            {
                var key = (ayahId, range.From, range.To);
                if (uniqueOccurrences.ContainsKey(key))
                {
                    continue;
                }

                var isRepresentative = ayahId == representativeAyahId
                    && range.From == source.From
                    && range.To == source.To
                    && string.Equals(verseKey, source.Key, StringComparison.Ordinal);

                uniqueOccurrences[key] = new OccurrenceDto(
                    ayahId,
                    range.From,
                    range.To,
                    isRepresentative);
            }
        }

        return uniqueOccurrences.Values.ToList();
    }

    private static short CountDistinctSurahs(
        IReadOnlyDictionary<string, IReadOnlyList<WordRange>> occurrencesByVerseKey)
    {
        var distinctSurahs = new HashSet<short>();

        foreach (var verseKey in occurrencesByVerseKey.Keys)
        {
            distinctSurahs.Add(ParseSurahNumber(verseKey));
        }

        return (short)distinctSurahs.Count;
    }

    private static short ParseSurahNumber(string verseKey) =>
        short.Parse(verseKey.Split(':')[0], CultureInfo.InvariantCulture);

    private static int ResolveAyahId(
        string verseKey,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        int sourceGroupId,
        string context)
    {
        if (!ayahIdsByVerseKey.TryGetValue(verseKey, out var ayahId))
        {
            throw new InvalidDataException(
                $"Unresolved verse_key '{verseKey}' for group {sourceGroupId} ({context}).");
        }

        return ayahId;
    }

    private static int ResolveSimilarAyahId(
        string verseKey,
        IReadOnlyDictionary<string, int> ayahIdsByVerseKey,
        string context)
    {
        if (!ayahIdsByVerseKey.TryGetValue(verseKey, out var ayahId))
        {
            throw new InvalidDataException(
                $"Unresolved verse_key '{verseKey}' for {context}.");
        }

        return ayahId;
    }

    private static void ValidateVerseKeyFormat(string verseKey)
    {
        if (!VerseKeyPattern.IsMatch(verseKey))
        {
            throw new InvalidDataException($"Invalid verse_key format '{verseKey}'.");
        }
    }
}
