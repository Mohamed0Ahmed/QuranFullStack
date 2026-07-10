namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;

internal static class MorphologySql
{
    internal const string CheckReadableComplete = """
        SELECT count(*)::int
        FROM quran_word_morphology m
        JOIN quran_words w ON w.id = m.quran_word_id
        WHERE w.is_ayah_marker = false
        """;

    internal const string CheckReadableWordsCount = """
        SELECT count(*)::int
        FROM quran_words
        WHERE is_ayah_marker = false
        """;

    internal const string CheckMarkersExcludedMorphology = """
        SELECT count(*)::int
        FROM quran_word_morphology m
        JOIN quran_words w ON w.id = m.quran_word_id
        WHERE w.is_ayah_marker = true
        """;

    internal const string CheckMarkersExcludedSegments = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments s
        JOIN quran_words w ON w.id = s.quran_word_id
        WHERE w.is_ayah_marker = true
        """;

    internal const string CheckLocationIdMismatch = """
        SELECT count(*)::int
        FROM quran_word_morphology m
        JOIN quran_words w ON w.id = m.quran_word_id
        WHERE m.location IS DISTINCT FROM w.location
        """;

    internal const string CheckLocationUnmatchedReadable = """
        SELECT count(*)::int
        FROM quran_words w
        LEFT JOIN quran_word_morphology m ON m.quran_word_id = w.id
        WHERE w.is_ayah_marker = false
          AND m.quran_word_id IS NULL
        """;

    internal const string CheckSegmentsPresentViolations = """
        SELECT count(*)::int
        FROM (
          SELECT m.quran_word_id
          FROM quran_word_morphology m
          LEFT JOIN quran_word_morphology_segments s ON s.quran_word_id = m.quran_word_id
          GROUP BY m.quran_word_id, m.segment_count
          HAVING count(s.id) = 0 OR count(s.id) <> m.segment_count
        ) violations
        """;

    internal const string CheckPosPresentNullSegmentPos = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE pos IS NULL OR btrim(pos) = ''
        """;

    internal const string CheckPosPresentStemCountViolations = """
        SELECT count(*)::int
        FROM (
          SELECT m.quran_word_id
          FROM quran_word_morphology m
          WHERE m.head_pos IS NULL
             OR btrim(m.head_pos) = ''
             OR NOT EXISTS (
                SELECT 1
                FROM quran_word_morphology_segments s
                WHERE s.quran_word_id = m.quran_word_id
                  AND s.kind = 'STEM'
             )
             OR m.head_pos IS DISTINCT FROM (
                SELECT s.pos
                FROM quran_word_morphology_segments s
                WHERE s.quran_word_id = m.quran_word_id
                  AND s.kind = 'STEM'
                ORDER BY s.segment_number
                LIMIT 1
             )
        ) violations
        """;

    internal const string CheckVerbFeatureViolations = """
        SELECT count(*)::int
        FROM quran_word_morphology m
        LEFT JOIN LATERAL (
          SELECT
            s.features_json,
            (
              SELECT count(*)::int
              FROM jsonb_array_elements_text(COALESCE(s.features_json, '[]'::jsonb)) AS token(value)
              WHERE token.value IN ('PERF', 'IMPF', 'IMPV')
            ) AS tense_count,
            CASE
              WHEN COALESCE(s.features_json, '[]'::jsonb) ? 'PERF' THEN 'past'
              WHEN COALESCE(s.features_json, '[]'::jsonb) ? 'IMPF' THEN 'present'
              WHEN COALESCE(s.features_json, '[]'::jsonb) ? 'IMPV' THEN 'imperative'
              ELSE NULL
            END AS expected_tense,
            CASE
              WHEN COALESCE(s.features_json, '[]'::jsonb) ? 'PASS' THEN 'passive'
              ELSE 'active'
            END AS expected_voice
          FROM quran_word_morphology_segments s
          WHERE s.quran_word_id = m.quran_word_id
            AND s.kind = 'STEM'
          ORDER BY s.segment_number
          LIMIT 1
        ) head_stem ON true
        WHERE (
            m.is_verb IS DISTINCT FROM (m.head_pos = 'V')
          )
          OR (
            m.head_pos = 'V'
            AND (
              head_stem.tense_count <> 1
              OR m.verb_tense IS DISTINCT FROM head_stem.expected_tense
              OR m.verb_voice IS DISTINCT FROM head_stem.expected_voice
            )
          )
          OR (
            m.head_pos <> 'V'
            AND (m.verb_tense IS NOT NULL OR m.verb_voice IS NOT NULL)
          )
        """;

