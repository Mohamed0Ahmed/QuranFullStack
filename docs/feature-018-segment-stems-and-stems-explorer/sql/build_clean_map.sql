-- Emit a JSON map keyed by EXACT secondary form string -> de-shadda clean stem + shadda match.
-- Avoids hand-typed Arabic literal mismatches. Read-only.
\t on
\a
\o /projects/Dashboard/App/docs/feature-018-segment-stems-and-stems-explorer/sql/clean_stem_map.json
WITH two_stem AS (
  SELECT quran_word_id FROM quran_word_morphology_segments WHERE kind='STEM'
  GROUP BY quran_word_id HAVING COUNT(*)=2),
ranked AS (
  SELECT s.*, ROW_NUMBER() OVER (PARTITION BY s.quran_word_id ORDER BY s.segment_number) rk
  FROM quran_word_morphology_segments s JOIN two_stem t USING(quran_word_id) WHERE s.kind='STEM'),
forms AS (SELECT DISTINCT form_arabic_normalized f FROM ranked WHERE rk=2)
SELECT json_object_agg(f, json_build_object(
         'clean_form', replace(f, U&'\0651',''),
         'clean_stem_id', cln.id,
         'clean_stem_text', cln.stem_text,
         'mech_stem_id', mech.id,
         'mech_stem_text', mech.stem_text))
FROM forms
LEFT JOIN quran_stems mech ON mech.stem_text = f
LEFT JOIN quran_stems cln  ON cln.stem_text = replace(f, U&'\0651','');
\o
