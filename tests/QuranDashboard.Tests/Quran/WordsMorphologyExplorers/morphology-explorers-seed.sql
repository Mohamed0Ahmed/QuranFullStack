-- ======================================================================
-- Lemmas & Stems Explorer — representative content slice (fixture seed, T018)
-- Feature 016 · read-only · deterministic · offline
-- ----------------------------------------------------------------------
-- Loaded by MorphologyExplorersTestFixture into a fresh Testcontainers
-- Postgres instance AFTER EnsureCreatedAsync. Covers only the rows the
-- Feature 016 lemma/stem list/summary/words/ayahs/surahs/relationships tests
-- assert on; it is intentionally NOT the full DB and NOT the developer's
-- local DB. Canonical Quranic Uthmani ayah text is used verbatim (reused from
-- the Feature 015 Roots seed for the same surah/ayah rows); individual word
-- display forms are real Arabic words used as morphology display text — no
-- Quran text is invented or altered.
--
-- Coverage goals (per T018):
--   (a) Lemma with NULL owned root        → L501 'نِعْمَة' (quran_lemmas.root_id NULL).
--   (b) Stem with NULL lemma AND NULL root→ S601 'مَجْهُول' (morphology rows with
--                                            lemma_id NULL, root_id NULL).
--   (c) Multi-type lemma AND stem with an EXACT count tie
--                                        → L503 'حُكْم' / S604 'حَكَمَ' each have
--                                            N=2 and ADJ=2; tie broken by earliest
--                                            Mushaf occurrence (N wins).
--   (d) Stem with multiple lemma AND multiple root candidates
--                                        → S602 'عَلِمَ': lemma L502×3 vs L504×1
--                                            (dominant L502); root R701×3 vs
--                                            R702×1 (dominant R701) — independent
--                                            rankings.
--   (e) Multiple matches in one ayah     → L500/S600 appear 4× in 1:1; L503/S604
--                                            2× in 1:2 and 2× in 1:3; S602 4× in 3:8.
--   (f) High-frequency paged rows        → L500/S600 across 7 ayahs / 3 surahs
--                                            (11 occurrences) for ayah paging, and
--                                            2 distinct simple + 2 tashkeel unique
--                                            forms for the words sub-view.
--   (g) Simple/tashkeel identities       → 7 distinct display forms each linked to
--                                            a tashkeel + simple unique identity.
--   (h) Mentioned/missing surahs         → L500/S600 in surahs {1,2,3}; missing =
--                                            {4..114} (111 surahs); mentioned+missing
--                                            disjoint union = 114 (all surahs).
--   (i) Related stems/lemmas             → L500 related stems = {S600}; S602
--                                            related lemmas = {L502, L504}.
--   (j) Matched ayah marker exclusion    → L509 has one real synthetic word plus
--                                            one synthetic ayah-marker segment;
--                                            visible word counts/rows exclude marker.
--
-- Driving relation reminder:
--   Lemmas:  quran_word_morphology_segments s (s.lemma_id = X) JOIN quran_words w
--   Stems:   quran_word_morphology m (m.stem_id = X) JOIN quran_words w
--   Word-level morphology may still supply data such as stem_id where no segment-level stem_id exists.
--   Lemma owned root = quran_lemmas.root_id (NOT morphology co-occurrence).
--
-- Deterministic ids (3001+ for words, 4000s lemmas, 600s stems, 700s roots)
-- keep this slice self-contained and independent of other fixture slices.
-- ======================================================================

-- ----------------------------------------------------------------------
-- Surahs (slice). Canonical Arabic names for the asserted rows; the
-- generate_series filler for 4..113 is obvious synthetic fixture data
-- (NOT authoritative catalog metadata) and only asserts surah_number +
-- non-empty name_arabic. Unique name_arabic is respected via distinct suffix.
-- ----------------------------------------------------------------------
INSERT INTO quran_surahs
  (surah_number, name_arabic, name_simple, name_transliteration, revelation_place, revelation_order, verses_count, bismillah_pre)
VALUES
  (1,   'الفاتحة',  'Al-Fatihah', 'Al-Fatihah', 'makkah',  5,  7,   FALSE),
  (2,   'البقرة',   'Al-Baqarah', 'Al-Baqarah', 'madinah', 87, 286, TRUE),
  (3,   'آل عمران', 'Aal-E-Imran','Aal-E-Imran','madinah', 89, 200, TRUE);

-- Catalog filler for the missing-surahs edge: L500/S600 are seeded into
-- surahs 1,2,3, so 4..114 must exist as missing candidates.
INSERT INTO quran_surahs
  (surah_number, name_arabic, name_simple, name_transliteration, revelation_place, revelation_order, verses_count, bismillah_pre)
