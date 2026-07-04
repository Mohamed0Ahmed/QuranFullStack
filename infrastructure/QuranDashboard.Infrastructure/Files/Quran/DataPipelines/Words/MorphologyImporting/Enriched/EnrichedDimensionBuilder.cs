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
        private readonly Dictionary<string, LemmaDimensionEntry> lemmaIndex = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DimensionEntry> stemIndex = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> rootLemmaMap = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (int RootId, int WordOrder)> lemmaRootLinks = new(StringComparer.Ordinal);
        private readonly List<EnrichedAlignedWordProjection> alignedWords;

        private int agreementMatches;
        private int nextDimId = 1;

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

            // Resolve per-segment dimension ids, value-based from each segment's own buckwalter. This is the
            // SINGLE resolution pass — the word-level head ids are derived from the head STEM segment's
            // resolved ids below, so dimensions are not counted twice.
            var resolvedSegments = new List<AlignedSegmentDto>(projectedSegments.Count);
            foreach (var projected in projectedSegments)
            {
                var sourceSegment = (record.Segments ?? [])
                    .FirstOrDefault(segment => segment.SegmentNumber == projected.SegmentNumber);

                int? segRootId = null;
                int? segLemmaId = null;
                int? segStemId = null;

                if (sourceSegment is not null && IsStem(projected))
                {
                    segRootId = ResolveOrCreateRoot(
                        sourceSegment, wordOrder, rootIndex, rootLemmaMap, ref nextDimId);
                    segLemmaId = ResolveOrCreateLemma(
                        sourceSegment, wordOrder, segRootId, lemmaIndex, lemmaRootLinks, rootLemmaMap, ref nextDimId);
                    segStemId = ResolveOrCreateStem(sourceSegment, wordOrder, stemIndex, ref nextDimId);
                }
                else if (sourceSegment is not null && !IsStem(projected))
                {
                    // Non-STEM segments reference an already-created root dimension (shared with the head
                    // STEM of the same word). No new id is minted for non-STEM segments; if none exists yet
                    // the segment row stays null rather than guessing. The enriched artifact does not attach
                    // roots/lemmas to non-STEM segments, so this branch is normally a no-op.
                    segRootId = ResolveNonStemRoot(sourceSegment, rootIndex);
                }

                resolvedSegments.Add(projected with
                {
                    RootId = segRootId,
                    LemmaId = segLemmaId,
                    StemId = segStemId,
                });
            }

            // Word-level head dimensions = the head STEM segment's resolved ids (single source of truth,
            // no double-count). When there is no STEM segment, the word carries no head dimension.
            int? wordRootId = null;
            int? wordLemmaId = null;
            int? wordStemId = null;
            if (headStemSegmentNumber is not null)
            {
                var headResolved = resolvedSegments.First(segment => segment.SegmentNumber == headStemSegmentNumber);
                wordRootId = headResolved.RootId;
                wordLemmaId = headResolved.LemmaId;
                wordStemId = headResolved.StemId;
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
                    resolvedSegments,
                    wordRootId,
                    wordLemmaId,
                    wordStemId),
                wordOrder));
        }

        public EnrichedDimensionBuildResult ToResult()
        {
            var resolvedRoots = BuildResolvedRoots(rootIndex, rootLemmaMap);
            var resolvedLemmas = BuildResolvedLemmas(lemmaIndex, lemmaRootLinks);
            var resolvedStems = BuildResolvedStems(stemIndex);

            return new EnrichedDimensionBuildResult(
                alignedWords,
                resolvedRoots,
                resolvedLemmas,
                resolvedStems,
                charsetWarnings,
                unknownPosCodes.ToList(),
                agreementMatches,
                emptyFormLocations);
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

    // Non-STEM segments sometimes carry a root in the enriched artifact. When they do, they reference an
    // already-created root dimension (shared with the head STEM of the same word). We do NOT mint a new id
    // for a non-STEM segment; if no STEM has introduced the root yet, the segment row stays null rather
    // than guessing. The enriched artifact does not attach roots to non-STEM segments in practice.
    private static int? ResolveNonStemRoot(
        EnrichedMorphologySegment segment,
        Dictionary<string, RootDimensionEntry> rootIndex)
    {
        var rootBuckwalter = segment.RootBuckwalter;
        if (string.IsNullOrWhiteSpace(rootBuckwalter))
        {
            return null;
        }

        return rootIndex.TryGetValue(rootBuckwalter, out var entry) ? entry.Id : null;
    }

    private static int? ResolveOrCreateLemma(
        EnrichedMorphologySegment segment,
        int wordOrder,
        int? rootId,
        Dictionary<string, LemmaDimensionEntry> lemmaIndex,
        Dictionary<string, (int RootId, int WordOrder)> lemmaRootLinks,
        Dictionary<string, HashSet<string>> rootLemmaMap,
        ref int nextDimId)
    {
        var lemmaBuckwalter = segment.LemmaBuckwalter;
        if (string.IsNullOrWhiteSpace(lemmaBuckwalter))
        {
            return null;
        }

        if (!lemmaIndex.TryGetValue(lemmaBuckwalter, out var entry))
        {
            entry = new LemmaDimensionEntry(nextDimId++, wordOrder, segment.LemmaArabic);
            lemmaIndex[lemmaBuckwalter] = entry;
        }

        entry.AddWord(wordOrder);

        if (rootId.HasValue)
        {
            if (!lemmaRootLinks.TryGetValue(lemmaBuckwalter, out var existing) || wordOrder < existing.WordOrder)
            {
                lemmaRootLinks[lemmaBuckwalter] = (rootId.Value, wordOrder);
            }

            if (!string.IsNullOrWhiteSpace(segment.RootBuckwalter)
                && rootLemmaMap.TryGetValue(segment.RootBuckwalter, out var lemmaSet))
            {
                lemmaSet.Add(lemmaBuckwalter);
            }
        }

        return entry.Id;
    }

    // Stem identity is the persisted schema rule ONLY: normalized stem_text (the STEM segment's formArabic
    // under the enriched artifact). stemBuckwalter is audit-only and never mints a separate row —
    // vocalization-distinct stems sharing stem_text collapse to one quran_stems row by design (no
    // stem_buckwalter column exists). See plan §4.
    private static int? ResolveOrCreateStem(
        EnrichedMorphologySegment segment,
        int wordOrder,
        Dictionary<string, DimensionEntry> stemIndex,
        ref int nextDimId)
    {
        var stemText = segment.FormArabic;
        if (string.IsNullOrWhiteSpace(stemText))
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
        Dictionary<string, LemmaDimensionEntry> lemmaIndex,
        Dictionary<string, (int RootId, int WordOrder)> lemmaRootLinks)
    {
        var result = new List<ResolvedLemmaDto>(lemmaIndex.Count);
        foreach (var (lemmaBuckwalter, entry) in lemmaIndex.OrderBy(entry => entry.Value.FirstWordOrder))
        {
            int? rootId = lemmaRootLinks.TryGetValue(lemmaBuckwalter, out var link) ? link.RootId : null;
            // Lemma text is the primary lemmaArabic; lemma_buckwalter keeps the Corpus key.
            result.Add(new ResolvedLemmaDto(
                entry.Id,
                entry.LemmaArabic ?? lemmaBuckwalter,
                lemmaBuckwalter,
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

    private sealed class LemmaDimensionEntry(int id, int firstWordOrder, string? lemmaArabic)
        : DimensionEntry(id, firstWordOrder)
    {
        public string? LemmaArabic { get; } = lemmaArabic;
    }
}

public sealed record EnrichedDimensionBuildResult(
    IReadOnlyList<EnrichedAlignedWordProjection> Words,
    IReadOnlyList<ResolvedRootDto> ResolvedRoots,
    IReadOnlyList<ResolvedLemmaDto> ResolvedLemmas,
    IReadOnlyList<ResolvedStemDto> ResolvedStems,
    IReadOnlyList<string> CharsetWarnings,
    IReadOnlyList<string> UnknownPosCodes,
    int WholeWordAgreementMatches,
    IReadOnlyList<string> EmptyFormLocations);

// Carries the word-order (quran_words.id) alongside each AlignedWordDto so dry-validation can reason
// about ordering without re-reading the DB.
public sealed record EnrichedAlignedWordProjection(AlignedWordDto Word, int QuranWordId);
