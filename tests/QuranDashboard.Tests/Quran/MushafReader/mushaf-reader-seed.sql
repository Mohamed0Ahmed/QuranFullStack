-- ======================================================================
-- Mushaf Reader — representative content slice (fixture seed, G2)
-- Feature 011 · read-only · deterministic · offline
-- ----------------------------------------------------------------------
-- Loaded by MushafReaderTestFixture into a fresh Testcontainers Postgres
-- instance AFTER EnsureCreatedAsync (see fixture for why not MigrateAsync). Covers only the rows the reader tests assert
-- on; it is intentionally NOT the full DB.
--
-- FK ordering note: quran_ayahs has nullable juz/hizb/rub FKs while
-- quran_juzs/hizbs/rubs require first/last_ayah_id. To break the cycle, ayahs
-- are inserted with NULL divisions first, the division rows are inserted next,
-- then the ayahs are updated with their division numbers.
-- ======================================================================

-- Surahs ----------------------------------------------------------------
INSERT INTO quran_surahs
  (surah_number, name_arabic, name_simple, name_transliteration, revelation_place, revelation_order, verses_count, bismillah_pre)
VALUES
  (1,   'الفاتحة', 'Al-Fatihah', 'Al-Fatihah', 'makkah',  5,  7, FALSE),
  (2,   'البقرة',  'Al-Baqarah', 'Al-Baqarah', 'madinah', 87, 286, TRUE),
  (114, 'الناس',   'An-Nas',     'An-Nas',     'makkah',  21, 6, TRUE);

-- Ayahs (divisions NULL first to satisfy the juz/hizb/rub FK cycle) -------
INSERT INTO quran_ayahs
  (id, surah_number, ayah_number, verse_key, text_uthmani, words_count_source, words_count_real, page_from, page_to, juz_number, hizb_number, rub_number)
VALUES
  (11,   1,   1, '1:1',   'بِسْمِ ٱللَّهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ',                4, 4, 1,   1,   NULL, NULL, NULL),
  (12,   1,   2, '1:2',   'ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَٰلَمِينَ',                  4, 4, 1,   1,   NULL, NULL, NULL),
  (25,   2,  25, '2:25',  'وَبَشِّرِ ٱلَّذِينَ ءَامَنُوا۟ وَعَمِلُوا۟ ٱلصَّٰلِحَٰتِ', 4, 5, 5,   5,   NULL, NULL, NULL),
  (26,   2,  26, '2:26',  'ٱللَّهُ لَا إِلَٰهَ إِلَّا هُوَ ٱلْحَىُّ ٱلْقَيُّومُ',     4, 5, 5,   5,   NULL, NULL, NULL),
  (1141, 114, 1, '114:1', 'قُلْ أَعُوذُ بِرَبِّ ٱلنَّاسِ',                       2, 3, 604, 604, NULL, NULL, NULL);

-- Division rows (FK first/last_ayah_id satisfied — ayahs exist above) -----
INSERT INTO quran_juzs
  (juz_number, verses_count, first_ayah_id, last_ayah_id, first_verse_key, last_verse_key)
VALUES (1, 7, 11, 26, '1:1', '2:26');

INSERT INTO quran_hizbs
  (hizb_number, juz_number, verses_count, first_ayah_id, last_ayah_id, first_verse_key, last_verse_key)
VALUES (1, 1, 7, 11, 26, '1:1', '2:26');

INSERT INTO quran_rubs
  (rub_number, hizb_number, verses_count, first_ayah_id, last_ayah_id, first_verse_key, last_verse_key)
VALUES
  (1, 1, 5, 11, 25, '1:1', '2:25'),
  (2, 1, 1, 26, 26, '2:26', '2:26');

-- Backfill ayah divisions now that juz/hizb/rub exist -------------------
UPDATE quran_ayahs SET juz_number = 1, hizb_number = 1, rub_number = 1 WHERE id IN (11, 12, 25);
UPDATE quran_ayahs SET juz_number = 1, hizb_number = 1, rub_number = 2 WHERE id = 26;

-- Mushaf pages ----------------------------------------------------------
INSERT INTO quran_mushaf_pages
  (page_number, first_surah_number, first_ayah_number, last_surah_number, last_ayah_number, lines_count)
