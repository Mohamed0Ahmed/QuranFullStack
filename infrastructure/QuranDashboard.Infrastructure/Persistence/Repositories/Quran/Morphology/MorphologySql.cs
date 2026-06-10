namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Morphology;

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
          LEFT JOIN quran_word_morphology_segments s
            ON s.quran_word_id = m.quran_word_id AND s.kind = 'STEM'
          GROUP BY m.quran_word_id, m.head_pos
          HAVING count(s.id) <> 1
             OR max(s.pos) IS DISTINCT FROM m.head_pos
        ) violations
        """;

    internal const string CheckVerbFeatureViolations = """
        SELECT count(*)::int
        FROM quran_word_morphology m
        LEFT JOIN quran_word_morphology_segments s
          ON s.quran_word_id = m.quran_word_id AND s.kind = 'STEM'
        WHERE (
            m.is_verb = true
            AND (
              m.verb_tense IS NULL
              OR m.verb_voice IS NULL
              OR (
                SELECT count(*)::int
                FROM jsonb_array_elements_text(COALESCE(s.features_json, '[]'::jsonb)) AS token(value)
                WHERE token.value IN ('PERF', 'IMPF', 'IMPV')
              ) <> 1
            )
          )
          OR (
            m.is_verb = false
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

    internal const string CheckSegSourceValid = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE form_buckwalter <> ''
          AND arabic_render_source IS DISTINCT FROM 'buckwalter-transliteration'
        """;

    internal const string CheckSegNotUthmani = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments s
        JOIN quran_words w ON w.id = s.quran_word_id
        WHERE s.form_arabic_normalized IS NOT NULL
          AND (s.form_arabic_normalized = w.text_uthmani
               OR s.form_arabic_normalized = w.qpc_glyph)
        """;

    internal const string CheckSegBuckwalterPresent = """
        SELECT count(*)::int
        FROM quran_word_morphology_segments
        WHERE form_buckwalter IS NULL
        """;

    internal const string CountMorphologyRows = "SELECT count(*)::int FROM quran_word_morphology";
    internal const string CountSegmentRows = "SELECT count(*)::int FROM quran_word_morphology_segments";
    internal const string CountRootRows = "SELECT count(*)::int FROM quran_roots";
    internal const string CountLemmaRows = "SELECT count(*)::int FROM quran_lemmas";
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
            quran_lemmas,
            quran_roots,
            quran_stems,
            quran_pos_tags
        RESTART IDENTITY CASCADE
        """;
}
