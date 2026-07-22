using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

public sealed class EnrichedDimensionBuilder
{
    private const string QuranicSmallYeh = "ۦ";

    private static readonly HashSet<string> KnownPosCodes =
        PosTagSeed.GetAll().Select(tag => tag.Code).ToHashSet(StringComparer.Ordinal);

    private static readonly string[] VerbTenseMarkers = ["PERF", "IMPF", "IMPV"];

    public const string EnrichedRenderTier = "clean";
    public const string EnrichedRenderSource = MorphologyInvariants.EnrichedRenderSource;

    public EnrichedDimensionBuildResult Build(IReadOnlyList<EnrichedMorphologyRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var state = new BuildState(records.Count);
        foreach (var record in records)
        {
            state.Add(record);
        }

        return state.ToResult();
    }

    public async Task<EnrichedDimensionBuildResult> BuildAsync(
        IAsyncEnumerable<EnrichedMorphologyRecord> records,
        int expectedRecordCount,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(records);

        var state = new BuildState(expectedRecordCount);
        await foreach (var record in records.WithCancellation(ct))
        {
            state.Add(record);
        }

        return state.ToResult();
    }

    private sealed class BuildState
    {
        private readonly List<string> charsetWarnings = [];
        private readonly SortedSet<string> unknownPosCodes = new(StringComparer.Ordinal);
        private readonly List<string> emptyFormLocations = [];
        private readonly Dictionary<string, RootDimensionEntry> rootIndex = new(StringComparer.Ordinal);
        // Keyed by Arabic lemma_text: buckwalter homographs rendering to the same text collapse to ONE row (honours UNIQUE(lemma_text)).
        private readonly Dictionary<string, LemmaDimensionEntry> lemmaTextIndex = new(StringComparer.Ordinal);
        private readonly Dictionary<string, LemmaAnalysisEntry> lemmaAnalysisIndex = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DimensionEntry> stemIndex = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> rootLemmaMap = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (int RootId, int WordOrder)> lemmaRootLinks = new(StringComparer.Ordinal);
        private readonly List<EnrichedAlignedWordProjection> alignedWords;

        private int agreementMatches;
        private int nextDimId = 1;
        private int nextAnalysisId = 1;

        public BuildState(int expectedRecordCount)
        {
            alignedWords = new List<EnrichedAlignedWordProjection>(Math.Max(expectedRecordCount, 0));
        }

        public void Add(EnrichedMorphologyRecord record)
        {
            var location = RequireLocation(record);
            var wordOrder = RequireQuranWordId(record, location);
            var projectedSegments = ProjectSegments(
                record, location, charsetWarnings, unknownPosCodes, emptyFormLocations);

            if (!string.IsNullOrWhiteSpace(record.TextUthmani))
            {
                var wholeWordRender = string.Concat(
                    projectedSegments.Select(segment => segment.FormArabicNormalized ?? string.Empty));
                if (wholeWordRender == record.TextUthmani)
                {
                    agreementMatches++;
                }
            }

            var stemSegments = projectedSegments
                .Where(segment => string.Equals(segment.Kind, "STEM", StringComparison.Ordinal))
                .ToList();
            var headStemSegmentNumber = stemSegments.Count > 0
                ? stemSegments.Min(segment => segment.SegmentNumber)
                : (short?)null;
            var headStemSource = headStemSegmentNumber is null
                ? null
                : (record.Segments ?? []).FirstOrDefault(segment => segment.SegmentNumber == headStemSegmentNumber);
            var headFeatures = headStemSource is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : ParseFeatureTokens(headStemSource.FeaturesRaw);
            var headPos = headStemSegmentNumber is null
                ? projectedSegments.FirstOrDefault()?.Pos ?? string.Empty
                : projectedSegments.First(segment => segment.SegmentNumber == headStemSegmentNumber).Pos;
            var isVerb = string.Equals(headPos, "V", StringComparison.Ordinal);

            // PHASE 1 mint: only the head STEM mints, stamping this word's unique order as FirstWordOrder
            // (honours UNIQUE first_word_order_in_mushaf; prevents the duplicate-key defect). Segment ids resolve in phase 2.
            int? wordRootId = null;
            int? wordLemmaId = null;
            int? wordStemId = null;
            if (headStemSource is not null)
            {
                wordRootId = ResolveOrCreateRoot(
                    headStemSource, wordOrder, rootIndex, rootLemmaMap, ref nextDimId);
                wordLemmaId = ResolveOrCreateLemma(
                    headStemSource, wordOrder, location, headPos, wordRootId,
                    lemmaTextIndex, lemmaAnalysisIndex, lemmaRootLinks, rootLemmaMap,
                    ref nextDimId, ref nextAnalysisId);
                wordStemId = ResolveOrCreateStem(headStemSource, wordOrder, stemIndex, ref nextDimId);
            }

            alignedWords.Add(new EnrichedAlignedWordProjection(
                new AlignedWordDto(
                    location,
                    headPos,
                    isVerb,
                    isVerb ? MapVerbTense(headFeatures) : null,
                    isVerb ? MapVerbVoice(headFeatures) : null,
                    MapCaseFeature(headFeatures),
                    BuildFeaturesJson(headStemSource?.FeaturesRaw),
                    projectedSegments,
                    wordRootId,
                    wordLemmaId,
                    wordStemId),
                wordOrder));
        }

