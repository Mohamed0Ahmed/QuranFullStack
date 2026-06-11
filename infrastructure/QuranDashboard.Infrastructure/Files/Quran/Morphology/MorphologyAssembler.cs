using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Files.Quran.Morphology;

public sealed class MorphologyAssembler
{
    private static readonly string[] VerbTenseMarkers = ["PERF", "IMPF", "IMPV"];

    // The controlled POS vocabulary that head_pos / segment pos reference via FK. Resolving POS codes
    // in memory (before the COPY) lets an unknown code fail the MORPH-POS-RESOLVES gate with a report
    // instead of crashing the binary COPY on the quran_pos_tags foreign key.
    private static readonly HashSet<string> KnownPosCodes =
        PosTagSeed.GetAll().Select(tag => tag.Code).ToHashSet(StringComparer.Ordinal);

    private readonly SegmentArabicRenderer renderer;

    public MorphologyAssembler(SegmentArabicRenderer renderer)
    {
        this.renderer = renderer;
    }

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
        var orderedLocations = readableWordIdsByLocation
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToList();

        var charsetWarnings = new List<string>();
        var unknownPosCodes = new SortedSet<string>(StringComparer.Ordinal);
        var reviewForms = new HashSet<string>(StringComparer.Ordinal);
        var multiwordForms = new HashSet<string>(StringComparer.Ordinal);
        var emptyFormLocations = new List<string>();
        var agreementMatches = 0;
        var alignedWords = new List<AlignedWordDto>(orderedLocations.Count);
        var rootIndex = new Dictionary<string, DimensionEntry>(StringComparer.Ordinal);
        var lemmaIndex = new Dictionary<string, DimensionEntry>(StringComparer.Ordinal);
        var stemIndex = new Dictionary<string, DimensionEntry>(StringComparer.Ordinal);
        var rootLemmaMap = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var lemmaRootLinks = new Dictionary<string, (int RootId, int WordOrder)>(StringComparer.Ordinal);
        var nextDimId = 1;

        foreach (var (location, wordId) in orderedLocations)
        {
            var corpusWord = corpusByLocation[location];
            var segments = BuildAlignedSegments(corpusWord, charsetWarnings);
            CollectUnknownPosCodes(segments, unknownPosCodes);

            if (WholeWordRender(segments) == corpusWord.QpcUthmani)
            {
                agreementMatches++;
            }

            CollectRenderLists(location, segments, reviewForms, multiwordForms, emptyFormLocations);

            var stemSegment = segments.FirstOrDefault(s =>
                string.Equals(s.Kind, "STEM", StringComparison.Ordinal));

            var stemFeatures = stemSegment is null
                ? []
                : ParseFeatureTokens(stemSegment.FeaturesRaw);

            var headPos = stemSegment?.Pos ?? segments.FirstOrDefault()?.Pos ?? string.Empty;
            var isVerb = string.Equals(headPos, "V", StringComparison.Ordinal);

            var corpusRoot = stemSegment?.RootBuckwalter;
            var corpusLemma = stemSegment?.LemmaBuckwalter;

            string? qulRoot = null;
            string? qulLemma = null;
            string? qulStem = null;

            if (roots.TryGetValue(location, out var rv)) qulRoot = rv;
            if (lemmas.TryGetValue(location, out var lv)) qulLemma = lv;
            if (stems.TryGetValue(location, out var sv)) qulStem = sv;

            int? rootId = null;
            int? lemmaId = null;
            int? stemId = null;

            if (!string.IsNullOrWhiteSpace(qulRoot))
            {
                if (!rootIndex.TryGetValue(qulRoot, out var rootEntry))
                {
                    rootEntry = new DimensionEntry(nextDimId++, wordId);
                    rootIndex[qulRoot] = rootEntry;
                    rootLemmaMap[qulRoot] = [];
                }

                rootEntry.AddWord(wordId);
                rootId = rootEntry.Id;

                if (!string.IsNullOrWhiteSpace(corpusRoot))
                {
                    rootEntry.AddBuckwalter(corpusRoot);
                }
            }

            if (!string.IsNullOrWhiteSpace(qulLemma))
            {
                if (!lemmaIndex.TryGetValue(qulLemma, out var lemmaEntry))
                {
                    lemmaEntry = new DimensionEntry(nextDimId++, wordId);
                    lemmaIndex[qulLemma] = lemmaEntry;
                }

                lemmaEntry.AddWord(wordId);
                lemmaId = lemmaEntry.Id;

                if (!string.IsNullOrWhiteSpace(corpusLemma))
                {
                    lemmaEntry.AddBuckwalter(corpusLemma);
                }

                if (rootId.HasValue && !string.IsNullOrWhiteSpace(qulRoot))
                {
                    var lemmaSet = rootLemmaMap[qulRoot];
                    lemmaSet.Add(qulLemma);
                }

                // Link the lemma to its dominant (earliest, by mushaf order) co-occurring root.
                if (rootId.HasValue &&
                    (!lemmaRootLinks.TryGetValue(qulLemma, out var existingLink) || wordId < existingLink.WordOrder))
                {
                    lemmaRootLinks[qulLemma] = (rootId.Value, wordId);
                }
            }

            if (!string.IsNullOrWhiteSpace(qulStem))
            {
                if (!stemIndex.TryGetValue(qulStem, out var stemEntry))
                {
                    stemEntry = new DimensionEntry(nextDimId++, wordId);
                    stemIndex[qulStem] = stemEntry;
                }

                stemEntry.AddWord(wordId);
                stemId = stemEntry.Id;
            }

            alignedWords.Add(new AlignedWordDto(
                location,
                headPos,
                isVerb,
                isVerb ? MapVerbTense(stemFeatures) : null,
                isVerb ? MapVerbVoice(stemFeatures) : null,
                MapCaseFeature(stemFeatures),
                BuildFeaturesJson(stemSegment?.FeaturesRaw),
                segments,
                rootId,
                lemmaId,
                stemId));
        }

