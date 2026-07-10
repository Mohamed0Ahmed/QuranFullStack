-- Word Types Explorer fixture seed (Feature 019, Phase 2 skeleton).
-- Story-phase tests will extend this source-safe slice with concrete assertions.
-- No production Quran source data is mutated by this script.

INSERT INTO quran_surahs
  (surah_number, name_arabic, name_simple, name_transliteration, revelation_place, revelation_order, verses_count, bismillah_pre)
VALUES
  (1, 'الفاتحة', 'Al-Fatihah', 'Al-Fatihah', 'makkah', 5, 7, FALSE),
  (2, 'البقرة', 'Al-Baqarah', 'Al-Baqarah', 'madinah', 87, 286, TRUE),
  (3, 'آل عمران', 'Aal-E-Imran', 'Aal-E-Imran', 'madinah', 89, 200, TRUE)
ON CONFLICT DO NOTHING;

INSERT INTO quran_pos_tags (code, arabic_label, english_label, category, sort_order)
VALUES
  ('N', 'اسم', 'Noun', 'noun', 1),
  ('PN', 'اسم علم', 'Proper Noun', 'noun', 2),
  ('ADJ', 'صفة', 'Adjective', 'noun', 3),
  ('TIM', 'ظرف زمان', 'Temporal Adverb', 'noun', 33),
  ('V', 'فعل', 'Verb', 'verb', 4),
  ('P', 'حرف جر', 'Preposition', 'particle', 5),
  ('PRO', 'حرف نهي', 'Prohibition', 'particle', 6),
  ('INL', 'حروف مقطّعة', 'Quranic Initials', 'particle', 7),
  ('X', 'خارج النطاق', 'Fixture out of bucket', 'other', 99)
ON CONFLICT DO NOTHING;

INSERT INTO quran_mushaf_pages
  (page_number, first_surah_number, first_ayah_number, last_surah_number, last_ayah_number, lines_count)
VALUES
  (1, 1, 1, 1, 3, 3),
  (2, 2, 1, 2, 1, 1),
  (5, 2, 25, 2, 25, 1),
  (50, 3, 1, 3, 8, 2)
ON CONFLICT DO NOTHING;

INSERT INTO quran_ayahs
  (id, surah_number, ayah_number, verse_key, text_uthmani, words_count_source, words_count_real, page_from, page_to, juz_number, hizb_number, rub_number)
VALUES
  (190011, 1, 1, '1:1', 'بِسْمِ ٱللَّهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ', 4, 4, 1, 1, NULL, NULL, NULL),
  (190012, 1, 2, '1:2', 'ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَٰلَمِينَ', 4, 4, 1, 1, NULL, NULL, NULL),
  (190021, 2, 1, '2:1', 'الٓمٓ', 1, 1, 2, 2, NULL, NULL, NULL),
  (190025, 2, 25, '2:25', 'وَبَشِّرِ ٱلَّذِينَ ءَامَنُوا۟ وَعَمِلُوا۟ ٱلصَّٰلِحَٰتِ', 6, 6, 5, 5, NULL, NULL, NULL),
  (190032, 3, 8, '3:8', 'رَبَّنَا لَا تُزِغْ قُلُوبَنَا بَعْدَ إِذْ هَدَيْتَنَا', 7, 7, 50, 50, NULL, NULL, NULL)
ON CONFLICT DO NOTHING;

INSERT INTO quran_roots (id, root_text, root_buckwalter, words_count, distinct_lemmas_count, first_word_order_in_mushaf)
VALUES
  (190700, 'ك ل م', 'klm', 3, 1, 1903001),
  (190701, 'ع ل م', 'Elm', 3, 1, 1907001)
ON CONFLICT DO NOTHING;

INSERT INTO quran_lemmas (id, lemma_text, lemma_buckwalter, root_id, words_count, first_word_order_in_mushaf)
VALUES
  (190500, 'كَلِمَة', 'kalimap', 190700, 3, 1903001),
  (190501, 'لَا', 'lA', NULL, 1, 1908001),
  (190502, 'عِلْم', 'Elm', 190701, 3, 1907001),
  (190503, 'الٓمٓ', 'Alm', NULL, 1, 1903005),
  (190504, 'خَارِج', 'fixture', NULL, 1, 1909001)
ON CONFLICT DO NOTHING;

INSERT INTO quran_stems (id, stem_text, words_count, first_word_order_in_mushaf)
VALUES
  (190600, 'كَلَّمَ', 3, 1903001),
  (190601, 'لَا', 1, 1908001),
  (190602, 'عَلِمَ', 3, 1907001),
  (190603, 'الٓمٓ', 1, 1903005)
