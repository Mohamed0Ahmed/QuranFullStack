-- Build the full curation-candidate JSON document (single line, insertion-ordered keys).
-- Written to a raw temp file via \o; a downstream `python3 -m json.tool` pretty-prints it
-- (order-preserving) into the final segment-stem-curation-candidates.json.
\t on
\a
\o /tmp/f018_candidates_raw.json
SELECT json_build_object(
  'feature', '018-segment-stems-and-stems-explorer',
  'artifactType', 'segment-stem-curation-candidates',
  'status', 'candidates_only_not_approved',
  'generatedAtUtc', to_char((now() AT TIME ZONE 'utc'), 'YYYY-MM-DD"T"HH24:MI:SS"Z"'),
  'sourceDatabase', 'quran_dashboard @ localhost:5432 (local dev)',
  'notice', json_build_array(
    'These candidates are NOT approved mappings.',
    'mechanical_candidate_* is a mechanical review aid only: exact Arabic-text match of segment_form_arabic_normalized against quran_stems.stem_text. Deterministic, NOT safe.',
    'The final segment-stem-corrected-arabic.json artifact must be created ONLY after human/linguistic review. Do not treat any row here as approved.'
  ),
  'counts', json_build_object(
    'readable_words', (SELECT COUNT(*) FROM quran_word_morphology),
    'total_segment_rows', (SELECT COUNT(*) FROM quran_word_morphology_segments),
    'total_stem_segments', (SELECT COUNT(*) FROM quran_word_morphology_segments WHERE kind='STEM'),
    'words_with_1_stem', (SELECT COUNT(*) FROM (SELECT quran_word_id FROM quran_word_morphology_segments WHERE kind='STEM' GROUP BY quran_word_id HAVING COUNT(*)=1) a),
    'words_with_2_stem', (SELECT COUNT(*) FROM (SELECT quran_word_id FROM quran_word_morphology_segments WHERE kind='STEM' GROUP BY quran_word_id HAVING COUNT(*)=2) a),
    'words_with_0_stem', (SELECT COUNT(*) FROM quran_word_morphology m WHERE NOT EXISTS (SELECT 1 FROM quran_word_morphology_segments s WHERE s.quran_word_id=m.quran_word_id AND s.kind='STEM')),
    'words_with_more_than_2_stem', (SELECT COUNT(*) FROM (SELECT quran_word_id FROM quran_word_morphology_segments WHERE kind='STEM' GROUP BY quran_word_id HAVING COUNT(*)>2) a),
    'secondary_stem_candidates_generated', (SELECT COUNT(*) FROM seg_stem_candidates),
    'exact_text_matches', (SELECT COUNT(*) FROM seg_stem_candidates WHERE mechanical_candidate_match_method='exact_arabic_text_match'),
    'no_text_matches', (SELECT COUNT(*) FROM seg_stem_candidates WHERE mechanical_candidate_match_method='no_text_match'),
    'circular_matches', (SELECT COUNT(*) FROM seg_stem_candidates WHERE mechanical_candidate_is_same_as_head_stem),
    'distinct_secondary_lemmas', (SELECT COUNT(DISTINCT segment_lemma_id) FROM seg_stem_candidates),
    'distinct_secondary_forms', (SELECT COUNT(DISTINCT segment_form_arabic_normalized) FROM seg_stem_candidates),
    'quran_stems', (SELECT COUNT(*) FROM quran_stems),
    'quran_lemmas', (SELECT COUNT(*) FROM quran_lemmas),
    'quran_roots', (SELECT COUNT(*) FROM quran_roots)
  ),
  'riskSummary', json_build_object(
    'byStatus', (SELECT json_object_agg(candidate_status, n) FROM (SELECT candidate_status, COUNT(*) n FROM seg_stem_candidates GROUP BY candidate_status ORDER BY n DESC) s),
    'byFlag', (SELECT json_object_agg(f, n) FROM (SELECT f, COUNT(*) n FROM seg_stem_candidates, unnest(risk_flags) f GROUP BY f ORDER BY n DESC) s)
  ),
  'candidates', (
    SELECT json_agg(obj ORDER BY quran_word_id, segment_number)
    FROM (
      SELECT quran_word_id, segment_number, json_build_object(
        'location', location,
        'quran_word_id', quran_word_id,
        'word_text_uthmani', word_text_uthmani,
        'word_text_uthmani_simple', word_text_uthmani_simple,
        'segment_id', segment_id,
        'segment_location', segment_location,
        'segment_number', segment_number,
        'segment_kind', segment_kind,
        'segment_pos', segment_pos,
        'segment_form_buckwalter', segment_form_buckwalter,
        'segment_form_arabic_normalized', segment_form_arabic_normalized,
        'segment_lemma_buckwalter', segment_lemma_buckwalter,
        'segment_lemma_id', segment_lemma_id,
        'segment_lemma_text', segment_lemma_text,
        'segment_root_buckwalter', segment_root_buckwalter,
        'segment_root_id', segment_root_id,
        'segment_root_text', segment_root_text,
        'current_word_head_stem_id', current_word_head_stem_id,
        'current_word_head_stem_text', current_word_head_stem_text,
        'primary_stem_segment_id', primary_stem_segment_id,
        'primary_stem_segment_number', primary_stem_segment_number,
        'primary_stem_pos', primary_stem_pos,
        'primary_stem_form_arabic_normalized', primary_stem_form_arabic_normalized,
        'primary_stem_lemma_id', primary_stem_lemma_id,
        'primary_stem_lemma_text', primary_stem_lemma_text,
        'primary_stem_root_id', primary_stem_root_id,
        'primary_stem_root_text', primary_stem_root_text,
        'mechanical_candidate_stem_id', mechanical_candidate_stem_id,
        'mechanical_candidate_stem_text', mechanical_candidate_stem_text,
        'mechanical_candidate_match_method', mechanical_candidate_match_method,
        'mechanical_candidate_is_same_as_head_stem', mechanical_candidate_is_same_as_head_stem,
        'mechanical_candidate_is_existing_stem', mechanical_candidate_is_existing_stem,
        'candidate_status', candidate_status,
        'risk_flags', array_to_json(risk_flags),
        'review_decision', NULL,
        'reviewed_stem_id', NULL,
        'reviewed_stem_text', NULL,
        'review_notes', NULL,
        'stem_segments', json_build_array(
          json_build_object(
            'segment_id', primary_stem_segment_id,
            'segment_number', primary_stem_segment_number,
            'stem_rank', 1,
            'role', 'primary',
            'pos', primary_stem_pos,
            'form_buckwalter', primary_stem_form_buckwalter,
            'form_arabic_normalized', primary_stem_form_arabic_normalized,
            'lemma_id', primary_stem_lemma_id,
            'lemma_text', primary_stem_lemma_text,
            'root_id', primary_stem_root_id,
            'root_text', primary_stem_root_text
          ),
          json_build_object(
            'segment_id', segment_id,
            'segment_number', segment_number,
            'stem_rank', 2,
            'role', 'secondary',
            'pos', segment_pos,
            'form_buckwalter', segment_form_buckwalter,
            'form_arabic_normalized', segment_form_arabic_normalized,
            'lemma_id', segment_lemma_id,
            'lemma_text', segment_lemma_text,
            'root_id', segment_root_id,
            'root_text', segment_root_text
          )
        )
      ) AS obj
      FROM seg_stem_candidates
    ) sub
  )
);
\o