SELECT
  n,
  'سورة-صيغ-' || n::text,
  'MORPH-FIXTURE-' || n::text,
  'MORPH-FIXTURE-' || n::text,
  'makkah',
  n,
  1,
  FALSE
FROM generate_series(4, 114) AS n
ON CONFLICT (surah_number) DO NOTHING;

-- ----------------------------------------------------------------------
-- Mushaf pages referenced by quran_words.page_number (FK).
-- ----------------------------------------------------------------------
INSERT INTO quran_mushaf_pages
  (page_number, first_surah_number, first_ayah_number, last_surah_number, last_ayah_number, lines_count)
VALUES
  (1,  1, 1, 1, 3, 3),
  (2,  2, 1, 2, 1, 1),
  (5,  2, 25, 2, 25, 1),
  (50, 3, 1, 3, 8, 2)
ON CONFLICT DO NOTHING;

-- ----------------------------------------------------------------------
-- Ayahs referenced by the seeded occurrences (canonical Uthmani text,
-- reused verbatim from the Feature 015 Roots seed for the same rows).
-- ----------------------------------------------------------------------
INSERT INTO quran_ayahs
  (id, surah_number, ayah_number, verse_key, text_uthmani, words_count_source, words_count_real, page_from, page_to, juz_number, hizb_number, rub_number)
VALUES
  (11, 1, 1,  '1:1',  'بِسْمِ ٱللَّهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ',            4, 4, 1,  1,  NULL, NULL, NULL),
  (12, 1, 2,  '1:2',  'ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَٰلَمِينَ',           4, 4, 1,  1,  NULL, NULL, NULL),
  (13, 1, 3,  '1:3',  'ٱلرَّحْمَٰنِ ٱلرَّحِيمِ',                       2, 2, 1,  1,  NULL, NULL, NULL),
  (21, 2, 1,  '2:1',  'الٓمٓ',                                       1, 1, 2,  2,  NULL, NULL, NULL),
  (25, 2, 25, '2:25', 'وَبَشِّرِ ٱلَّذِينَ ءَامَنُوا۟ وَعَمِلُوا۟ ٱلصَّٰلِحَٰتِ', 6, 6, 5, 5, NULL, NULL, NULL),
  (31, 3, 1,  '3:1',  'أَلٓمٓ',                                      1, 1, 50, 50, NULL, NULL, NULL),
  (32, 3, 8,  '3:8',  'رَبَّنَا لَا تُزِغْ قُلُوبَنَا بَعْدَ إِذْ هَدَيْتَنَا', 7, 7, 50, 50, NULL, NULL, NULL);

-- ----------------------------------------------------------------------
-- Roots referenced by morphology rows.
--   R700 'ك ل م' — owned by L500 (and co-occurring root of S600 words).
--   R701 'ع ل م' — owned by L502 & L504; dominant co-occurring root of S602.
--   R702 'ك ت ب' — secondary co-occurring root of S602 (dominance tie-break).
-- ----------------------------------------------------------------------
INSERT INTO quran_roots
  (id, root_text, root_buckwalter, words_count, distinct_lemmas_count, first_word_order_in_mushaf)
VALUES
  (700, 'ك ل م', 'klm', 11, 1, 3001),
  (701, 'ع ل م', 'Alm', 4,  2, 7001),
  (702, 'ك ت ب', 'ktb', 1,  1, 7004);

-- ----------------------------------------------------------------------
-- Lemmas.
--   L500 'كَلِمَة'  — HIGH-FREQUENCY, owned root R700 (coverage f,h,i).
--   L501 'نِعْمَة'  — NULL owned root (coverage a).
--   L502 'عِلْم'   — owned root R701; dominant related lemma of S602.
--   L503 'حُكْم'   — NULL owned root; multi-type exact tie (coverage c).
--   L504 'مَعْرِفَة'— owned root R701; secondary related lemma of S602.
--   L508 'لَفْظٌ-تَجْرِيبِيّ' — synthetic same-lemma segment fan-out regression.
--   L509 'وَسْم-مُؤَشِّر-تَجْرِيبِيّ' — synthetic matched-marker exclusion regression.
-- words_count is reconciled below for word-level lemma rows; segment-only synthetic
-- lemmas keep explicit fixture counts.
-- ----------------------------------------------------------------------
INSERT INTO quran_lemmas
  (id, lemma_text, lemma_buckwalter, root_id, words_count, first_word_order_in_mushaf)