VALUES
  (1,   1,   1, 1,   2,  2),
  (5,   2,  25, 2,  26,  2),
  (604, 114, 1, 114, 1, 1);

-- Quran words (readable + one ayah-end marker on page 5) ----------------
INSERT INTO quran_words
  (id, location, ayah_id, surah_number, ayah_number, word_number, page_number, line_number, line_word_order, qpc_glyph, text_uthmani, text_uthmani_simple, text_imlaei_simple, word_key_imlaei_simple, is_ayah_marker, unique_tashkeel_word_id, unique_simple_word_id)
VALUES
  -- page 1 / 1:1 (line 1)
  (1001, '1:1:1', 11, 1, 1, 1, 1,   1, 1, 'g1001', 'بِسْمِ',        'بسم',     'بسم',     'بسم',     FALSE, NULL, NULL),
  (1002, '1:1:2', 11, 1, 1, 2, 1,   1, 2, 'g1002', 'ٱللَّهِ',        'الله',    'الله',    'الله',    FALSE, NULL, NULL),
  (1003, '1:1:3', 11, 1, 1, 3, 1,   1, 3, 'g1003', 'ٱلرَّحْمَٰنِ',     'الرحمن',  'الرحمن',  'الرحمن',  FALSE, NULL, NULL),
  (1004, '1:1:4', 11, 1, 1, 4, 1,   1, 4, 'g1004', 'ٱلرَّحِيمِ',     'الرحيم',  'الرحيم',  'الرحيم',  FALSE, NULL, NULL),
  -- page 1 / 1:2 (line 2)
  (1005, '1:2:1', 12, 1, 2, 1, 1,   2, 1, 'g1005', 'ٱلْحَمْدُ',      'الحمد',   'الحمد',   'الحمد',   FALSE, NULL, NULL),
  (1006, '1:2:2', 12, 1, 2, 2, 1,   2, 2, 'g1006', 'لِلَّهِ',         'لله',     'لله',     'لله',     FALSE, NULL, NULL),
  (1007, '1:2:3', 12, 1, 2, 3, 1,   2, 3, 'g1007', 'رَبِّ',          'رب',      'رب',      'رب',      FALSE, NULL, NULL),
  (1008, '1:2:4', 12, 1, 2, 4, 1,   2, 4, 'g1008', 'ٱلْعَٰلَمِينَ',    'العالمين','العالمين','العالمين',FALSE, NULL, NULL),
  -- page 5 / 2:25 (line 1): 4 readable + 1 ayah-end marker
  (2001, '2:25:1', 25, 2, 25, 1, 5, 1, 1, 'g2001', 'وَبَشِّرِ',      'وبشر',    'وبشر',    'وبشر',    FALSE, NULL, NULL),
  (2002, '2:25:2', 25, 2, 25, 2, 5, 1, 2, 'g2002', 'ٱلَّذِينَ',      'الذين',   'الذين',   'الذين',   FALSE, NULL, NULL),
  (2003, '2:25:3', 25, 2, 25, 3, 5, 1, 3, 'g2003', 'ءَامَنُوا۟',      'امنوا',   'آمنوا',   'امنوا',   FALSE, NULL, NULL),
  (2004, '2:25:4', 25, 2, 25, 4, 5, 1, 4, 'g2004', 'وَعَمِلُوا۟',      'وعملوا',  'وعملوا',  'وعملوا',  FALSE, NULL, NULL),
  (2005, '2:25:5', 25, 2, 25, 5, 5, 1, 5, 'g2005', '۝',             '۝',       '۝',       '۝',       TRUE,  NULL, NULL),
  -- page 5 / 2:26 (line 2)
  (2006, '2:26:1', 26, 2, 26, 1, 5, 2, 1, 'g2006', 'ٱللَّهُ',        'الله',    'الله',    'الله',    FALSE, NULL, NULL),
  (2007, '2:26:2', 26, 2, 26, 2, 5, 2, 2, 'g2007', 'لَا',            'لا',      'لا',      'لا',      FALSE, NULL, NULL),
  -- page 604 / 114:1 (line 1)
  (60041, '114:1:1', 1141, 114, 1, 1, 604, 1, 1, 'g60041', 'قُلْ',   'قل',      'قل',      'قل',      FALSE, NULL, NULL),
  (60042, '114:1:2', 1141, 114, 1, 2, 604, 1, 2, 'g60042', 'أَعُوذُ','اعوذ',    'اعوذ',    'اعوذ',    FALSE, NULL, NULL);

