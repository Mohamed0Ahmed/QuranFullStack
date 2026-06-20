namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.SimpleI3rabGeneration;

internal static class I3rabSql
{
    internal const string UpsertRule = """
        INSERT INTO quran_i3rab_rules (signature_key, rule_family, i3rab_arabic, default_status, description, sort_order)
        VALUES (@signatureKey, @ruleFamily, @i3rabArabic, @defaultStatus, @description, @sortOrder)
        ON CONFLICT (signature_key) DO UPDATE SET
            rule_family = EXCLUDED.rule_family,
            i3rab_arabic = EXCLUDED.i3rab_arabic,
            default_status = EXCLUDED.default_status,
            description = EXCLUDED.description,
            sort_order = EXCLUDED.sort_order
        """;

    internal const string CreateStagingTable = """
        CREATE TEMP TABLE i3rab_segment_staging (
            segment_id integer NOT NULL,
            i3rab_arabic text NULL,
            signature_key text NOT NULL,
            status text NOT NULL,
            review_reason text NULL
        ) ON COMMIT DROP
        """;

    internal const string CopyStaging = """
        COPY i3rab_segment_staging (segment_id, i3rab_arabic, signature_key, status, review_reason)
        FROM STDIN (FORMAT BINARY)
        """;

    internal const string UpdateSegmentsFromStaging = """
        UPDATE quran_word_morphology_segments AS target
        SET
            i3rab_arabic = staging.i3rab_arabic,
            i3rab_rule_id = rules.id,
            i3rab_status = staging.status,
            i3rab_review_reason = staging.review_reason
        FROM i3rab_segment_staging AS staging
        LEFT JOIN quran_i3rab_rules AS rules ON rules.signature_key = staging.signature_key
        WHERE target.id = staging.segment_id
        """;

    internal const string CountSegments = "SELECT COUNT(*)::int FROM quran_word_morphology_segments";

    internal const string CountReadableWords = """
        SELECT COUNT(*)::int
        FROM quran_words
        WHERE is_ayah_marker = false
        """;

    internal const string CountApprovedSegments = """
        SELECT COUNT(*)::int
        FROM quran_word_morphology_segments
        WHERE i3rab_status = 'approved'
        """;

    internal const string CountNeedsReviewSegments = """
        SELECT COUNT(*)::int
        FROM quran_word_morphology_segments
        WHERE i3rab_status = 'needs_review'
        """;

    internal const string CountUnsupportedSegments = """
        SELECT COUNT(*)::int
        FROM quran_word_morphology_segments
        WHERE i3rab_status = 'unsupported'
        """;

    internal const string CountApprovedViolations = """
        SELECT COUNT(*)::int
        FROM quran_word_morphology_segments
        WHERE i3rab_status = 'approved'
          AND (i3rab_arabic IS NULL OR i3rab_rule_id IS NULL)
        """;

    internal const string CountNeedsReviewViolations = """
        SELECT COUNT(*)::int
        FROM quran_word_morphology_segments
        WHERE i3rab_status = 'needs_review'
          AND (i3rab_rule_id IS NULL OR i3rab_review_reason IS NULL OR btrim(i3rab_review_reason) = '')
        """;

    internal const string CountUnsupportedViolations = """
        SELECT COUNT(*)::int
        FROM quran_word_morphology_segments
        WHERE i3rab_status = 'unsupported'
          AND (i3rab_review_reason IS NULL OR btrim(i3rab_review_reason) = '')
        """;

    internal const string CountDanglingRuleIds = """
        SELECT COUNT(*)::int
        FROM quran_word_morphology_segments AS segment
        LEFT JOIN quran_i3rab_rules AS rules ON rules.id = segment.i3rab_rule_id
        WHERE segment.i3rab_rule_id IS NOT NULL
          AND rules.id IS NULL
        """;

    internal const string CountCompleteStatuses = """
        SELECT COUNT(*)::int
        FROM quran_word_morphology_segments
        WHERE i3rab_status IN ('approved', 'needs_review', 'unsupported')
        """;