        public EnrichedDimensionBuildResult ToResult()
        {
            var resolvedWords = ResolveSegmentDimensions();
            var resolvedRoots = BuildResolvedRoots(rootIndex, rootLemmaMap);
            var resolvedLemmas = BuildResolvedLemmas(lemmaTextIndex, lemmaRootLinks);
            var resolvedLemmaAnalyses = BuildResolvedLemmaAnalyses(lemmaAnalysisIndex);
            var resolvedStems = BuildResolvedStems(stemIndex);

            return new EnrichedDimensionBuildResult(
                resolvedWords,
                resolvedRoots,
                resolvedLemmas,
                resolvedStems,
                resolvedLemmaAnalyses,
                charsetWarnings,
                unknownPosCodes.ToList(),
                agreementMatches,
                emptyFormLocations);
        }

        // PHASE 2 resolve: mints NOTHING (preserves phase-1 UNIQUE FirstWordOrder); a value that was never
        // a word's head stays null rather than fabricating a FirstWordOrder.
        private List<EnrichedAlignedWordProjection> ResolveSegmentDimensions()
        {
            var resolved = new List<EnrichedAlignedWordProjection>(alignedWords.Count);
            foreach (var projection in alignedWords)
            {
                var word = projection.Word;
                var segments = new List<AlignedSegmentDto>(word.Segments.Count);
                foreach (var segment in word.Segments)
                {
                    var isStem = IsStem(segment);
                    segments.Add(segment with
                    {
                        RootId = LookupId(rootIndex, segment.RootBuckwalter),
                        LemmaId = isStem ? LookupLemmaId(segment.LemmaBuckwalter) : null,
                        StemId = isStem ? LookupId(stemIndex, NormalizeStemIdentity(segment.FormArabicNormalized)) : null,
                    });
                }

                resolved.Add(projection with { Word = word with { Segments = segments } });
            }

            return resolved;
        }

        private static int? LookupId<TEntry>(Dictionary<string, TEntry> index, string? key)
            where TEntry : DimensionEntry
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return index.TryGetValue(key, out var entry) ? entry.Id : null;
        }

