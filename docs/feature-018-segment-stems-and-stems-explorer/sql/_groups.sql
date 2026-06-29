\echo @@A by_secondary_lemma
SELECT segment_lemma_id, segment_lemma_text,
       COUNT(*) n,
       COUNT(*) FILTER (WHERE mechanical_candidate_match_method='exact_arabic_text_match') exact,
       COUNT(*) FILTER (WHERE mechanical_candidate_match_method='no_text_match') no_match,
       COUNT(*) FILTER (WHERE mechanical_candidate_is_same_as_head_stem) circular
FROM seg_stem_candidates GROUP BY 1,2 ORDER BY n DESC;
\echo @@B by_secondary_pos
SELECT segment_pos, COUNT(*) n,
       COUNT(*) FILTER (WHERE mechanical_candidate_match_method='no_text_match') no_match,
       COUNT(*) FILTER (WHERE mechanical_candidate_is_same_as_head_stem) circular
FROM seg_stem_candidates GROUP BY 1 ORDER BY n DESC;
\echo @@C by_secondary_form
SELECT segment_form_arabic_normalized form, COUNT(*) n,
       string_agg(DISTINCT segment_pos, '/' ORDER BY segment_pos) pos_set,
       MAX(mechanical_candidate_stem_id) mech_stem_id, MAX(mechanical_candidate_stem_text) mech_stem_text,
       string_agg(DISTINCT candidate_status, '/' ORDER BY candidate_status) statuses
FROM seg_stem_candidates GROUP BY 1 ORDER BY n DESC;
\echo @@D by_pos_pattern
SELECT primary_stem_pos || '+' || segment_pos pattern, COUNT(*) n,
       (array_agg(word_text_uthmani ORDER BY quran_word_id))[1] example
FROM seg_stem_candidates GROUP BY 1 ORDER BY n DESC;
\echo @@E circular_full
SELECT segment_location, segment_form_arabic_normalized form, segment_pos pos,
       current_word_head_stem_id head_id, current_word_head_stem_text head_text,
       mechanical_candidate_stem_id mech_id, word_text_uthmani word
FROM seg_stem_candidates WHERE mechanical_candidate_is_same_as_head_stem ORDER BY quran_word_id;
\echo @@F no_text_match_full
SELECT segment_location, word_text_uthmani word, segment_form_arabic_normalized form, segment_pos pos,
       segment_lemma_id, segment_lemma_text, current_word_head_stem_id head_id, current_word_head_stem_text head_text
FROM seg_stem_candidates WHERE candidate_status='no_text_match' ORDER BY quran_word_id;
\echo @@G idgham_by_form
SELECT segment_form_arabic_normalized form, segment_pos pos, COUNT(*) n,
       MAX(mechanical_candidate_stem_id) mech_id, MAX(mechanical_candidate_stem_text) mech_text
FROM seg_stem_candidates WHERE 'contextual_idgham' = ANY(risk_flags)
GROUP BY 1,2 ORDER BY n DESC;