        var resolvedRoots = BuildResolvedRoots(rootIndex, rootLemmaMap);
        var resolvedLemmas = BuildResolvedLemmas(lemmaIndex, lemmaRootLinks);
        var resolvedStems = BuildResolvedStems(stemIndex);

        return new MorphologySourceData(
            alignedWords,
            roots,
            lemmas,
            stems,
            resolvedRoots,
            resolvedLemmas,
            resolvedStems,
            charsetWarnings,
            unknownPosCodes.ToList(),
            new MorphologyRenderStats(
                agreementMatches,
                alignedWords.Count,
                reviewForms.OrderBy(form => form, StringComparer.Ordinal).ToList(),
                multiwordForms.OrderBy(form => form, StringComparer.Ordinal).ToList(),
                emptyFormLocations));
    }

    private List<AlignedSegmentDto> BuildAlignedSegments(
        AlignedCorpusWord corpusWord, List<string> charsetWarnings)
    {
        var segments = new List<AlignedSegmentDto>();

        foreach (var segment in corpusWord.Segments.OrderBy(s => s.SegmentNumber))
        {
            var (arabic, tier) = renderer.Render(segment.Form);

            if (!string.IsNullOrEmpty(segment.Form))
            {
                var unmapped = renderer.CollectUnmappedCharacters(segment.Form);
                if (unmapped.Count > 0)
                {
                    charsetWarnings.Add(
                        $"Segment {segment.Kind}#{segment.SegmentNumber} form='{segment.Form}' " +
                        $"unmapped chars: {string.Join(", ", unmapped.Select(c => $"'{c}' (U+{((int)c):X4})"))}");
                }
            }

            segments.Add(new AlignedSegmentDto(
                segment.SegmentNumber,
                segment.Kind,
                segment.Pos,
                segment.Form,
                arabic,
                tier,
                MorphologyInvariants.RenderSource,
                segment.Root,
                segment.Lemma,
                segment.Features,
                BuildFeaturesJson(segment.Features)));
        }

        return segments;
    }

    private static void CollectUnknownPosCodes(
        IReadOnlyList<AlignedSegmentDto> segments, SortedSet<string> unknownPosCodes)
    {
        foreach (var segment in segments)
        {
            if (!KnownPosCodes.Contains(segment.Pos))
            {
                unknownPosCodes.Add(segment.Pos);
            }
        }
    }

    private static string WholeWordRender(IReadOnlyList<AlignedSegmentDto> segments) =>
        string.Concat(segments.Select(segment => segment.FormArabicNormalized ?? string.Empty));

    private static void CollectRenderLists(
        string location,
        IReadOnlyList<AlignedSegmentDto> segments,
        HashSet<string> reviewForms,
        HashSet<string> multiwordForms,
        List<string> emptyFormLocations)
    {
        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment.FormBuckwalter))
            {
                emptyFormLocations.Add($"{location}:{segment.SegmentNumber}");
            }
            else if (string.Equals(segment.RenderTier, "review", StringComparison.Ordinal))
            {
                reviewForms.Add(segment.FormBuckwalter);
            }
            else if (string.Equals(segment.RenderTier, "multiword", StringComparison.Ordinal))
            {
                multiwordForms.Add(segment.FormBuckwalter);
            }
        }
    }

    private static List<ResolvedRootDto> BuildResolvedRoots(
        Dictionary<string, DimensionEntry> rootIndex,
        Dictionary<string, HashSet<string>> rootLemmaMap)
    {
        var result = new List<ResolvedRootDto>(rootIndex.Count);

        foreach (var (rootText, entry) in rootIndex.OrderBy(e => e.Value.FirstWordOrder))
        {
            var distinctLemmas = rootLemmaMap.TryGetValue(rootText, out var set) ? set.Count : 0;
            result.Add(new ResolvedRootDto(
                entry.Id,
                rootText,
                entry.Buckwalter,
                entry.WordsCount,
                (short)distinctLemmas,
                entry.FirstWordOrder));
        }

        return result;
    }

    private static List<ResolvedLemmaDto> BuildResolvedLemmas(
        Dictionary<string, DimensionEntry> lemmaIndex,
        Dictionary<string, (int RootId, int WordOrder)> lemmaRootLinks)
    {
        var result = new List<ResolvedLemmaDto>(lemmaIndex.Count);

        foreach (var (lemmaText, entry) in lemmaIndex.OrderBy(e => e.Value.FirstWordOrder))
        {
            int? rootId = lemmaRootLinks.TryGetValue(lemmaText, out var link) ? link.RootId : null;

            result.Add(new ResolvedLemmaDto(
                entry.Id,
                lemmaText,
                entry.Buckwalter,
                rootId,
                entry.WordsCount,
                entry.FirstWordOrder));
        }

        return result;
    }

    private static List<ResolvedStemDto> BuildResolvedStems(
        Dictionary<string, DimensionEntry> stemIndex)
    {
        var result = new List<ResolvedStemDto>(stemIndex.Count);

        foreach (var (stemText, entry) in stemIndex.OrderBy(e => e.Value.FirstWordOrder))
        {
            result.Add(new ResolvedStemDto(
                entry.Id,
                stemText,
                entry.WordsCount,
                entry.FirstWordOrder));
        }

        return result;
    }

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

        // The real QAC corpus delimits feature tokens with '|' (e.g. "STEM|POS:V|IMPF|ROOT:Ebd");
        // the synthetic fixtures use spaces. Split on both so the bare grammatical flags
        // (PERF/IMPF/IMPV, PASS, NOM/ACC/GEN) resolve regardless of source format. Prefixed tokens
        // such as "POS:V"/"ROOT:Ebd" simply don't match the bare flags and are ignored.
        return featuresRaw
            .Split(['|', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string? BuildFeaturesJson(string? featuresRaw)
    {
        var tokens = ParseFeatureTokens(featuresRaw);
        return tokens.Count == 0
            ? null
            : JsonSerializer.Serialize(tokens.OrderBy(token => token, StringComparer.Ordinal));
    }

    private sealed class DimensionEntry(int id, int firstWordOrder)
    {
        public int Id { get; } = id;
        public int FirstWordOrder { get; private set; } = firstWordOrder;
        public int WordsCount { get; private set; }
        public string? Buckwalter { get; private set; }

        public void AddWord(int wordOrder)
        {
            WordsCount++;
            if (wordOrder < FirstWordOrder)
            {
                FirstWordOrder = wordOrder;
            }
        }

        public void AddBuckwalter(string buckwalter)
        {
            Buckwalter ??= buckwalter;
        }
    }
}
