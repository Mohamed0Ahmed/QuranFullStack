using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Files.Quran.Morphology;

public sealed class MorphologyAssembler
{
    private static readonly string[] VerbTenseMarkers = ["PERF", "IMPF", "IMPV"];

    public MorphologySourceData Assemble(
        IReadOnlyList<AlignedCorpusWord> corpusWords,
        IReadOnlyDictionary<string, int> readableWordIdsByLocation,
        IReadOnlyDictionary<string, string> roots,
        IReadOnlyDictionary<string, string> lemmas,
        IReadOnlyDictionary<string, string> stems)
    {
        ArgumentNullException.ThrowIfNull(corpusWords);
        ArgumentNullException.ThrowIfNull(readableWordIdsByLocation);

        MorphologySourceValidation.ValidateCorpusCoverage(corpusWords, readableWordIdsByLocation);

        var corpusByLocation = corpusWords.ToDictionary(word => word.QpcLocation, StringComparer.Ordinal);
        var alignedWords = new List<AlignedWordDto>(readableWordIdsByLocation.Count);

        foreach (var (location, _) in readableWordIdsByLocation.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            alignedWords.Add(BuildAlignedWord(location, corpusByLocation[location]));
        }

        return new MorphologySourceData(
            alignedWords,
            roots,
            lemmas,
            stems,
            CharsetWarnings: []);
    }

    private static AlignedWordDto BuildAlignedWord(string location, AlignedCorpusWord corpusWord)
    {
        var segments = corpusWord.Segments
            .OrderBy(segment => segment.SegmentNumber)
            .Select(BuildAlignedSegment)
            .ToList();

        var stemSegment = segments.FirstOrDefault(segment =>
            string.Equals(segment.Kind, "STEM", StringComparison.Ordinal));

        var stemFeatures = stemSegment is null
            ? []
            : ParseFeatureTokens(stemSegment.FeaturesRaw);

        var headPos = stemSegment?.Pos ?? segments.FirstOrDefault()?.Pos ?? string.Empty;
        var isVerb = string.Equals(headPos, "V", StringComparison.Ordinal);

        return new AlignedWordDto(
            location,
            headPos,
            isVerb,
            isVerb ? MapVerbTense(stemFeatures) : null,
            isVerb ? MapVerbVoice(stemFeatures) : null,
            MapCaseFeature(stemFeatures),
            BuildFeaturesJson(stemSegment?.FeaturesRaw),
            segments);
    }

    private static AlignedSegmentDto BuildAlignedSegment(AlignedCorpusSegment segment) =>
        new(
            segment.SegmentNumber,
            segment.Kind,
            segment.Pos,
            segment.Form,
            FormArabicNormalized: null,
            RenderTier: null,
            MorphologyInvariants.RenderSource,
            segment.Root,
            segment.Lemma,
            segment.Features,
            BuildFeaturesJson(segment.Features));

    private static string? MapVerbTense(HashSet<string> features)
    {
        var tenseMarkerCount = VerbTenseMarkers.Count(features.Contains);
        if (tenseMarkerCount != 1)
        {
            return null;
        }

        if (features.Contains("PERF"))
        {
            return "past";
        }

        if (features.Contains("IMPF"))
        {
            return "present";
        }

        return "imperative";
    }

    private static string MapVerbVoice(HashSet<string> features) =>
        features.Contains("PASS") ? "passive" : "active";

    private static string? MapCaseFeature(HashSet<string> features)
    {
        if (features.Contains("NOM"))
        {
            return "nominative";
        }

        if (features.Contains("ACC"))
        {
            return "accusative";
        }

        if (features.Contains("GEN"))
        {
            return "genitive";
        }

        return null;
    }

    private static HashSet<string> ParseFeatureTokens(string? featuresRaw)
    {
        if (string.IsNullOrWhiteSpace(featuresRaw))
        {
            return [];
        }

        return featuresRaw
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string? BuildFeaturesJson(string? featuresRaw)
    {
        var tokens = ParseFeatureTokens(featuresRaw);
        return tokens.Count == 0
            ? null
            : JsonSerializer.Serialize(tokens.OrderBy(token => token, StringComparer.Ordinal));
    }
}