-- Mushaf lines (FK first/last word satisfied — words inserted above) -----
INSERT INTO quran_mushaf_lines
  (page_number, line_number, line_type, is_centered, surah_number, first_word_id, last_word_id, words_count)
VALUES
  (1,   1, 'ayah', FALSE, NULL, 1001, 1004, 4),
  (1,   2, 'ayah', FALSE, NULL, 1005, 1008, 4),
  (5,   1, 'ayah', FALSE, NULL, 2001, 2005, 5),
  (5,   2, 'ayah', FALSE, NULL, 2006, 2007, 2),
  (604, 1, 'ayah', FALSE, NULL, 60041, 60042, 2);

-- ======================================================================
-- Ayah study slice (ayah 2:25 / id=25)
-- ======================================================================

-- Tafsir source ar-muyassar (content_coverage_count invariant = 6236) ----
INSERT INTO quran_tafsir_sources
  (source_key, language_code, language_name_ar, language_name_en, direction,
   display_name_ar, short_name_ar, display_name_en, short_name_en,
   contributor_type, resource_kind, tafsir_kind, content_coverage_count,
   package_file, source_file_original, sha256, file_size_bytes,
   license_status, provenance_status, manifest_metadata, imported_at_utc)
VALUES
  ('ar-muyassar', 'ar', 'العربية', 'Arabic', 'rtl',
   'التفسير الميسر', 'الميسر', 'Muyassar Tafsir', 'Muyassar',
   'author', 'tafsir', 'tafsir', 6236,
   'ar-muyassar.json', 'ar-muyassar.json', 'seed-sha256', 1,
   'unknown', 'unknown', '{}'::jsonb, '2026-06-17T00:00:00Z');

-- Grouped tafsir entry covering 2:25 + 2:26 (leader on 2:25) ------------
INSERT INTO quran_tafsir_entries
  (source_id, source_entry_key, leader_ayah_id, tafsir_text,
   covered_ayah_count, covered_ayah_keys, source_shape, text_hash)
VALUES
  ((SELECT id FROM quran_tafsir_sources WHERE source_key = 'ar-muyassar'),
   'ar-muyassar:2:25-26', 25, 'متن تجريبي للتفسير يغطي الآيتين 25 و 26 من سورة البقرة.',
   2, '["2:25","2:26"]'::jsonb, 'grouped_leader', 'seed-tafsir-1');

INSERT INTO quran_tafsir_ayah_entries
  (source_id, ayah_id, tafsir_entry_id, verse_key, source_value_kind, source_leader_verse_key, is_group_leader, sort_order)
VALUES
  ((SELECT id FROM quran_tafsir_sources WHERE source_key = 'ar-muyassar'),
   25,
   (SELECT id FROM quran_tafsir_entries WHERE source_entry_key = 'ar-muyassar:2:25-26'),
   '2:25', 'leader', '2:25', TRUE, 1),
  ((SELECT id FROM quran_tafsir_sources WHERE source_key = 'ar-muyassar'),
   26,
   (SELECT id FROM quran_tafsir_entries WHERE source_entry_key = 'ar-muyassar:2:25-26'),
   '2:26', 'member_pointer', '2:25', FALSE, 2);

-- Translation source en-sahih-international -----------------------------
INSERT INTO quran_translation_sources
  (source_key, language_code, language_name_en, language_name_ar, native_name,
   direction, translation_type, display_name_en, display_name_ar,
   contains_inline_footnotes, contains_html_markup, content_coverage_count)
VALUES
  ('en-sahih-international', 'en', 'English', 'الإنجليزية', 'English',
   'ltr', 'simple', 'Sahih International', 'صحيح الدولية',
   FALSE, FALSE, 6236);

INSERT INTO quran_translation_ayah_entries
  (source_id, ayah_id, verse_key, text)
VALUES
  ((SELECT id FROM quran_translation_sources WHERE source_key = 'en-sahih-international'),
   25, '2:25', 'And give good tidings to those who believe and do righteous deeds that for them are gardens.');