VALUES
  (500, 'كَلِمَة',    'kalimap',  700,  11, 3001),
  (501, 'نِعْمَة',    'niEomap',  NULL, 2,  4001),
  (502, 'عِلْم',     'Ailm',     701,  3,  7001),
  (503, 'حُكْم',     'Hukom',    NULL, 4,  5001),
  (504, 'مَعْرِفَة',  'maArifap', 701,  1,  7004),
  (506, 'لَا',       'lA',       NULL, 2,  8001),
  (507, 'أَن',      '>an',      NULL, 0,  8003),
  (508, 'لَفْظٌ-تَجْرِيبِيّ', 'fixture', NULL, 1, 8101),
  (509, 'وَسْم-مُؤَشِّر-تَجْرِيبِيّ', 'marker-fixture', NULL, 2, 8201);

-- ----------------------------------------------------------------------
-- Stems.
--   S600 'كَلَّمَ'  — HIGH-FREQUENCY (L500 words), dominant lemma L500,
--                     dominant root R700 (coverage f,h,i).
--   S601 'مَجْهُول' — NULL lemma AND NULL root (coverage b).
--   S602 'عَلِمَ'   — multiple lemma (L502×3,L504×1) and multiple root
--                     (R701×3,R702×1) candidates (coverage d,i).
--   S604 'حَكَمَ'   — multi-type exact tie with L503 (coverage c).
--   S605 'نِعْمَة'  — L501's stem.
--   S606 'سَاق-تَجْرِيبِيّ' — synthetic stem for segment-matched related-stem coverage.
-- ----------------------------------------------------------------------
INSERT INTO quran_stems
  (id, stem_text, words_count, first_word_order_in_mushaf)
VALUES
  (600, 'كَلَّمَ',   11, 3001),
  (601, 'مَجْهُول',  2,  6001),
  (602, 'عَلِمَ',    4,  7001),
  (604, 'حَكَمَ',    4,  5001),
  (605, 'نِعْمَة',   2,  4001),
  (606, 'سَاق-تَجْرِيبِيّ', 1, 8101);

-- ----------------------------------------------------------------------
-- Quran words (canonical ayah text reused for source rows; synthetic regression
-- rows are explicitly marked). unique_*_word_id set later.
-- ----------------------------------------------------------------------
INSERT INTO quran_words
  (id, location, ayah_id, surah_number, ayah_number, word_number, page_number, line_number, line_word_order, qpc_glyph, text_uthmani, text_uthmani_simple, text_imlaei_simple, word_key_imlaei_simple, is_ayah_marker, unique_tashkeel_word_id, unique_simple_word_id)
