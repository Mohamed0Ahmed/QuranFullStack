SELECT word_text_uthmani word, segment_form_arabic_normalized form, segment_pos pos,
       current_word_head_stem_id || ' (' || current_word_head_stem_text || ')' AS head_and_mech_stem,
       COUNT(*) n
FROM seg_stem_candidates
WHERE mechanical_candidate_is_same_as_head_stem
GROUP BY 1,2,3,4 ORDER BY n DESC, 1;