-- Full i3rab source muyassar (invariants: ar/html/unknown/...) ----------
INSERT INTO quran_full_i3rab_sources
  (source_key, display_name_ar, short_name_ar, display_name_en, short_name_en,
   language_code, direction, resource_kind, markup_format, has_quran_quotation_markup,
   content_coverage_count, package_file, source_file_original, sha256, file_size_bytes,
   license_status, provenance_status, usage_scope, manifest_metadata, imported_at_utc)
VALUES
  ('muyassar', 'الإعراب الميسر', 'الميسر', 'Muyassar I3rab', 'Muyassar',
   'ar', 'rtl', 'full_i3rab', 'html', FALSE,
   6236, 'muyassar.json', 'muyassar.json', 'seed-sha256', 1,
   'unknown', 'unknown', 'internal-only-until-cleared', '{}'::jsonb, '2026-06-17T00:00:00Z');

INSERT INTO quran_full_i3rab_entries
  (source_id, source_entry_key, leader_ayah_id, i3rab_html,
   covered_ayah_count, covered_ayah_keys, source_shape, text_hash)
VALUES
  ((SELECT id FROM quran_full_i3rab_sources WHERE source_key = 'muyassar'),
   'muyassar:2:25', 25, '<p>إعراب تجريبي للآية 2:25.</p>',
   1, '["2:25"]'::jsonb, 'flat', 'seed-i3rab-1');

INSERT INTO quran_full_i3rab_ayah_entries
  (source_id, ayah_id, entry_id, verse_key, source_value_kind, source_leader_verse_key, is_group_leader, sort_order)
VALUES
  ((SELECT id FROM quran_full_i3rab_sources WHERE source_key = 'muyassar'),
   25,
   (SELECT id FROM quran_full_i3rab_entries WHERE source_entry_key = 'muyassar:2:25'),
   '2:25', 'flat', '2:25', TRUE, 1);

-- ======================================================================
-- Word analysis slice (word 2:25:3 / quran_word_id=2003)
-- ======================================================================

-- POS tags referenced by morphology + segments --------------------------
INSERT INTO quran_pos_tags
  (code, arabic_label, english_label, category, sort_order, description)
VALUES
  ('V',    'فعل',         'Verb',         'verb',     10, 'Verb'),
  ('N',    'اسم',         'Noun',         'noun',     20, 'Noun'),
  ('PRON', 'ضمير',        'Pronoun',      'pronoun',  30, 'Pronoun');

-- Head morphology for word 2:25:3 ---------------------------------------
INSERT INTO quran_word_morphology
  (quran_word_id, location, head_pos, segment_count, root_id, lemma_id, stem_id,
   is_verb, verb_tense, verb_voice, case_feature, head_features_json)
VALUES
  (2003, '2:25:3', 'V', 2, NULL, NULL, NULL,
   TRUE, 'perfect', 'active', NULL, NULL);

-- Two ordered segments; segment 2 has empty form_arabic_normalized (fallback) --
INSERT INTO quran_word_morphology_segments
  (quran_word_id, segment_location, segment_number, kind, pos,
   form_buckwalter, form_arabic_normalized, arabic_render_tier, arabic_render_source,
   root_buckwalter, lemma_buckwalter, features_raw, features_json,
   i3rab_arabic, i3rab_rule_id, i3rab_status, i3rab_review_reason)
VALUES
  (2003, '2:25:3:1', 1, 'STEM', 'V',
   'amanuwA', 'آمَنُوا', 'primary', 'derived',
   NULL, NULL, 'POS=V', '[]'::jsonb,
   'فعل ماضٍ', NULL, 'approved', NULL),
  (2003, '2:25:3:2', 2, 'SUFFIX', 'PRON',
   'hum', NULL, NULL, 'derived',
   NULL, NULL, 'POS=PRON', '[]'::jsonb,
   NULL, NULL, 'unsupported', NULL);

-- Ordered identity rows (tashkeel + simple) -----------------------------
INSERT INTO quran_words_ordered_tashkeel
  (word_order_in_mushaf, quran_word_id, location, verse_key, surah_number, ayah_number,
   page_number, line_number, word_order_in_ayah, word_order_in_surah,
   text_uthmani, text_uthmani_simple, text_imlaei_simple,
   occurrences_count, ayahs_count, surahs_count)
