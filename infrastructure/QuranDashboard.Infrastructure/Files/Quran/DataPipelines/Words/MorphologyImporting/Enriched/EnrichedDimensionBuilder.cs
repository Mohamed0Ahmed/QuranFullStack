using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

// Builds the value-based root/lemma/stem dimensions for the enriched pathway AND projects the enriched
// source records into the persistence-shape MorphologySourceData DTO consumed unchanged by
// EfBulkMorphologyWriter.
//
// Identity rules (Feature 020, signed-off):
//   - Root identity  = Corpus rootBuckwalter (unambiguous); rootArabic is the stored root_text.
//   - Lemma identity = Corpus lemmaBuckwalter; primary lemmaArabic is the stored lemma_text; lemma→root
//                      link taken from the co-occurring root of the SAME segment (no QUL location join).
//   - Stem identity  = persisted schema rule only: normalized stem_text (the STEM segment's formArabic).
//                      stemBuckwalter is audit-only and MUST NOT create a separate persisted row
//                      (quran_stems has no buckwalter column and ResolvedStemDto has no buckwalter member).
//
// Audit-only JSON fields (corpusPresent, provenance, *MappingStatus, *QulCanonical, stemBuckwalter,
// quranWordIdVerifiedAgainstDashboard, boundaryAyah, boundaryHandling, text*) are intentionally not
// projected onto the DTO — they have no DTO members and cannot land in the DB by accident.
//
// QUL word-level location links are NEVER consulted: dimension identity comes only from the Corpus
// Buckwalter + bridge Arabic already merged into each enriched record upstream in SourceAudit.
public sealed class EnrichedDimensionBuilder
{
    private const string QuranicSmallYeh = "ۦ";

    private static readonly HashSet<string> KnownPosCodes =
        PosTagSeed.GetAll().Select(tag => tag.Code).ToHashSet(StringComparer.Ordinal);

    private static readonly string[] VerbTenseMarkers = ["PERF", "IMPF", "IMPV"];

    // Render-quality constants. arabic_render_tier/source describe how the Arabic form was produced; they
    // MUST NOT carry *MappingStatus (that audit axis is unstored). The enriched artifact already provides
    // display-clean formArabic, so every enriched segment is tier "clean" from source "corpus_enriched_bridge".
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
        // Display lemma dimension keyed by Arabic lemma_text: numeric-suffix buckwalter homographs that
        // render to the same text collapse to ONE row here (honours UNIQUE(lemma_text)).
        private readonly Dictionary<string, LemmaDimensionEntry> lemmaTextIndex = new(StringComparer.Ordinal);
        // Per-buckwalter analytical breakdown keyed by lemma_buckwalter: preserves each Corpus variant's
        // root/POS/first-occurrence under its display lemma.
        private readonly Dictionary<string, LemmaAnalysisEntry> lemmaAnalysisIndex = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DimensionEntry> stemIndex = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> rootLemmaMap = new(StringComparer.Ordinal);
        // Representative root per display lemma_text (the root at the lemma's first occurrence).
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

            // Resolve the head STEM segment for word-level head fields. HeadPos / IsVerb / verb features
            // come from the primary (lowest-numbered) STEM segment, mirroring the legacy assembler.
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

            // PHASE 1 (mint): ONLY the head STEM segment mints new root/lemma/stem dimensions. Each word
            // therefore mints at most one new row per dimension, stamped with this word's unique order as
            // FirstWordOrder — so no two rows ever share a FirstWordOrder (the UNIQUE index on
            // quran_{roots,lemmas,stems}.first_word_order_in_mushaf; this is the Phase 2A duplicate-key
            // defect fixed). Segment-level ids are filled later by ResolveSegmentDimensions, once every
            // word's head has minted, so a secondary STEM can reference a dimension whose head occurs later
            // in word order.
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

            // Segments are stored with null dimension ids here; ResolveSegmentDimensions fills them in phase 2.
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