ON CONFLICT DO NOTHING;

INSERT INTO quran_words
  (id, location, ayah_id, surah_number, ayah_number, word_number, page_number, line_number, line_word_order, qpc_glyph, text_uthmani, text_uthmani_simple, text_imlaei_simple, word_key_imlaei_simple, is_ayah_marker, unique_tashkeel_word_id, unique_simple_word_id)
VALUES
  (1903001, '1:1:1', 190011, 1, 1, 1, 1, 1, 1, 'g1903001', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (1903002, '1:1:2', 190011, 1, 1, 2, 1, 1, 2, 'g1903002', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (1903011, '1:1:3', 190011, 1, 1, 3, 1, 1, 3, 'g1903011', 'مُثَل', 'مثل', 'مثل', 'مثل', FALSE, NULL, NULL),
  (1903003, '1:2:1', 190012, 1, 2, 1, 1, 1, 1, 'g1903003', 'كَلِمَة', 'كلمة', 'كلمة', 'كلمة', FALSE, NULL, NULL),
  (1903004, '1:2:2', 190012, 1, 2, 2, 1, 1, 2, 'g1903004', 'ۚ', 'ۚ', 'ۚ', 'ۚ', TRUE, NULL, NULL),
  (1903010, '1:2:3', 190012, 1, 2, 3, 1, 1, 3, 'g1903010', 'مُثَل', 'مثل', 'مثل', 'مثل', FALSE, NULL, NULL),
  (1903005, '2:1:1', 190021, 2, 1, 1, 2, 1, 1, 'g1903005', 'الٓمٓ', 'الم', 'الم', 'الم', FALSE, NULL, NULL),
  (1907001, '2:25:1', 190025, 2, 25, 1, 5, 1, 1, 'g1907001', 'عَلِمَ', 'علم', 'علم', 'علم', FALSE, NULL, NULL),
  (1907002, '2:25:2', 190025, 2, 25, 2, 5, 1, 2, 'g1907002', 'عُلِمَ', 'علم', 'علم', 'علم', FALSE, NULL, NULL),
  (1907003, '3:8:1', 190032, 3, 8, 1, 50, 1, 1, 'g1907003', 'عَلِمَ', 'علم', 'علم', 'علم', FALSE, NULL, NULL),
  (1908001, '3:8:2', 190032, 3, 8, 2, 50, 1, 2, 'g1908001', 'لَا', 'لا', 'لا', 'لا', FALSE, NULL, NULL),
  (1909001, '3:8:3', 190032, 3, 8, 3, 50, 1, 3, 'g1909001', 'خَارِج', 'خارج', 'خارج', 'خارج', FALSE, NULL, NULL)
ON CONFLICT DO NOTHING;

INSERT INTO quran_words_unique_tashkeel
  (id, text_uthmani, text_uthmani_simple, text_imlaei_simple, occurrences_count, ayahs_count, surahs_count, first_quran_word_id, first_location, first_surah_number, first_ayah_number, first_word_order_in_mushaf, first_page_number, first_line_number)
VALUES
  (191001, 'كَلِمَة', 'كلمة', 'كلمة', 3, 2, 1, 1903001, '1:1:1', 1, 1, 1903001, 1, 1),
  (191002, 'الٓمٓ', 'الم', 'الم', 1, 1, 1, 1903005, '2:1:1', 2, 1, 1903005, 2, 1),
  (191003, 'عَلِمَ', 'علم', 'علم', 2, 2, 2, 1907001, '2:25:1', 2, 25, 1907001, 5, 1),
  (191004, 'عُلِمَ', 'علم', 'علم', 1, 1, 1, 1907002, '2:25:2', 2, 25, 1907002, 5, 1),
  (191005, 'لَا', 'لا', 'لا', 1, 1, 1, 1908001, '3:8:2', 3, 8, 1908001, 50, 1),
  (191006, 'خَارِج', 'خارج', 'خارج', 1, 1, 1, 1909001, '3:8:3', 3, 8, 1909001, 50, 1),
  (191007, 'ۚ', 'ۚ', 'ۚ', 1, 1, 1, 1903004, '1:2:2', 1, 2, 1903004, 1, 1),
  (191008, 'مُثَل', 'مثل', 'مثل', 2, 2, 1, 1903011, '1:1:3', 1, 1, 1903011, 1, 1)
ON CONFLICT DO NOTHING;

INSERT INTO quran_words_unique_simple
  (id, word_key_imlaei_simple, text_uthmani, text_uthmani_simple, text_imlaei_simple, qpc_glyph, occurrences_count, ayahs_count, surahs_count, first_quran_word_id, first_location, first_surah_number, first_ayah_number, first_word_order_in_mushaf, first_page_number, first_line_number)
VALUES
  (192001, 'كلمة', 'كَلِمَة', 'كلمة', 'كلمة', 'g1903001', 3, 2, 1, 1903001, '1:1:1', 1, 1, 1903001, 1, 1),
  (192002, 'الم', 'الٓمٓ', 'الم', 'الم', 'g1903005', 1, 1, 1, 1903005, '2:1:1', 2, 1, 1903005, 2, 1),
  (192003, 'علم', 'عَلِمَ', 'علم', 'علم', 'g1907001', 3, 2, 2, 1907001, '2:25:1', 2, 25, 1907001, 5, 1),
  (192004, 'لا', 'لَا', 'لا', 'لا', 'g1908001', 1, 1, 1, 1908001, '3:8:2', 3, 8, 1908001, 50, 1),
  (192005, 'خارج', 'خَارِج', 'خارج', 'خارج', 'g1909001', 1, 1, 1, 1909001, '3:8:3', 3, 8, 1909001, 50, 1),
  (192006, 'ۚ', 'ۚ', 'ۚ', 'ۚ', 'g1903004', 1, 1, 1, 1903004, '1:2:2', 1, 2, 1903004, 1, 1),
  (192007, 'مثل', 'مُثَل', 'مثل', 'مثل', 'g1903011', 2, 2, 1, 1903011, '1:1:3', 1, 1, 1903011, 1, 1)
ON CONFLICT DO NOTHING;

UPDATE quran_words SET unique_tashkeel_word_id = 191001, unique_simple_word_id = 192001 WHERE id IN (1903001, 1903002, 1903003);
UPDATE quran_words SET unique_tashkeel_word_id = 191008, unique_simple_word_id = 192007 WHERE id IN (1903010, 1903011);
UPDATE quran_words SET unique_tashkeel_word_id = 191007, unique_simple_word_id = 192006 WHERE id = 1903004;
UPDATE quran_words SET unique_tashkeel_word_id = 191002, unique_simple_word_id = 192002 WHERE id = 1903005;
UPDATE quran_words SET unique_tashkeel_word_id = 191003, unique_simple_word_id = 192003 WHERE id IN (1907001, 1907003);
UPDATE quran_words SET unique_tashkeel_word_id = 191004, unique_simple_word_id = 192003 WHERE id = 1907002;
UPDATE quran_words SET unique_tashkeel_word_id = 191005, unique_simple_word_id = 192004 WHERE id = 1908001;
UPDATE quran_words SET unique_tashkeel_word_id = 191006, unique_simple_word_id = 192005 WHERE id = 1909001;

INSERT INTO quran_word_morphology
  (quran_word_id, location, head_pos, segment_count, root_id, lemma_id, stem_id, is_verb, verb_tense, verb_voice, case_feature, head_features_json)
VALUES
  (1903001, '1:1:1', 'N', 1, 190700, 190500, 190600, FALSE, NULL, NULL, 'nominative', NULL),
  (1903002, '1:1:2', 'PN', 1, 190700, 190500, 190600, FALSE, NULL, NULL, 'genitive', NULL),
  (1903003, '1:2:1', 'ADJ', 1, 190700, 190500, 190600, FALSE, NULL, NULL, NULL, NULL),
  (1903004, '1:2:2', 'N', 1, NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL),
  (1903005, '2:1:1', 'INL', 1, NULL, 190503, 190603, FALSE, NULL, NULL, NULL, NULL),
  (1907001, '2:25:1', 'V', 1, 190701, 190502, 190602, TRUE, 'past', 'active', NULL, NULL),
  (1907002, '2:25:2', 'V', 1, 190701, 190502, 190602, TRUE, 'present', 'passive', NULL, NULL),
  (1907003, '3:8:1', 'V', 1, 190701, 190502, 190602, TRUE, 'imperative', 'active', NULL, NULL),
  (1908001, '3:8:2', 'PRO', 1, NULL, 190501, 190601, FALSE, NULL, NULL, NULL, NULL),
  (1909001, '3:8:3', 'X', 1, NULL, 190504, NULL, FALSE, NULL, NULL, NULL, NULL),
  (1903011, '1:1:3', 'N', 1, NULL, NULL, NULL, FALSE, NULL, NULL, 'nominative', NULL),
  (1903010, '1:2:3', 'V', 1, NULL, NULL, NULL, TRUE, NULL, NULL, NULL, NULL)
ON CONFLICT DO NOTHING;