VALUES
  (2003, 2003, '2:25:3', '2:25', 2, 25, 5, 1, 3, 3,
   'ءَامَنُوا۟', 'ءامنوا', 'آمنوا', 1, 1, 1);

INSERT INTO quran_words_ordered_simple
  (word_order_in_mushaf, quran_word_id, location, verse_key, surah_number, ayah_number,
   page_number, line_number, word_order_in_ayah, word_order_in_surah,
   word_key_imlaei_simple, text_uthmani_simple, text_imlaei_simple,
   occurrences_count, ayahs_count, surahs_count)
VALUES
  (2003, 2003, '2:25:3', '2:25', 2, 25, 5, 1, 3, 3,
   'امنوا', 'ءامنوا', 'آمنوا', 1, 1, 1);

-- Unique identity rows (auto-id; referenced back by quran_words) --------
INSERT INTO quran_words_unique_tashkeel
  (text_uthmani, text_uthmani_simple, text_imlaei_simple,
   occurrences_count, ayahs_count, surahs_count,
   first_quran_word_id, first_location, first_surah_number, first_ayah_number,
   first_word_order_in_mushaf, first_page_number, first_line_number)
VALUES
  ('ءَامَنُوا۟', 'ءامنوا', 'آمنوا', 1, 1, 1,
   2003, '2:25:3', 2, 25, 2003, 5, 1);

INSERT INTO quran_words_unique_simple
  (word_key_imlaei_simple, text_uthmani, text_uthmani_simple, text_imlaei_simple, qpc_glyph,
   occurrences_count, ayahs_count, surahs_count,
   first_quran_word_id, first_location, first_surah_number, first_ayah_number,
   first_word_order_in_mushaf, first_page_number, first_line_number)
VALUES
  ('امنوا', 'ءَامَنُوا۟', 'ءامنوا', 'آمنوا', 'g2003',
   1, 1, 1, 2003, '2:25:3', 2, 25, 2003, 5, 1);

-- Link the source word to its unique identity rows ----------------------
UPDATE quran_words
SET unique_tashkeel_word_id =
      (SELECT id FROM quran_words_unique_tashkeel WHERE first_word_order_in_mushaf = 2003),
    unique_simple_word_id =
      (SELECT id FROM quran_words_unique_simple WHERE first_word_order_in_mushaf = 2003)
WHERE id = 2003;

-- ======================================================================
-- Ayah similarity summary slice (Feature 012 US1)
-- ======================================================================

-- Similar ayah links for 2:25 (id=25):
--   outgoing  2:25 -> 2:26
--   incoming  1:2  -> 2:25
--   reverse   2:26 -> 2:25  (bidirectional dedup with outgoing)
-- Expected similarAyahCount for 2:25 = 2 distinct related ayahs (26, 12)
INSERT INTO quran_similar_ayah_links
  (id, source_ayah_id, target_ayah_id, score, coverage, matched_words_count, match_words)
VALUES
  (1, 25, 26, 80, 90, 3, '[]'::jsonb),
  (2, 12, 25, 70, 85, 2, '[]'::jsonb),
  (3, 26, 25, 80, 90, 3, '[]'::jsonb);

-- Mutashabihat groups containing 2:25:
--   group 1: occurrences on 2:25 + 2:26  (2 total in group)
--   group 2: occurrence on 2:25 only     (1 total in group)
-- Expected mutashabihatGroupCount = 2, mutashabihatOccurrenceCount = 3
INSERT INTO quran_mutashabihat_groups
  (id, source_group_id, representative_ayah_id, representative_word_from, representative_word_to,
   occurrence_count, distinct_ayah_count, distinct_surah_count, raw_source_counts)
VALUES
  (1, 90001, 25, 1, 2, 2, 2, 1, NULL),
  (2, 90002, 25, 3, 4, 1, 1, 1, NULL);

INSERT INTO quran_mutashabihat_occurrences
  (id, group_id, ayah_id, word_from, word_to, is_representative)
VALUES
  (1, 1, 25, 1, 2, TRUE),
  (2, 1, 26, 1, 1, FALSE),
  (3, 2, 25, 3, 4, TRUE);