        // PHASE 2 (resolve): every word's head has now minted, so the root/lemma/stem indices are complete.
        // Fill each segment's dimension ids by value-based lookup against those indices — STEM segments
        // resolve root+lemma+stem, non-STEM segments resolve only a shared root (lemma/stem stay null so the
        // STEM-only invariants hold). NOTHING is minted here, so the UNIQUE FirstWordOrder guarantee from
        // phase 1 is preserved; a secondary STEM resolves to a dimension even when that dimension's head
        // occurs later in word order. A value with no minted dimension (a buckwalter/stem_text that is never
        // any word's head) stays null rather than fabricating a FirstWordOrder.
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
                        // Resolve to the collapsed display lemma via the segment's own buckwalter's analysis
                        // (a buckwalter maps to exactly one display lemma).
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

        // Resolves a segment's lemma_buckwalter to the id of its COLLAPSED display lemma (quran_lemmas.id).
        // The buckwalter's analysis carries the display lemma link; a buckwalter that was never any word's
        // head has no analysis and stays null (mirrors the existing null-safe lemma rule).
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

    // --- segment projection --------------------------------------------------------------

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
                // formArabic arrives display-clean from the artifact; no further rendering is needed.
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

    // --- value-based dimension resolution ------------------------------------------------

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

    // Word-level lemma identity is the Arabic lemma_text (UNIQUE in quran_lemmas): numeric-suffix
    // buckwalter homographs that render to the SAME text collapse to ONE display lemma, so no second
    // quran_lemmas row can violate the unique index. Each distinct buckwalter still mints its own analysis
    // row (quran_lemma_analyses) carrying its first-occurrence root/POS/location, so the Corpus
    // distinctions are preserved rather than lost in the collapse.
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

        // Fall back to the buckwalter as the display key only when the artifact carries no Arabic text
        // (a buckwalter never collides with a real lemma_text).
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
            // Representative root of the display lemma = the root at its earliest occurrence; per-variant
            // roots (which may differ, e.g. عَصَا) are preserved on the analysis rows below.
            if (!lemmaRootLinks.TryGetValue(lemmaText, out var existing) || wordOrder < existing.WordOrder)
            {
                lemmaRootLinks[lemmaText] = (rootId.Value, wordOrder);
            }

            // distinct_lemmas_count fans out on DISPLAY lemmas (text), so collapsed homographs count once.
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

    // Stem identity is the persisted schema rule ONLY: normalized stem_text (the STEM segment's formArabic
    // under the enriched artifact). stemBuckwalter is audit-only and never mints a separate row —
    // vocalization-distinct stems sharing stem_text collapse to one quran_stems row by design (no
    // stem_buckwalter column exists). See plan §4. Quranic small yeh is render/provenance, not stem
    // identity, so it is removed only for quran_stems keys/text; segment form_arabic_normalized keeps it.
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

    // --- resolved dimension lists --------------------------------------------------------

    private static List<ResolvedRootDto> BuildResolvedRoots(
        Dictionary<string, RootDimensionEntry> rootIndex,
        Dictionary<string, HashSet<string>> rootLemmaMap)
    {
        var result = new List<ResolvedRootDto>(rootIndex.Count);
        foreach (var (rootBuckwalter, entry) in rootIndex.OrderBy(entry => entry.Value.FirstWordOrder))
        {
            var distinctLemmas = rootLemmaMap.TryGetValue(rootBuckwalter, out var set) ? set.Count : 0;
            // Root text is the bridge Arabic (rootArabic); root_buckwalter keeps the Corpus key. Both
            // columns exist in quran_roots; the Arabic text is the displayed root_text.
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
            // lemma_text is the display key; lemma_buckwalter stores the representative variant's Corpus key
            // (earliest occurrence). The full set of buckwalter variants lives in quran_lemma_analyses.
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

    // --- feature mapping (mirrors the legacy assembler's pure logic) ---------------------

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

        // quran_lemmas.lemma_buckwalter for the collapsed display lemma = the buckwalter of its earliest
        // occurrence (matches how first_word_order is chosen); deterministic under collapse.
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

    // One per distinct Corpus lemma_buckwalter. Links to its collapsed display lemma (LemmaId) and keeps
    // the variant's first-occurrence root/POS/location/count so different-root or different-POS homographs
    // are never analytically merged.
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

// Carries the word-order (quran_words.id) alongside each AlignedWordDto so dry-validation can reason
// about ordering without re-reading the DB.
public sealed record EnrichedAlignedWordProjection(AlignedWordDto Word, int QuranWordId);