        private int? LookupLemmaId(string? lemmaBuckwalter)
        {
            if (string.IsNullOrWhiteSpace(lemmaBuckwalter))
            {
                return null;
            }

            return lemmaAnalysisIndex.TryGetValue(lemmaBuckwalter, out var analysis) ? analysis.LemmaId : null;
        }
    }

    private static bool IsStem(AlignedSegmentDto segment) =>
        string.Equals(segment.Kind, "STEM", StringComparison.Ordinal);

    private static List<AlignedSegmentDto> ProjectSegments(
        EnrichedMorphologyRecord record,
        string location,
        List<string> charsetWarnings,
        SortedSet<string> unknownPosCodes,
        List<string> emptyFormLocations)
    {
        var segments = record.Segments ?? [];
        var result = new List<AlignedSegmentDto>(segments.Count);

        foreach (var segment in segments.OrderBy(segment => segment.SegmentNumber))
        {
            var pos = segment.Pos ?? string.Empty;
            if (!string.IsNullOrEmpty(pos) && !KnownPosCodes.Contains(pos))
            {
                unknownPosCodes.Add(pos);
            }

            var formBuckwalter = segment.FormBuckwalter ?? string.Empty;
            if (string.IsNullOrEmpty(formBuckwalter))
            {
                emptyFormLocations.Add($"{location}:{segment.SegmentNumber}");
            }

            result.Add(new AlignedSegmentDto(
                segment.SegmentNumber,
                string.IsNullOrEmpty(segment.Kind) ? string.Empty : segment.Kind,
                pos,
                formBuckwalter,
                string.IsNullOrEmpty(segment.FormArabic) ? null : segment.FormArabic,
                EnrichedRenderTier,
                EnrichedRenderSource,
                segment.RootBuckwalter,
                segment.LemmaBuckwalter,
                null,
                null,
                null,
                segment.FeaturesRaw ?? string.Empty,
                BuildFeaturesJson(segment.FeaturesRaw)));
        }

        return result;
    }

    private static int? ResolveOrCreateRoot(
        EnrichedMorphologySegment segment,
        int wordOrder,
        Dictionary<string, RootDimensionEntry> rootIndex,
        Dictionary<string, HashSet<string>> rootLemmaMap,
        ref int nextDimId)
    {
        var rootBuckwalter = segment.RootBuckwalter;
        if (string.IsNullOrWhiteSpace(rootBuckwalter))
        {
            return null;
        }

        if (!rootIndex.TryGetValue(rootBuckwalter, out var entry))
        {
            entry = new RootDimensionEntry(nextDimId++, wordOrder, segment.RootArabic);
            rootIndex[rootBuckwalter] = entry;
            rootLemmaMap[rootBuckwalter] = [];
        }

        entry.AddWord(wordOrder);
        return entry.Id;
    }

    private static int? ResolveOrCreateLemma(
        EnrichedMorphologySegment segment,
        int wordOrder,
        string location,
        string headPos,
        int? rootId,
        Dictionary<string, LemmaDimensionEntry> lemmaTextIndex,
        Dictionary<string, LemmaAnalysisEntry> lemmaAnalysisIndex,
        Dictionary<string, (int RootId, int WordOrder)> lemmaRootLinks,
        Dictionary<string, HashSet<string>> rootLemmaMap,
        ref int nextDimId,
        ref int nextAnalysisId)
    {
        var lemmaBuckwalter = segment.LemmaBuckwalter;
        if (string.IsNullOrWhiteSpace(lemmaBuckwalter))
        {
            return null;
        }

        var lemmaText = string.IsNullOrWhiteSpace(segment.LemmaArabic) ? lemmaBuckwalter : segment.LemmaArabic;
        var normalizedHeadPos = string.IsNullOrEmpty(headPos) ? null : headPos;

        if (!lemmaTextIndex.TryGetValue(lemmaText, out var lemmaEntry))
        {
            lemmaEntry = new LemmaDimensionEntry(nextDimId++, wordOrder, lemmaText, lemmaBuckwalter);
            lemmaTextIndex[lemmaText] = lemmaEntry;
        }

        lemmaEntry.AddWord(wordOrder);
        lemmaEntry.ConsiderRepresentative(wordOrder, lemmaBuckwalter);

        if (rootId.HasValue)
        {
            if (!lemmaRootLinks.TryGetValue(lemmaText, out var existing) || wordOrder < existing.WordOrder)
            {
                lemmaRootLinks[lemmaText] = (rootId.Value, wordOrder);
            }

            if (!string.IsNullOrWhiteSpace(segment.RootBuckwalter)
                && rootLemmaMap.TryGetValue(segment.RootBuckwalter, out var lemmaSet))
            {
                lemmaSet.Add(lemmaText);
            }
        }

        if (!lemmaAnalysisIndex.TryGetValue(lemmaBuckwalter, out var analysis))
        {
            analysis = new LemmaAnalysisEntry(
                nextAnalysisId++, lemmaEntry.Id, lemmaBuckwalter, wordOrder, location, rootId, normalizedHeadPos);
            lemmaAnalysisIndex[lemmaBuckwalter] = analysis;
        }

        analysis.Observe(wordOrder, location, rootId, normalizedHeadPos);

        return lemmaEntry.Id;
    }

    // Stem identity = normalized stem_text only; stemBuckwalter never mints a row (no column). Quranic small
    // yeh is stripped for stem keys/text but kept on segment form_arabic_normalized.
    private static int? ResolveOrCreateStem(
        EnrichedMorphologySegment segment,
        int wordOrder,
        Dictionary<string, DimensionEntry> stemIndex,
        ref int nextDimId)
    {
        var stemText = NormalizeStemIdentity(segment.FormArabic);
        if (stemText is null)
        {
            return null;
        }

        if (!stemIndex.TryGetValue(stemText, out var entry))
        {
            entry = new DimensionEntry(nextDimId++, wordOrder);
            stemIndex[stemText] = entry;
        }

        entry.AddWord(wordOrder);
        return entry.Id;
    }

    private static string? NormalizeStemIdentity(string? stemText)
    {
        if (string.IsNullOrWhiteSpace(stemText))
        {
            return null;
        }

        var normalized = stemText.Replace(QuranicSmallYeh, string.Empty, StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static List<ResolvedRootDto> BuildResolvedRoots(
        Dictionary<string, RootDimensionEntry> rootIndex,
        Dictionary<string, HashSet<string>> rootLemmaMap)
    {
        var result = new List<ResolvedRootDto>(rootIndex.Count);
        foreach (var (rootBuckwalter, entry) in rootIndex.OrderBy(entry => entry.Value.FirstWordOrder))
        {
            var distinctLemmas = rootLemmaMap.TryGetValue(rootBuckwalter, out var set) ? set.Count : 0;
            result.Add(new ResolvedRootDto(
                entry.Id,
                entry.RootArabic ?? rootBuckwalter,
                rootBuckwalter,
                entry.WordsCount,
                (short)distinctLemmas,
                entry.FirstWordOrder));
        }

        return result;
    }

    private static List<ResolvedLemmaDto> BuildResolvedLemmas(
        Dictionary<string, LemmaDimensionEntry> lemmaTextIndex,
        Dictionary<string, (int RootId, int WordOrder)> lemmaRootLinks)
    {
        var result = new List<ResolvedLemmaDto>(lemmaTextIndex.Count);
        foreach (var (lemmaText, entry) in lemmaTextIndex.OrderBy(entry => entry.Value.FirstWordOrder))
        {
            int? rootId = lemmaRootLinks.TryGetValue(lemmaText, out var link) ? link.RootId : null;
            result.Add(new ResolvedLemmaDto(
                entry.Id,
                entry.LemmaArabic ?? lemmaText,
                entry.RepresentativeBuckwalter,
                rootId,
                entry.WordsCount,
                entry.FirstWordOrder));
        }

        return result;
    }

    private static List<ResolvedLemmaAnalysisDto> BuildResolvedLemmaAnalyses(
        Dictionary<string, LemmaAnalysisEntry> lemmaAnalysisIndex)
    {
        var result = new List<ResolvedLemmaAnalysisDto>(lemmaAnalysisIndex.Count);
        foreach (var (_, entry) in lemmaAnalysisIndex.OrderBy(entry => entry.Value.FirstWordOrder))
        {
            result.Add(new ResolvedLemmaAnalysisDto(
                entry.Id,
                entry.LemmaId,
                entry.LemmaBuckwalter,
                entry.RootId,
                entry.HeadPos,
                entry.WordsCount,
                entry.FirstWordOrder,
                entry.FirstLocation));
        }

        return result;
    }

    private static List<ResolvedStemDto> BuildResolvedStems(
        Dictionary<string, DimensionEntry> stemIndex)
    {
        var result = new List<ResolvedStemDto>(stemIndex.Count);
        foreach (var (stemText, entry) in stemIndex.OrderBy(entry => entry.Value.FirstWordOrder))
        {
            result.Add(new ResolvedStemDto(
                entry.Id,
                stemText,
                entry.WordsCount,
                entry.FirstWordOrder));
        }

        return result;
    }

    private static HashSet<string> ParseFeatureTokens(string? featuresRaw)
    {
        if (string.IsNullOrWhiteSpace(featuresRaw))
        {
            return new HashSet<string>(StringComparer.Ordinal);
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

    private static string RequireLocation(EnrichedMorphologyRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Location))
        {
            throw new InvalidDataException(
                $"Enriched morphology record is missing 'location' (quranWordId={record.QuranWordId}).");
        }

        return record.Location;
    }

    private static int RequireQuranWordId(EnrichedMorphologyRecord record, string location)
    {
        if (record.QuranWordId is null || record.QuranWordId <= 0)
        {
            throw new InvalidDataException(
                $"Enriched morphology record '{location}' is missing a valid quranWordId.");
        }

        return record.QuranWordId.Value;
    }

    private class DimensionEntry(int id, int firstWordOrder)
    {
        public int Id { get; } = id;
        public int FirstWordOrder { get; private set; } = firstWordOrder;
        public int WordsCount { get; private set; }

        public void AddWord(int wordOrder)
        {
            WordsCount++;
            if (wordOrder < FirstWordOrder)
            {
                FirstWordOrder = wordOrder;
            }
        }
    }

    private sealed class RootDimensionEntry(int id, int firstWordOrder, string? rootArabic)
        : DimensionEntry(id, firstWordOrder)
    {
        public string? RootArabic { get; } = rootArabic;
    }

    private sealed class LemmaDimensionEntry : DimensionEntry
    {
        private int representativeWordOrder;

        public LemmaDimensionEntry(int id, int firstWordOrder, string? lemmaArabic, string representativeBuckwalter)
            : base(id, firstWordOrder)
        {
            LemmaArabic = lemmaArabic;
            RepresentativeBuckwalter = representativeBuckwalter;
            representativeWordOrder = firstWordOrder;
        }

        public string? LemmaArabic { get; }

        public string RepresentativeBuckwalter { get; private set; }

        public void ConsiderRepresentative(int wordOrder, string buckwalter)
        {
            if (wordOrder < representativeWordOrder)
            {
                representativeWordOrder = wordOrder;
                RepresentativeBuckwalter = buckwalter;
            }
        }
    }

    private sealed class LemmaAnalysisEntry
    {
        public LemmaAnalysisEntry(
            int id, int lemmaId, string lemmaBuckwalter, int firstWordOrder, string firstLocation,
            int? rootId, string? headPos)
        {
            Id = id;
            LemmaId = lemmaId;
            LemmaBuckwalter = lemmaBuckwalter;
            FirstWordOrder = firstWordOrder;
            FirstLocation = firstLocation;
            RootId = rootId;
            HeadPos = headPos;
        }

        public int Id { get; }
        public int LemmaId { get; }
        public string LemmaBuckwalter { get; }
        public int WordsCount { get; private set; }
        public int FirstWordOrder { get; private set; }
        public string FirstLocation { get; private set; }
        public int? RootId { get; private set; }
        public string? HeadPos { get; private set; }

        public void Observe(int wordOrder, string location, int? rootId, string? headPos)
        {
            WordsCount++;
            if (wordOrder < FirstWordOrder)
            {
                FirstWordOrder = wordOrder;
                FirstLocation = location;
                RootId = rootId;
                HeadPos = headPos;
            }
        }
    }
}

public sealed record EnrichedDimensionBuildResult(
    IReadOnlyList<EnrichedAlignedWordProjection> Words,
    IReadOnlyList<ResolvedRootDto> ResolvedRoots,
    IReadOnlyList<ResolvedLemmaDto> ResolvedLemmas,
    IReadOnlyList<ResolvedStemDto> ResolvedStems,
    IReadOnlyList<ResolvedLemmaAnalysisDto> ResolvedLemmaAnalyses,
    IReadOnlyList<string> CharsetWarnings,
    IReadOnlyList<string> UnknownPosCodes,
    int WholeWordAgreementMatches,
    IReadOnlyList<string> EmptyFormLocations);

public sealed record EnrichedAlignedWordProjection(AlignedWordDto Word, int QuranWordId);