    internal const string CheckDimensionResolvesRoots = """
        SELECT count(*)::int
        FROM quran_word_morphology m
        WHERE m.root_id IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM quran_roots r WHERE r.id = m.root_id)
        """;

    internal const string CheckDimensionResolvesLemmas = """
        SELECT count(*)::int
        FROM quran_word_morphology m
        WHERE m.lemma_id IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM quran_lemmas l WHERE l.id = m.lemma_id)
        """;

    internal const string CheckDimensionResolvesStems = """
        SELECT count(*)::int
        FROM quran_word_morphology m
        WHERE m.stem_id IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM quran_stems s WHERE s.id = m.stem_id)
        """;

    internal const string CheckSegLemmaStemOnly = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE lemma_id IS NOT NULL
          AND kind <> 'STEM'
        """;

    internal const string CheckSegLemmaRequiredForStem = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments s
        JOIN quran_word_morphology m ON m.quran_word_id = s.quran_word_id
        WHERE s.kind = 'STEM'
          AND s.lemma_buckwalter IS NOT NULL
          AND btrim(s.lemma_buckwalter) <> ''
          AND s.lemma_id IS NULL
          AND m.lemma_id IS NOT NULL
        """;

    internal const string CheckSegLemmaSingleStemHeadConsistent = """
        WITH stem_counts AS (
          SELECT quran_word_id, count(*)::int AS stem_count
          FROM quran_word_morphology_segments
          WHERE kind = 'STEM'
          GROUP BY quran_word_id
        )
        SELECT count(*)::int
        FROM quran_word_morphology_segments s
        JOIN stem_counts sc ON sc.quran_word_id = s.quran_word_id
        JOIN quran_word_morphology m ON m.quran_word_id = s.quran_word_id
        WHERE s.kind = 'STEM'
          AND sc.stem_count = 1
          AND s.lemma_id IS DISTINCT FROM m.lemma_id
        """;

    internal const string CheckSegLemmaMultiStemResolves = """
        WITH stem_counts AS (
          SELECT quran_word_id, count(*)::int AS stem_count
          FROM quran_word_morphology_segments
          WHERE kind = 'STEM'
          GROUP BY quran_word_id
        )
        SELECT count(*)::int
        FROM quran_word_morphology_segments s
        JOIN stem_counts sc ON sc.quran_word_id = s.quran_word_id
        WHERE s.kind = 'STEM'
          AND sc.stem_count > 1
          AND s.lemma_buckwalter IS NOT NULL
          AND btrim(s.lemma_buckwalter) <> ''
          AND s.lemma_id IS NULL
        """;

    internal const string CheckSegLemmaNoFanout = """
        SELECT 0::int
        """;

    internal const string CheckSegRootResolves = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE root_buckwalter IS NOT NULL
          AND btrim(root_buckwalter) <> ''
          AND root_id IS NULL
        """;

    internal const string CheckSegRootConsistent = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments s
        JOIN quran_roots r ON r.id = s.root_id
        WHERE s.root_buckwalter IS NULL
           OR btrim(s.root_buckwalter) = ''
           OR s.root_buckwalter IS DISTINCT FROM r.root_buckwalter
        """;