    internal const string CountDisplayableWords = """
        SELECT COUNT(*)::int
        FROM quran_words AS word
        INNER JOIN quran_word_morphology AS morphology ON morphology.quran_word_id = word.id
        WHERE word.is_ayah_marker = false
          AND (
              SELECT COUNT(*)::int
              FROM quran_word_morphology_segments AS segment
              WHERE segment.quran_word_id = word.id
                AND segment.i3rab_status = 'approved'
                AND segment.i3rab_arabic IS NOT NULL
                AND btrim(segment.i3rab_arabic) <> ''
          ) = morphology.segment_count
        """;

    internal const string CountUndisplayableWords = """
        SELECT COUNT(*)::int
        FROM quran_words AS word
        INNER JOIN quran_word_morphology AS morphology ON morphology.quran_word_id = word.id
        WHERE word.is_ayah_marker = false
          AND (
              SELECT COUNT(*)::int
              FROM quran_word_morphology_segments AS segment
              WHERE segment.quran_word_id = word.id
                AND segment.i3rab_status = 'approved'
                AND segment.i3rab_arabic IS NOT NULL
                AND btrim(segment.i3rab_arabic) <> ''
          ) < morphology.segment_count
        """;

    // Counts how many of the authoritative FR-011 rule-layer label corrections are present after a run
    // (the catalogue owns these labels instead of reusing quran_pos_tags.arabic_label — coverage report §7).
    // The corrected signatures are passed as a text[] parameter so the set stays single-sourced in
    // I3rabSeedLabelCorrections; matching by exact signature avoids the earlier ':'-part heuristic that
    // missed multi-part corrections such as STEM:N:GEN:1S.
    internal const string CountLabelCorrectionsPresent = """
        SELECT COUNT(*)::int
        FROM quran_i3rab_rules
        WHERE signature_key = ANY(@correctedSignatures)
        """;

    internal const string SegmentSourceFingerprint = """
        SELECT COALESCE(
            md5(string_agg(
                concat_ws('|',
                    id::text,
                    quran_word_id::text,
                    segment_location,
                    segment_number::text,
                    kind,
                    pos,
                    form_buckwalter,
                    COALESCE(form_arabic_normalized, ''),
                    COALESCE(arabic_render_tier, ''),
                    arabic_render_source,
                    COALESCE(root_buckwalter, ''),
                    COALESCE(lemma_buckwalter, ''),
                    features_raw,
                    COALESCE(features_json::text, '')) ,
                ',' ORDER BY id)),
            'empty')
        FROM quran_word_morphology_segments
        """;

    internal const string QuranWordsFingerprint = """
        SELECT COALESCE(
            md5(string_agg(
                concat_ws('|',
                    id::text,
                    location,
                    ayah_id::text,
                    surah_number::text,
                    ayah_number::text,
                    word_number::text,
                    page_number::text,
                    line_number::text,
                    line_word_order::text,
                    qpc_glyph,
                    text_uthmani,
                    text_uthmani_simple,
                    text_imlaei_simple,
                    word_key_imlaei_simple,
                    is_ayah_marker::text,
                    COALESCE(unique_tashkeel_word_id::text, ''),
                    COALESCE(unique_simple_word_id::text, '')),
                ',' ORDER BY id)),
            'empty')
        FROM quran_words
        """;

    internal const string PosTagsFingerprint = """
        SELECT COALESCE(
            md5(string_agg(
                concat_ws('|', code, arabic_label, english_label, category, sort_order::text, COALESCE(description, '')),
                ',' ORDER BY code)),
            'empty')
        FROM quran_pos_tags
        """;

    internal const string RuleUsage = """
        SELECT
            rules.signature_key,
            rules.rule_family,
            rules.i3rab_arabic,
            COUNT(segment.id)::int AS segment_count
        FROM quran_i3rab_rules AS rules
        LEFT JOIN quran_word_morphology_segments AS segment ON segment.i3rab_rule_id = rules.id
        GROUP BY rules.signature_key, rules.rule_family, rules.i3rab_arabic, rules.sort_order
        ORDER BY rules.sort_order
        """;

    internal const string NullFormSegmentIds = """
        SELECT id::int
        FROM quran_word_morphology_segments
        WHERE form_arabic_normalized IS NULL
        ORDER BY id
        """;
}
