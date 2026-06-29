\echo == status counts ==
SELECT candidate_status, COUNT(*) AS n FROM seg_stem_candidates GROUP BY candidate_status ORDER BY n DESC;
\echo == flag counts ==
SELECT f AS risk_flag, COUNT(*) AS n FROM seg_stem_candidates, unnest(risk_flags) AS f GROUP BY f ORDER BY n DESC;
\echo == match-method counts ==
SELECT mechanical_candidate_match_method, COUNT(*) AS n FROM seg_stem_candidates GROUP BY 1 ORDER BY n DESC;
\echo == representative examples ==
SELECT location, segment_location, segment_pos AS pos, segment_form_arabic_normalized AS form,
       current_word_head_stem_id AS head_id, current_word_head_stem_text AS head_text,
       mechanical_candidate_stem_id AS mech_id, mechanical_candidate_stem_text AS mech_text,
       candidate_status, array_to_string(risk_flags, ';') AS flags
FROM seg_stem_candidates WHERE location IN ('2:3:6','2:11:9','2:90:1','72:16:1') ORDER BY quran_word_id;
\echo == artifact-target rows ==
SELECT segment_location, segment_form_arabic_normalized AS form, segment_pos AS pos,
       mechanical_candidate_stem_id AS mech_id, candidate_status, array_to_string(risk_flags,';') AS flags
FROM seg_stem_candidates WHERE 'artifact_stem_text' = ANY(risk_flags) ORDER BY segment_location;
\echo == no_text_match rows ==
SELECT segment_location, segment_form_arabic_normalized AS form, segment_pos, segment_lemma_id, segment_lemma_text, candidate_status
FROM seg_stem_candidates WHERE candidate_status='no_text_match' ORDER BY segment_location;
\echo == total candidate rows ==
SELECT COUNT(*) AS total_candidates, COUNT(DISTINCT segment_id) AS distinct_segments,
       COUNT(*) FILTER (WHERE segment_kind='STEM') AS stem_kind_rows,
       COUNT(*) FILTER (WHERE segment_number = primary_stem_segment_number) AS primary_leak
FROM seg_stem_candidates;