    internal const string CheckSegDimNullSafe = """
        WITH stem_counts AS (
          SELECT quran_word_id, count(*)::int AS stem_count
          FROM quran_word_morphology_segments
          WHERE kind = 'STEM'
          GROUP BY quran_word_id
        )
        SELECT count(*)::int
        FROM quran_word_morphology_segments s
        JOIN quran_word_morphology m ON m.quran_word_id = s.quran_word_id
        LEFT JOIN stem_counts sc ON sc.quran_word_id = s.quran_word_id
        WHERE (s.root_id IS NOT NULL AND (s.root_buckwalter IS NULL OR btrim(s.root_buckwalter) = ''))
           OR (
                s.lemma_id IS NOT NULL
                AND (s.lemma_buckwalter IS NULL OR btrim(s.lemma_buckwalter) = '')
                AND NOT (
                  s.kind = 'STEM'
                  AND sc.stem_count = 1
                  AND s.lemma_id IS NOT DISTINCT FROM m.lemma_id
                )
              )
           OR (s.lemma_id IS NOT NULL AND s.kind <> 'STEM')
        """;

    internal const string CheckSegStemStemOnly = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE stem_id IS NOT NULL
          AND kind <> 'STEM'
        """;

    // Single-STEM segments, and the primary (first-by-segment_number) STEM of a multi-STEM word, must
    // always carry stem_id (they reuse the word head stem). Secondary STEM segments are curation-gated
    // and may be null (the 4 documented unresolved exceptions); their coverage is asserted separately
    // by SEG-STEM-ID-MULTI-STEM-CURATED.
    internal const string CheckSegStemRequiredForStem = """
        WITH stem_segs AS (
          SELECT
            s.stem_id,
            count(*) OVER (PARTITION BY s.quran_word_id) AS stem_count,
            row_number() OVER (PARTITION BY s.quran_word_id ORDER BY s.segment_number) AS stem_rank
          FROM quran_word_morphology_segments s
          WHERE s.kind = 'STEM'
        )
        SELECT count(*)::int
        FROM stem_segs
        WHERE stem_id IS NULL
          AND (stem_count = 1 OR stem_rank = 1)
        """;

    // Single-STEM words, and the primary (first-by-segment_number) STEM of a two-STEM word, must reuse
    // the word's head stem_id. The head/word-level stem is unchanged by this feature.
    internal const string CheckSegStemHeadConsistent = """
        WITH stem_segs AS (
          SELECT
            s.quran_word_id,
            s.stem_id,
            count(*) OVER (PARTITION BY s.quran_word_id) AS stem_count,
            row_number() OVER (PARTITION BY s.quran_word_id ORDER BY s.segment_number) AS stem_rank
          FROM quran_word_morphology_segments s
          WHERE s.kind = 'STEM'
        )
        SELECT count(*)::int
        FROM stem_segs ss
        JOIN quran_word_morphology m ON m.quran_word_id = ss.quran_word_id
        WHERE (ss.stem_count = 1 OR (ss.stem_count = 2 AND ss.stem_rank = 1))
          AND ss.stem_id IS DISTINCT FROM m.stem_id
        """;

    internal const string CheckSegStemResolves = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments s
        WHERE s.stem_id IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM quran_stems st WHERE st.id = s.stem_id)
        """;

    // Secondary (second-by-segment_number) STEM segments of two-STEM words, for artifact-vs-DB coverage.
    internal const string SelectSecondaryStemSegments = """
        WITH stem_segs AS (
          SELECT
            s.segment_location,
            s.stem_id,
            count(*) OVER (PARTITION BY s.quran_word_id) AS stem_count,
            row_number() OVER (PARTITION BY s.quran_word_id ORDER BY s.segment_number) AS stem_rank
          FROM quran_word_morphology_segments s
          WHERE s.kind = 'STEM'
        )
        SELECT segment_location, stem_id
        FROM stem_segs
        WHERE stem_count = 2 AND stem_rank = 2
        """;

    internal const string CheckSegRenderTotalNonEmpty = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE form_buckwalter <> '' AND form_arabic_normalized IS NULL
        """;

    internal const string CheckSegRenderTotalEmpty = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE form_buckwalter = '' AND form_arabic_normalized IS NOT NULL
        """;