VALUES
  -- L500/S600 high-frequency words (root R700). 1:1 carries 4 matches (e).
  (3001, '1:1:1',  11, 1, 1,  1, 1,  1, 1, 'g3001', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (3002, '1:1:2',  11, 1, 1,  2, 1,  1, 2, 'g3002', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (3010, '1:1:3',  11, 1, 1,  3, 1,  1, 3, 'g3010', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (3011, '1:1:4',  11, 1, 1,  4, 1,  1, 4, 'g3011', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (3012, '1:1:5',  11, 1, 1,  5, 1,  1, 5, 'g3012', 'ۚ',      'ۚ',     'ۚ',     'ۚ',     TRUE,  NULL, NULL),
  (3003, '1:2:1',  12, 1, 2,  1, 1,  1, 1, 'g3003', 'كَلَّمَ',  'كلم',  'كلم',  'كلم',  FALSE, NULL, NULL),
  (3004, '1:3:1',  13, 1, 3,  1, 1,  1, 1, 'g3004', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (3005, '2:1:1',  21, 2, 1,  1, 2,  1, 1, 'g3005', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (3006, '2:25:1', 25, 2, 25, 1, 5,  1, 1, 'g3006', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (3007, '3:1:1',  31, 3, 1,  1, 50, 1, 1, 'g3007', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (3008, '3:8:1',  32, 3, 8,  1, 50, 1, 1, 'g3008', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (3009, '3:8:2',  32, 3, 8,  2, 50, 1, 2, 'g3009', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  -- L501/S605 (NULL owned root) — coverage (a).
  (4001, '2:1:2',  21, 2, 1,  2, 2,  1, 2, 'g4001', 'نِعْمَة', 'نعمة', 'نعمة', 'نعمة', FALSE, NULL, NULL),
  (4002, '3:1:2',  31, 3, 1,  2, 50, 1, 2, 'g4002', 'نِعْمَة', 'نعمة', 'نعمة', 'نعمة', FALSE, NULL, NULL),
  -- L503/S604 multi-type exact tie (N=2, ADJ=2). 1:2 and 1:3 each carry 2 matches (e).
  (5001, '1:2:2',  12, 1, 2,  2, 1,  1, 2, 'g5001', 'حُكْم',   'حكم',  'حكم',  'حكم',  FALSE, NULL, NULL),
  (5002, '1:3:2',  13, 1, 3,  2, 1,  1, 2, 'g5002', 'حُكْم',   'حكم',  'حكم',  'حكم',  FALSE, NULL, NULL),
  (5003, '1:2:3',  12, 1, 2,  3, 1,  1, 3, 'g5003', 'حَكِيم',  'حكيم', 'حكيم', 'حكيم', FALSE, NULL, NULL),
  (5004, '1:3:3',  13, 1, 3,  3, 1,  1, 3, 'g5004', 'حَكِيم',  'حكيم', 'حكيم', 'حكيم', FALSE, NULL, NULL),
  -- S601 (NULL lemma AND NULL root) — coverage (b).
  (6001, '2:25:2', 25, 2, 25, 2, 5,  1, 2, 'g6001', 'مَجْهُول', 'مجهول','مجهول','مجهول',FALSE, NULL, NULL),
  (6002, '3:1:3',  31, 3, 1,  3, 50, 1, 3, 'g6002', 'مَجْهُول', 'مجهول','مجهول','مجهول',FALSE, NULL, NULL),
  -- S602 multiple lemma/root candidates. 3:8 carries the 4th; the first three are in 2:1/2:25/3:1.
  (7001, '2:1:3',  21, 2, 1,  3, 2,  1, 3, 'g7001', 'عَلِمَ',  'علم',  'علم',  'علم',  FALSE, NULL, NULL),
  (7002, '2:25:3', 25, 2, 25, 3, 5,  1, 3, 'g7002', 'عَلِمَ',  'علم',  'علم',  'علم',  FALSE, NULL, NULL),
  (7003, '3:1:4',  31, 3, 1,  4, 50, 1, 4, 'g7003', 'عَلِمَ',  'علم',  'علم',  'علم',  FALSE, NULL, NULL),
  (7004, '3:8:3',  32, 3, 8,  3, 50, 1, 3, 'g7004', 'عَلِمَ',  'علم',  'علم',  'علم',  FALSE, NULL, NULL),
  -- L506/L507 compound-word slice (أَلَّا = أَن + لَا): word-level lemma لَا + head_pos SUB,
  -- but segment POS is SUB for أَن and NEG for لَا (coverage j).
  (8001, '2:25:6', 25, 2, 25, 6, 5,  1, 6, 'g8001', 'أَلَّا',  'الا',  'الا',  'الا',  FALSE, NULL, NULL),
  (8002, '3:1:5',  31, 3, 1,  5, 50, 1, 5, 'g8002', 'لَا',     'لا',   'لا',   'لا',   FALSE, NULL, NULL),
  -- Synthetic non-source word used only to regress same-lemma segment fan-out.
  (8101, '2:1:4',  21, 2, 1,  4, 2,  1, 4, 'g8101', 'لَفْظٌ-تَجْرِيبِيّ', 'لفظ-تجريبي', 'لفظ-تجريبي', 'لفظ-تجريبي', FALSE, NULL, NULL),
  -- Synthetic non-source pair used only to regress matched ayah-marker exclusion.
  (8201, '2:1:5',  21, 2, 1,  5, 2,  1, 5, 'g8201', 'وَسْم-تَجْرِيبِيّ', 'وسم-تجريبي', 'وسم-تجريبي', 'وسم-تجريبي', FALSE, NULL, NULL),
  (8202, '2:1:6',  21, 2, 1,  6, 2,  1, 6, 'g8202', 'MARKER_FIXTURE', 'MARKER_FIXTURE', 'MARKER_FIXTURE', 'MARKER_FIXTURE', TRUE, NULL, NULL);

-- ----------------------------------------------------------------------
-- Part-of-speech tags referenced by quran_word_morphology.head_pos (FK).
-- Authentic labels (only the codes the slice morphology uses).
-- ----------------------------------------------------------------------
INSERT INTO quran_pos_tags
  (code, arabic_label, english_label, category, sort_order)
VALUES
  ('N',   'اسم', 'Noun',      'noun', 1),
  ('V',   'فعل', 'Verb',      'verb', 2),
  ('ADJ', 'صفة', 'Adjective', 'noun', 4),
  ('SUB', 'حرف', 'Subordinator', 'particle', 5),
  ('NEG', 'نفي', 'Negation',  'particle', 6);

-- ----------------------------------------------------------------------
-- Word morphology. Each row binds a readable word to word-level morphology data.
-- Lemma reads are segment-driven via quran_word_morphology_segments. Word-level
-- morphology may still supply stem_id/root_id for related reads where segments
-- do not carry stem_id. The lemma owned root is quran_lemmas.root_id (table above).
-- ----------------------------------------------------------------------
INSERT INTO quran_word_morphology
  (quran_word_id, location, head_pos, segment_count, root_id, lemma_id, stem_id, is_verb, verb_tense, verb_voice, case_feature, head_features_json)
VALUES
  -- L500/S600 (root R700): N=10, V=1 → dominant N, otherTypes=1. 11 rows.
  (3001, '1:1:1',  'N', 1, 700, 500, 600, FALSE, NULL, NULL, NULL, NULL),
  (3002, '1:1:2',  'N', 1, 700, 500, 600, FALSE, NULL, NULL, NULL, NULL),
  (3010, '1:1:3',  'N', 1, 700, 500, 600, FALSE, NULL, NULL, NULL, NULL),
  (3011, '1:1:4',  'N', 1, 700, 500, 600, FALSE, NULL, NULL, NULL, NULL),
  (3003, '1:2:1',  'V', 1, 700, 500, 600, TRUE,  'perfect', 'active', NULL, NULL),
  (3004, '1:3:1',  'N', 1, 700, 500, 600, FALSE, NULL, NULL, NULL, NULL),
  (3005, '2:1:1',  'N', 1, 700, 500, 600, FALSE, NULL, NULL, NULL, NULL),
  (3006, '2:25:1', 'N', 1, 700, 500, 600, FALSE, NULL, NULL, NULL, NULL),
  (3007, '3:1:1',  'N', 1, 700, 500, 600, FALSE, NULL, NULL, NULL, NULL),
  (3008, '3:8:1',  'N', 1, 700, 500, 600, FALSE, NULL, NULL, NULL, NULL),
  (3009, '3:8:2',  'N', 1, 700, 500, 600, FALSE, NULL, NULL, NULL, NULL),
  -- L501/S605 (lemma owned root NULL; morphology root_id NULL too).
  (4001, '2:1:2',  'N', 1, NULL, 501, 605, FALSE, NULL, NULL, NULL, NULL),
  (4002, '3:1:2',  'N', 1, NULL, 501, 605, FALSE, NULL, NULL, NULL, NULL),
  -- L503/S604 multi-type exact tie: N=2 (earliest 5001@1:2:2), ADJ=2 (earliest 5003@1:2:3).
  (5001, '1:2:2',  'N',   1, NULL, 503, 604, FALSE, NULL, NULL, NULL, NULL),
  (5002, '1:3:2',  'N',   1, NULL, 503, 604, FALSE, NULL, NULL, NULL, NULL),
  (5003, '1:2:3',  'ADJ', 1, NULL, 503, 604, FALSE, NULL, NULL, NULL, NULL),
  (5004, '1:3:3',  'ADJ', 1, NULL, 503, 604, FALSE, NULL, NULL, NULL, NULL),
  -- S601 (NULL lemma AND NULL root).
  (6001, '2:25:2', 'N', 1, NULL, NULL, 601, FALSE, NULL, NULL, NULL, NULL),
  (6002, '3:1:3',  'N', 1, NULL, NULL, 601, FALSE, NULL, NULL, NULL, NULL),
  -- S602 multiple candidates: lemma L502×3 (7001-7003), L504×1 (7004);
  --                          root R701×3 (7001-7003), R702×1 (7004).
  (7001, '2:1:3',  'V', 1, 701, 502, 602, TRUE, 'perfect', 'active', NULL, NULL),
  (7002, '2:25:3', 'V', 1, 701, 502, 602, TRUE, 'perfect', 'active', NULL, NULL),
  (7003, '3:1:4',  'V', 1, 701, 502, 602, TRUE, 'perfect', 'active', NULL, NULL),
  (7004, '3:8:3',  'V', 1, 702, 504, 602, TRUE, 'perfect', 'active', NULL, NULL),
  -- L506: compound أَلَّا (word-level lemma لَا, head_pos SUB) + standalone لَا (NEG).
  (8001, '2:25:6', 'SUB', 2, NULL, 506, NULL, FALSE, NULL, NULL, NULL, NULL),
  (8002, '3:1:5',  'NEG', 1, NULL, 506, NULL, FALSE, NULL, NULL, NULL, NULL),
  -- Synthetic word-level row intentionally has no lemma; segment rows below supply L508.
  (8101, '2:1:4',  'N',   2, NULL, NULL, 606, FALSE, NULL, NULL, NULL, NULL),
  -- Synthetic marker-exclusion rows intentionally have no word-level lemma; segment rows below supply L509.
  (8201, '2:1:5',  'N',   1, NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL),
  (8202, '2:1:6',  'N',   1, NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL);

-- Segment rows drive lemma occurrence counts, type distribution, and ayah type
-- filters. For simple words, one segment mirrors morphology head_pos + lemma_id.
-- Compound أَلَّا carries two segments with distinct lemma_id + pos (coverage j).
INSERT INTO quran_word_morphology_segments
  (quran_word_id, segment_location, segment_number, kind, pos,
   form_buckwalter, arabic_render_source, features_raw, lemma_id, root_id)
SELECT
  m.quran_word_id,
  m.location || ':1',
  1::smallint,
  'STEM',
  m.head_pos,
  'fixture',
  'fixture',
  'POS=' || m.head_pos,
  m.lemma_id,
  m.root_id
FROM quran_word_morphology m
WHERE m.lemma_id IS NOT NULL
  AND m.quran_word_id NOT IN (8001, 8101);

INSERT INTO quran_word_morphology_segments
  (quran_word_id, segment_location, segment_number, kind, pos,
   form_buckwalter, arabic_render_source, features_raw, lemma_id, root_id)
VALUES
  (8001, '2:25:6:1', 1, 'STEM', 'SUB', '>an',  'fixture', 'POS=SUB', 507, NULL),
  (8001, '2:25:6:2', 2, 'STEM', 'NEG', 'lA',   'fixture', 'POS=NEG', 506, NULL),
  (8101, '2:1:4:1', 1, 'STEM', 'N', 'fixture', 'fixture', 'POS=N;fixture=same-lemma-fanout', 508, NULL),
  (8101, '2:1:4:2', 2, 'STEM', 'N', 'fixture', 'fixture', 'POS=N;fixture=same-lemma-fanout', 508, NULL),
  (8201, '2:1:5:1', 1, 'STEM', 'N', 'fixture', 'fixture', 'POS=N;fixture=marker-exclusion-real-word', 509, NULL),
  (8202, '2:1:6:1', 1, 'STEM', 'N', 'fixture', 'fixture', 'POS=N;fixture=marker-exclusion-ayah-marker', 509, NULL);

-- ----------------------------------------------------------------------
-- Unique word identities for the words sub-views (simple + tashkeel). One row
-- per distinct display form. Linked back to quran_words below.
-- ----------------------------------------------------------------------
INSERT INTO quran_words_unique_tashkeel
  (id, text_uthmani, text_uthmani_simple, text_imlaei_simple,
   occurrences_count, ayahs_count, surahs_count,
   first_quran_word_id, first_location, first_surah_number, first_ayah_number,
   first_word_order_in_mushaf, first_page_number, first_line_number)
VALUES
  (31001, 'كَلِمَة',  'كلمة',  'كلمة',  10, 7, 3, 3001, '1:1:1',  1, 1,  3001, 1,  1),
  (31002, 'كَلَّمَ',   'كلم',   'كلم',   1,  1, 1, 3003, '1:2:1',  1, 2,  3003, 1,  1),
  (31003, 'نِعْمَة',  'نعمة',  'نعمة',  2,  2, 2, 4001, '2:1:2',  2, 1,  4001, 2,  1),
  (31004, 'حُكْم',   'حكم',   'حكم',   2,  2, 1, 5001, '1:2:2',  1, 2,  5001, 1,  1),
  (31005, 'حَكِيم',  'حكيم',  'حكيم',  2,  2, 1, 5003, '1:2:3',  1, 2,  5003, 1,  1),
  (31006, 'مَجْهُول', 'مجهول', 'مجهول', 2,  2, 2, 6001, '2:25:2', 2, 25, 6001, 5,  1),
  (31007, 'عَلِمَ',  'علم',   'علم',   4,  4, 3, 7001, '2:1:3',  2, 1,  7001, 2,  1),
  (31008, 'أَلَّا',  'الا',   'الا',   1,  1, 1, 8001, '2:25:6', 2, 25, 8001, 5,  1),
  (31009, 'لَا',     'لا',    'لا',    1,  1, 1, 8002, '3:1:5',  3, 1,  8002, 50, 1),
  (31010, 'لَفْظٌ-تَجْرِيبِيّ', 'لفظ-تجريبي', 'لفظ-تجريبي', 1, 1, 1, 8101, '2:1:4', 2, 1, 8101, 2, 1),
  (31011, 'وَسْم-تَجْرِيبِيّ', 'وسم-تجريبي', 'وسم-تجريبي', 1, 1, 1, 8201, '2:1:5', 2, 1, 8201, 2, 1),
  (31012, 'MARKER_FIXTURE', 'MARKER_FIXTURE', 'MARKER_FIXTURE', 1, 1, 1, 8202, '2:1:6', 2, 1, 8202, 2, 1);

INSERT INTO quran_words_unique_simple
  (id, word_key_imlaei_simple, text_uthmani, text_uthmani_simple, text_imlaei_simple, qpc_glyph,
   occurrences_count, ayahs_count, surahs_count,
   first_quran_word_id, first_location, first_surah_number, first_ayah_number,
   first_word_order_in_mushaf, first_page_number, first_line_number)
VALUES
  (32001, 'كلمة',  'كَلِمَة',  'كلمة',  'كلمة',  'g3001', 10, 7, 3, 3001, '1:1:1',  1, 1,  3001, 1,  1),
  (32002, 'كلم',   'كَلَّمَ',   'كلم',   'كلم',   'g3003', 1,  1, 1, 3003, '1:2:1',  1, 2,  3003, 1,  1),
  (32003, 'نعمة',  'نِعْمَة',  'نعمة',  'نعمة',  'g4001', 2,  2, 2, 4001, '2:1:2',  2, 1,  4001, 2,  1),
  (32004, 'حكم',   'حُكْم',   'حكم',   'حكم',   'g5001', 2,  2, 1, 5001, '1:2:2',  1, 2,  5001, 1,  1),
  (32005, 'حكيم',  'حَكِيم',  'حكيم',  'حكيم',  'g5003', 2,  2, 1, 5003, '1:2:3',  1, 2,  5003, 1,  1),
  (32006, 'مجهول', 'مَجْهُول', 'مجهول', 'مجهول', 'g6001', 2,  2, 2, 6001, '2:25:2', 2, 25, 6001, 5,  1),
  (32007, 'علم',   'عَلِمَ',  'علم',   'علم',   'g7001', 4,  4, 3, 7001, '2:1:3',  2, 1,  7001, 2,  1),
  (32008, 'الا',   'أَلَّا',  'الا',   'الا',   'g8001', 1,  1, 1, 8001, '2:25:6', 2, 25, 8001, 5,  1),
  (32009, 'لا',    'لَا',     'لا',    'لا',    'g8002', 1,  1, 1, 8002, '3:1:5',  3, 1,  8002, 50, 1),
  (32010, 'لفظ-تجريبي', 'لَفْظٌ-تَجْرِيبِيّ', 'لفظ-تجريبي', 'لفظ-تجريبي', 'g8101', 1, 1, 1, 8101, '2:1:4', 2, 1, 8101, 2, 1),
  (32011, 'وسم-تجريبي', 'وَسْم-تَجْرِيبِيّ', 'وسم-تجريبي', 'وسم-تجريبي', 'g8201', 1, 1, 1, 8201, '2:1:5', 2, 1, 8201, 2, 1),
  (32012, 'MARKER_FIXTURE', 'MARKER_FIXTURE', 'MARKER_FIXTURE', 'MARKER_FIXTURE', 'g8202', 1, 1, 1, 8202, '2:1:6', 2, 1, 8202, 2, 1);

-- Link each readable word to its tashkeel + simple unique identity by display form.
UPDATE quran_words SET
    unique_tashkeel_word_id = src.tid,
    unique_simple_word_id   = src.sid
FROM (
    SELECT w.id AS wid,
           CASE w.text_uthmani
             WHEN 'كَلِمَة'   THEN 31001
             WHEN 'كَلَّمَ'    THEN 31002
             WHEN 'نِعْمَة'   THEN 31003
             WHEN 'حُكْم'    THEN 31004
             WHEN 'حَكِيم'   THEN 31005
             WHEN 'مَجْهُول'  THEN 31006
             WHEN 'عَلِمَ'   THEN 31007
             WHEN 'أَلَّا'   THEN 31008
             WHEN 'لَا'      THEN 31009
             WHEN 'لَفْظٌ-تَجْرِيبِيّ' THEN 31010
             WHEN 'وَسْم-تَجْرِيبِيّ' THEN 31011
             WHEN 'MARKER_FIXTURE' THEN 31012
           END AS tid,
           CASE w.text_uthmani
             WHEN 'كَلِمَة'   THEN 32001
             WHEN 'كَلَّمَ'    THEN 32002
             WHEN 'نِعْمَة'   THEN 32003
             WHEN 'حُكْم'    THEN 32004
             WHEN 'حَكِيم'   THEN 32005
             WHEN 'مَجْهُول'  THEN 32006
             WHEN 'عَلِمَ'   THEN 32007
             WHEN 'أَلَّا'   THEN 32008
             WHEN 'لَا'      THEN 32009
             WHEN 'لَفْظٌ-تَجْرِيبِيّ' THEN 32010
             WHEN 'وَسْم-تَجْرِيبِيّ' THEN 32011
             WHEN 'MARKER_FIXTURE' THEN 32012
           END AS sid
    FROM quran_words w
    WHERE w.id IN (3001,3002,3003,3004,3005,3006,3007,3008,3009,3010,3011,
                   4001,4002,5001,5002,5003,5004,6001,6002,
                   7001,7002,7003,7004,8001,8002,8101,8201,8202)
) AS src
WHERE quran_words.id = src.wid;

-- ======================================================================
-- Expected facts (for the story-level tests; do NOT assert by editing SQL):
--
-- L500 'كَلِمَة' (id=500): occurrences=11; types N=10,V=1 → dominant N,
--   otherTypes=1, distribution total=11; ayahs=7 ({1:1,1:2,1:3,2:1,2:25,3:1,3:8});
--   surahs=3 ({1,2,3}) → missing=111; simple=1 ({32001}) + the V maps to 32002?
--   NOTE: L500 words: 10×'كلمة'(32001) + 1×'كلم'(32002) → simple distinct=2,
--   tashkeel distinct=2 ({31001,31002}); owned root R700; related stems={600}=1;
--   1:1 carries 4 L500 matches.
--
-- L501 'نِعْمَة' (id=501): occurrences=2; owned root NULL → root column dash.
--
-- L503 'حُكْم' (id=503): occurrences=4; types N=2 (earliest 5001@1:2:2),
--   ADJ=2 (earliest 5003@1:2:3) → EXACT TIE, dominant N by earliest occurrence;
--   owned root NULL; related stems={604}.
--
-- L508 'لَفْظٌ-تَجْرِيبِيّ' (id=508): synthetic same-lemma segment fan-out;
--   one word carries two N segments for the same lemma, so segment occurrences=2
--   while ayah/surah counts remain 1; its word-level morphology row keeps lemma_id NULL
--   to prove segment-only matched words can still contribute unique words and stems.
--
-- L509 'وَسْم-مُؤَشِّر-تَجْرِيبِيّ' (id=509): synthetic matched-marker regression;
--   one real word and one ayah marker carry matching segments. Occurrences remain segment-based,
--   while simple/tashkeel word counts and words rows must include only the real word.
--
-- S600 'كَلَّمَ' (id=600): occurrences=11; dominant lemma L500 (11×, sole),
--   dominant root R700 (11×, sole); same type distribution as L500.
--
-- S601 'مَجْهُول' (id=601): occurrences=2; dominant lemma NULL, dominant root NULL.
--
-- S602 'عَلِمَ' (id=602): occurrences=4; dominant lemma L502 (3× vs L504 1×),
--   dominant root R701 (3× vs R702 1×) — independent rankings;
--   related lemmas={502,504}=2; 3:8 carries 1 S602 match (7004), 2:1/2:25/3:1
--   carry one each.
--
-- S604 'حَكَمَ' (id=604): occurrences=4; same multi-type tie as L503.
--
-- S606 'سَاق-تَجْرِيبِيّ' (id=606): synthetic related-stem fixture for L508.
-- ======================================================================

-- Reconcile catalog words_count to the word-level morphology readable-word count
-- where the resource is still word-level driven. Segment-driven lemma reads use
-- quran_word_morphology_segments; segment-only synthetic lemmas keep explicit counts.
UPDATE quran_lemmas l
SET words_count = sub.cnt
FROM (
    SELECT m.lemma_id AS rid, COUNT(*) AS cnt
    FROM quran_word_morphology m
    WHERE m.lemma_id IS NOT NULL
    GROUP BY m.lemma_id
) AS sub
WHERE l.id = sub.rid;

UPDATE quran_stems s
SET words_count = sub.cnt
FROM (
    SELECT m.stem_id AS rid, COUNT(*) AS cnt
    FROM quran_word_morphology m
    WHERE m.stem_id IS NOT NULL
    GROUP BY m.stem_id
) AS sub
WHERE s.id = sub.rid;

UPDATE quran_roots r
SET words_count = sub.cnt
FROM (
    SELECT m.root_id AS rid, COUNT(*) AS cnt
    FROM quran_word_morphology m
    WHERE m.root_id IS NOT NULL
    GROUP BY m.root_id
) AS sub
WHERE r.id = sub.rid;

UPDATE quran_roots r
SET distinct_lemmas_count = sub.cnt
FROM (
    SELECT m.root_id AS rid, COUNT(DISTINCT m.lemma_id) AS cnt
    FROM quran_word_morphology m
    WHERE m.root_id IS NOT NULL AND m.lemma_id IS NOT NULL
    GROUP BY m.root_id
) AS sub
WHERE r.id = sub.rid;
