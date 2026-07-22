using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

public sealed class MorphologyAssembler
{
    private static readonly string[] VerbTenseMarkers = ["PERF", "IMPF", "IMPV"];

    private static readonly HashSet<string> KnownPosCodes =
        PosTagSeed.GetAll().Select(tag => tag.Code).ToHashSet(StringComparer.Ordinal);

    // Curated homograph disambiguation for multi-STEM segments; anything not listed fails closed via
    // SEG-LEMMA-ID-NO-FANOUT (no silent lowest-id guess).
    private static readonly IReadOnlyDictionary<(string Pos, string Buckwalter), string> CuratedLemmaDisambiguation =
        new Dictionary<(string Pos, string Buckwalter), string>
        {
            [("ACC", ">an~")] = "أَنّ",
        };

    private static readonly IReadOnlyDictionary<string, string> EmptyStemCorrections =
        new Dictionary<string, string>(StringComparer.Ordinal);

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
        IReadOnlyDictionary<string, string> stems,
        IReadOnlyDictionary<string, string>? secondaryStemCorrections = null)
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
        var segmentDimensionResult = ResolveSegmentDimensions(
            alignedWords,
            resolvedRoots,
            resolvedLemmas,
            resolvedStems,
            secondaryStemCorrections ?? EmptyStemCorrections);

        return new MorphologySourceData(
            segmentDimensionResult.Words,
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
                emptyFormLocations),
            segmentDimensionResult.Issues);
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
                null,
                null,
                null,
                segment.Features,
                BuildFeaturesJson(segment.Features)));
        }

        return segments;
    }

    private static SegmentDimensionResolutionResult ResolveSegmentDimensions(
        IReadOnlyList<AlignedWordDto> words,
        IReadOnlyList<ResolvedRootDto> roots,
        IReadOnlyList<ResolvedLemmaDto> lemmas,
        IReadOnlyList<ResolvedStemDto> stems,
        IReadOnlyDictionary<string, string> secondaryStemCorrections)
    {
        var issues = new List<SegmentDimensionIssue>();
        var rootLookup = roots
            .Where(root => !string.IsNullOrWhiteSpace(root.RootBuckwalter))
            .GroupBy(root => root.RootBuckwalter!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var lemmaById = lemmas.ToDictionary(lemma => lemma.AssignedId);
        var lemmaLookup = BuildLemmaBuckwalterLookup(words, lemmas, lemmaById);
        var stemTextToId = stems.ToDictionary(stem => stem.StemText, stem => stem.AssignedId, StringComparer.Ordinal);
        var resolvedWords = new List<AlignedWordDto>(words.Count);

        foreach (var word in words)
        {
            var stemSegments = word.Segments
                .Where(segment => string.Equals(segment.Kind, "STEM", StringComparison.Ordinal))
                .ToList();
            var isSingleStemWord = stemSegments.Count == 1;
            var primaryStemNumber = stemSegments.Count > 0
                ? stemSegments.Min(segment => segment.SegmentNumber)
                : (short)0;
            var headLemmaBuckwalter = word.LemmaId.HasValue && lemmaById.TryGetValue(word.LemmaId.Value, out var headLemma)
                ? headLemma.LemmaBuckwalter
                : null;
            var resolvedSegments = new List<AlignedSegmentDto>(word.Segments.Count);

            foreach (var segment in word.Segments)
            {
                var segmentLocation = $"{word.Location}:{segment.SegmentNumber}";
                var rootId = ResolveRootId(segment, rootLookup, issues, segmentLocation);
                var lemmaId = ResolveLemmaId(
                    segment,
                    word.LemmaId,
                    headLemmaBuckwalter,
                    isSingleStemWord,
                    lemmaLookup,
                    lemmas,
                    issues,
                    segmentLocation);
                var stemId = ResolveStemId(
                    segment,
                    word.StemId,
                    isSingleStemWord,
                    primaryStemNumber,
                    stemTextToId,
                    secondaryStemCorrections,
                    segmentLocation);

                resolvedSegments.Add(segment with { RootId = rootId, LemmaId = lemmaId, StemId = stemId });
            }

            resolvedWords.Add(word with { Segments = resolvedSegments });
        }

        return new SegmentDimensionResolutionResult(resolvedWords, issues);
    }

    private static int? ResolveStemId(
        AlignedSegmentDto segment,
        int? wordHeadStemId,
        bool isSingleStemWord,
        short primaryStemNumber,
        IReadOnlyDictionary<string, int> stemTextToId,
        IReadOnlyDictionary<string, string> secondaryStemCorrections,
        string segmentLocation)
    {
        if (!string.Equals(segment.Kind, "STEM", StringComparison.Ordinal))
        {
            return null;
        }

        if (isSingleStemWord || segment.SegmentNumber == primaryStemNumber)
        {
            return wordHeadStemId;
        }

        if (secondaryStemCorrections.TryGetValue(segmentLocation, out var reviewedStemText)
            && stemTextToId.TryGetValue(reviewedStemText, out var stemId))
        {
            return stemId;
        }

        return null;
    }

    private static Dictionary<string, List<ResolvedLemmaDto>> BuildLemmaBuckwalterLookup(
        IReadOnlyList<AlignedWordDto> words,
        IReadOnlyList<ResolvedLemmaDto> lemmas,
        IReadOnlyDictionary<int, ResolvedLemmaDto> lemmaById)
    {
        var lookup = lemmas
            .Where(lemma => !string.IsNullOrWhiteSpace(lemma.LemmaBuckwalter))
            .GroupBy(lemma => lemma.LemmaBuckwalter!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        foreach (var word in words)
        {
            var stemSegments = word.Segments
                .Where(segment => string.Equals(segment.Kind, "STEM", StringComparison.Ordinal))
                .ToList();
            if (stemSegments.Count != 1 || !word.LemmaId.HasValue)
            {
                continue;
            }

            if (!lemmaById.TryGetValue(word.LemmaId.Value, out var headLemma))
            {
                continue;
            }

            var stemLemmaBuckwalter = stemSegments[0].LemmaBuckwalter;
            if (string.IsNullOrWhiteSpace(stemLemmaBuckwalter))
            {
                continue;
            }

            if (!lookup.TryGetValue(stemLemmaBuckwalter, out var existing))
            {
                lookup[stemLemmaBuckwalter] = [headLemma];
                continue;
            }

            if (existing.All(candidate => candidate.AssignedId != headLemma.AssignedId))
            {
                existing.Add(headLemma);
            }
        }

        return lookup;
    }

    private static int? ResolveRootId(
        AlignedSegmentDto segment,
        IReadOnlyDictionary<string, List<ResolvedRootDto>> rootsByBuckwalter,
        List<SegmentDimensionIssue> issues,
        string segmentLocation)
    {
        if (string.IsNullOrWhiteSpace(segment.RootBuckwalter))
        {
            return null;
        }

        if (!rootsByBuckwalter.TryGetValue(segment.RootBuckwalter, out var candidates))
        {
            issues.Add(new SegmentDimensionIssue(
                MorphologyInvariants.CheckSegRootResolves,
                segmentLocation,
                $"root_buckwalter '{segment.RootBuckwalter}' does not resolve to quran_roots"));
            return null;
        }

        if (candidates.Count != 1)
        {
            issues.Add(new SegmentDimensionIssue(
                MorphologyInvariants.CheckSegRootResolves,
                segmentLocation,
                $"root_buckwalter '{segment.RootBuckwalter}' resolves to {candidates.Count} roots"));
            return null;
        }

        return candidates[0].AssignedId;
    }

    private static int? ResolveLemmaId(
        AlignedSegmentDto segment,
        int? wordHeadLemmaId,
        string? wordHeadLemmaBuckwalter,
        bool isSingleStemWord,
        IReadOnlyDictionary<string, List<ResolvedLemmaDto>> lemmasByBuckwalter,
        IReadOnlyList<ResolvedLemmaDto> lemmas,
        List<SegmentDimensionIssue> issues,
        string segmentLocation)
    {
        if (!string.Equals(segment.Kind, "STEM", StringComparison.Ordinal))
        {
            return null;
        }

        if (isSingleStemWord)
        {
            return wordHeadLemmaId;
        }

        if (string.IsNullOrWhiteSpace(segment.LemmaBuckwalter))
        {
            return null;
        }

        if (wordHeadLemmaId.HasValue
            && string.Equals(segment.LemmaBuckwalter, wordHeadLemmaBuckwalter, StringComparison.Ordinal))
        {
            return wordHeadLemmaId;
        }

        if (!lemmasByBuckwalter.TryGetValue(segment.LemmaBuckwalter, out var candidates))
        {
            var formMatches = lemmas
                .Where(lemma => SegmentFormMatchesLemmaText(segment, lemma))
                .ToList();
            if (formMatches.Count == 1)
            {
                return formMatches[0].AssignedId;
            }

            issues.Add(new SegmentDimensionIssue(
                MorphologyInvariants.CheckSegLemmaMultiStemResolves,
                segmentLocation,
                formMatches.Count > 1
                    ? $"lemma_buckwalter '{segment.LemmaBuckwalter}' does not resolve and segment form matches {formMatches.Count} lemmas"
                    : $"lemma_buckwalter '{segment.LemmaBuckwalter}' does not resolve to quran_lemmas"));
            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates[0].AssignedId;
        }

        var safeMatches = candidates
            .Where(candidate => SegmentFormMatchesLemmaText(segment, candidate))
            .ToList();
        if (safeMatches.Count == 1)
        {
            return safeMatches[0].AssignedId;
        }

        if (CuratedLemmaDisambiguation.TryGetValue((segment.Pos, segment.LemmaBuckwalter), out var curatedLemmaText))
        {
            var curatedMatches = candidates
                .Where(candidate => string.Equals(candidate.LemmaText, curatedLemmaText, StringComparison.Ordinal))
                .ToList();
            if (curatedMatches.Count == 1)
            {
                return curatedMatches[0].AssignedId;
            }
        }

        issues.Add(new SegmentDimensionIssue(
            MorphologyInvariants.CheckSegLemmaNoFanout,
            segmentLocation,
            $"lemma_buckwalter '{segment.LemmaBuckwalter}' resolves to {candidates.Count} lemmas without a safe form match"));
        return null;
    }

    private static bool SegmentFormMatchesLemmaText(AlignedSegmentDto segment, ResolvedLemmaDto lemma) =>
        !string.IsNullOrWhiteSpace(segment.FormArabicNormalized)
        && string.Equals(
            NormalizeArabicForDimensionMatch(segment.FormArabicNormalized),
            NormalizeArabicForDimensionMatch(lemma.LemmaText),
            StringComparison.Ordinal);

    private static string NormalizeArabicForDimensionMatch(string value) =>
        value.Replace("ـ", string.Empty, StringComparison.Ordinal).Trim();

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

    private sealed record SegmentDimensionResolutionResult(
        IReadOnlyList<AlignedWordDto> Words,
        IReadOnlyList<SegmentDimensionIssue> Issues);
}
