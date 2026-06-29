-- Feature 018 — segment-stem curation candidates.
-- Reusable read-only CTE: one row per SECONDARY STEM segment of a 2-STEM word.
-- This file ends at the `classified` CTE (no final SELECT) so callers append their own
-- terminal SELECT (CSV export / JSON build / summary counts). Read-only; no DDL/DML.
WITH two_stem AS (
  -- words with exactly two STEM segments
  SELECT quran_word_id
  FROM quran_word_morphology_segments
  WHERE kind = 'STEM'
  GROUP BY quran_word_id
  HAVING COUNT(*) = 2
),
stem_seg AS (
  -- the STEM segments of those words, ranked by segment_number (1 = primary/head, 2 = secondary)
  SELECT s.*,
         ROW_NUMBER() OVER (PARTITION BY s.quran_word_id ORDER BY s.segment_number) AS stem_rank
  FROM quran_word_morphology_segments s
  JOIN two_stem t ON t.quran_word_id = s.quran_word_id
  WHERE s.kind = 'STEM'
),
stem_per_word AS (
  SELECT quran_word_id, COUNT(*) FILTER (WHERE kind = 'STEM') AS sn
  FROM quran_word_morphology_segments
  GROUP BY quran_word_id
),
artifact_stems AS (
  -- stems whose head-words are ALL 2-STEM words (clitic/contextual artifact stems)
  SELECT st.id
  FROM quran_word_morphology m
  JOIN stem_per_word n ON n.quran_word_id = m.quran_word_id
  JOIN quran_stems st ON st.id = m.stem_id
  GROUP BY st.id
  HAVING bool_and(n.sn = 2)
),
prim AS (SELECT * FROM stem_seg WHERE stem_rank = 1),
sec  AS (SELECT * FROM stem_seg WHERE stem_rank = 2),
cand AS (
  SELECT
    w.location                                  AS location,
    w.id                                        AS quran_word_id,
    w.text_uthmani                              AS word_text_uthmani,
    w.text_uthmani_simple                       AS word_text_uthmani_simple,
    sec.id                                       AS segment_id,
    sec.segment_location                         AS segment_location,
    sec.segment_number                           AS segment_number,
    sec.kind                                     AS segment_kind,
    sec.pos                                      AS segment_pos,
    sec.form_buckwalter                          AS segment_form_buckwalter,
    sec.form_arabic_normalized                   AS segment_form_arabic_normalized,
    sec.lemma_buckwalter                         AS segment_lemma_buckwalter,
    sec.lemma_id                                 AS segment_lemma_id,
    sl.lemma_text                                AS segment_lemma_text,
    sec.root_buckwalter                          AS segment_root_buckwalter,
    sec.root_id                                  AS segment_root_id,
    sr.root_text                                 AS segment_root_text,
    m.stem_id                                    AS current_word_head_stem_id,
    hs.stem_text                                 AS current_word_head_stem_text,
    prim.id                                      AS primary_stem_segment_id,
    prim.segment_number                          AS primary_stem_segment_number,
    prim.pos                                     AS primary_stem_pos,
    prim.form_buckwalter                         AS primary_stem_form_buckwalter,
    prim.form_arabic_normalized                  AS primary_stem_form_arabic_normalized,
    prim.lemma_id                                AS primary_stem_lemma_id,
    pl.lemma_text                                AS primary_stem_lemma_text,
    prim.root_id                                 AS primary_stem_root_id,
    pr.root_text                                 AS primary_stem_root_text,
    ms.id                                        AS mechanical_candidate_stem_id,
    ms.stem_text                                 AS mechanical_candidate_stem_text,
    CASE WHEN ms.id IS NULL THEN 'no_text_match' ELSE 'exact_arabic_text_match' END
                                                 AS mechanical_candidate_match_method,
    (ms.id IS NOT NULL AND ms.id = m.stem_id)    AS mechanical_candidate_is_same_as_head_stem,
    (ms.id IS NOT NULL)                          AS mechanical_candidate_is_existing_stem,
    (ms.id IS NOT NULL AND ms.id IN (SELECT id FROM artifact_stems)) AS is_artifact_target,
    (sec.form_arabic_normalized LIKE '%' || U&'\0651' || '%') AS form_has_shadda,
    (sec.pos <> 'N')                             AS is_function_word
  FROM sec
  JOIN prim ON prim.quran_word_id = sec.quran_word_id
  JOIN quran_words w ON w.id = sec.quran_word_id
  JOIN quran_word_morphology m ON m.quran_word_id = sec.quran_word_id
  LEFT JOIN quran_stems   hs ON hs.id = m.stem_id
  LEFT JOIN quran_stems   ms ON ms.stem_text = sec.form_arabic_normalized
  LEFT JOIN quran_lemmas  sl ON sl.id = sec.lemma_id
  LEFT JOIN quran_roots   sr ON sr.id = sec.root_id
  LEFT JOIN quran_lemmas  pl ON pl.id = prim.lemma_id
  LEFT JOIN quran_roots   pr ON pr.id = prim.root_id
),
classified AS (
  SELECT c.*,
    CASE
      WHEN c.mechanical_candidate_stem_id IS NULL              THEN 'no_text_match'
      WHEN c.mechanical_candidate_is_same_as_head_stem          THEN 'circular_match'
      WHEN c.is_artifact_target                                 THEN 'needs_new_or_canonical_stem_decision'
      WHEN c.form_has_shadda                                    THEN 'needs_review_contextual_idgham'
      ELSE 'needs_review_text_match'
    END AS candidate_status,
    ARRAY_REMOVE(ARRAY[
      CASE WHEN c.form_has_shadda                          THEN 'contextual_idgham'      END,
      CASE WHEN c.mechanical_candidate_stem_id IS NULL     THEN 'no_text_match'          END,
      CASE WHEN c.mechanical_candidate_is_same_as_head_stem THEN 'same_as_head_stem'      END,
      CASE WHEN c.is_function_word                         THEN 'function_word_compound' END,
      CASE WHEN c.is_artifact_target                       THEN 'artifact_stem_text'     END
    ], NULL) AS risk_flags
  FROM cand c
)