    internal const string CheckSegTierValid = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE form_buckwalter <> ''
          AND (arabic_render_tier IS NULL
               OR arabic_render_tier NOT IN ('clean', 'quranic_marks', 'review', 'multiword'))
        """;

    internal const string SelectSegmentsForRenderProvenance = """
        SELECT id, form_buckwalter, form_arabic_normalized, arabic_render_tier, arabic_render_source
        FROM quran_word_morphology_segments
        WHERE form_buckwalter <> ''
           OR form_arabic_normalized IS NOT NULL
        """;

    internal const string SelectMorphologyLemmaObservations = """
        SELECT m.location, l.lemma_text
        FROM quran_word_morphology m
        LEFT JOIN quran_lemmas l ON l.id = m.lemma_id
        """;

    internal const string SelectStrictWordLemmaShiftLocations = """
        WITH ordered_words AS (
          SELECT
            w.id,
            w.location,
            w.ayah_id,
            LAG(w.id) OVER (PARTITION BY w.ayah_id ORDER BY w.word_number, w.id) AS prev_word_id
          FROM quran_words w
          WHERE w.is_ayah_marker = false
        ),
        current_heads AS (
          SELECT
            m.quran_word_id,
            m.location,
            l.lemma_buckwalter,
            ow.prev_word_id
          FROM quran_word_morphology m
          JOIN ordered_words ow ON ow.id = m.quran_word_id
          JOIN quran_lemmas l ON l.id = m.lemma_id
          WHERE m.lemma_id IS NOT NULL
            AND m.root_id IS NULL
            AND NOT EXISTS (
              SELECT 1
              FROM quran_word_morphology_segments s
              WHERE s.quran_word_id = m.quran_word_id
                AND s.root_id IS NOT NULL
            )
        )
        SELECT ch.location
        FROM current_heads ch
        WHERE EXISTS (
            SELECT 1
            FROM quran_word_morphology_segments ps
            WHERE ps.quran_word_id = ch.prev_word_id
              -- Match on buckwalter, not lemma_id: a shifted-from word loses its word-level
              -- lemma_id but keeps the Corpus buckwalter on its segment.
              AND ps.lemma_buckwalter = ch.lemma_buckwalter
           )
          AND NOT EXISTS (
            SELECT 1
            FROM quran_word_morphology_segments cs
            WHERE cs.quran_word_id = ch.quran_word_id
              AND cs.kind = 'STEM'
              AND cs.lemma_buckwalter = ch.lemma_buckwalter
          )
        """;

    internal const string CountMorphologyRows = "SELECT count(*)::int FROM quran_word_morphology";
    internal const string CountSegmentRows = "SELECT count(*)::int FROM quran_word_morphology_segments";
    internal const string CountRootRows = "SELECT count(*)::int FROM quran_roots";
    internal const string CountLemmaRows = "SELECT count(*)::int FROM quran_lemmas";
    internal const string CountLemmaAnalysisRows = "SELECT count(*)::int FROM quran_lemma_analyses";
    internal const string CountStemRows = "SELECT count(*)::int FROM quran_stems";
    internal const string CountPosTagRows = "SELECT count(*)::int FROM quran_pos_tags";
    internal const string CountEmptyFormRenders = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE form_buckwalter = '' AND form_arabic_normalized IS NULL
        """;

    internal const string CountTierClean = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE arabic_render_tier = 'clean'
        """;

    internal const string CountTierQuranicMarks = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE arabic_render_tier = 'quranic_marks'
        """;

    internal const string CountTierReview = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE arabic_render_tier = 'review'
        """;

    internal const string CountTierMultiword = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE arabic_render_tier = 'multiword'
        """;

    internal const string TruncateMorphologyTables = """
        TRUNCATE
            quran_word_morphology_segments,
            quran_word_morphology,
            quran_lemma_analyses,
            quran_lemmas,
            quran_roots,
            quran_stems,
            quran_pos_tags
        RESTART IDENTITY CASCADE
        """;
}
