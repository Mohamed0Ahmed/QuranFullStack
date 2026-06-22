-- ======================================================================
-- Unique Words Explorer — representative content slice (fixture seed, US2)
-- Feature 014 · read-only · deterministic · offline
-- ----------------------------------------------------------------------
-- Loaded by UniqueWordsTestFixture into a fresh Testcontainers Postgres
-- instance AFTER EnsureCreatedAsync. Covers only the rows the Unique Words
-- list/search/sort/paging/validation tests assert on; it is intentionally
-- NOT the full DB. Canonical Quranic Uthmani text is used verbatim — no
-- text is invented or altered.
--
-- Surah set: 1, 2, 114 (only those referenced by first-occurrence rows).
--
-- Tashkeel unique words (deterministic id := first_quran_word_id):
--   id    text_uthmani        simple/imlaei      occ ayah sura first
--   1001  بِسْمِ               بسم                3   3    3    1:1:1
--   1002  ٱللَّهِ               الله               5   5    5    1:1:2   (high occ for sort test)
--   2003  ءَامَنُوا۟              آمنوا              2   2    2    2:25:3   (variant-fold: query امنوا matches stored آمنوا)
--   1003  ٱلرَّحْمَٰنِ             الرحمن             1   1    1    1:1:3   (alpha-early)
--   1004  ٱلرَّحِيمِ             الرحيم             1   1    1    1:1:4   (alpha-early)
--   60041 قُلْ                قل                 1   1    1    114:1:1 (alpha-early, late mushaf order)
--
-- Simple unique words share ids with tashkeel for the same first word, plus
-- the same fold-matching word_key_imlaei_simple.
-- ======================================================================

-- Surahs (only the ones referenced) ------------------------------------
INSERT INTO quran_surahs
  (surah_number, name_arabic, name_simple, name_transliteration, revelation_place, revelation_order, verses_count, bismillah_pre)
VALUES
  (1,   'الفاتحة', 'Al-Fatihah', 'Al-Fatihah', 'makkah',  5,  7, FALSE),
  (2,   'البقرة',  'Al-Baqarah', 'Al-Baqarah', 'madinah', 87, 286, TRUE),
  (114, 'الناس',   'An-Nas',     'An-Nas',     'makkah',  21, 6, TRUE);

-- Ayahs referenced by first occurrences --------------------------------
INSERT INTO quran_ayahs
  (id, surah_number, ayah_number, verse_key, text_uthmani, words_count_source, words_count_real, page_from, page_to, juz_number, hizb_number, rub_number)
VALUES
  (11,    1,   1, '1:1',   'بِسْمِ ٱللَّهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ',          4, 4, 1,   1,   NULL, NULL, NULL),
  (25,    2,  25, '2:25',  'وَبَشِّرِ ٱلَّذِينَ ءَامَنُوا۟ وَعَمِلُوا۟ ٱلصَّٰلِحَٰتِ', 4, 5, 5, 5, NULL, NULL, NULL),
  (1141, 114, 1, '114:1', 'قُلْ أَعُوذُ بِرَبِّ ٱلنَّاسِ',              2, 3, 604, 604, NULL, NULL, NULL);

-- Mushaf pages referenced by quran_words.page_number (FK) ---------------
INSERT INTO quran_mushaf_pages
  (page_number, first_surah_number, first_ayah_number, last_surah_number, last_ayah_number, lines_count)
VALUES
  (1,   1,   1, 1,   2,  2),
  (5,   2,  25, 2,  26,  2),
  (604, 114, 1, 114, 1, 1);

-- Quran words (first occurrences only; minimal slice) -------------------
-- text_uthmani is canonical; text_uthmani_simple/text_imlaei_simple are
-- the unvoweled forms used by search; word_key_imlaei_simple is the simple
-- identity key. unique_*_word_id links are backfilled after unique rows exist.
INSERT INTO quran_words
  (id, location, ayah_id, surah_number, ayah_number, word_number, page_number, line_number, line_word_order, qpc_glyph, text_uthmani, text_uthmani_simple, text_imlaei_simple, word_key_imlaei_simple, is_ayah_marker, unique_tashkeel_word_id, unique_simple_word_id)
VALUES
  (1001, '1:1:1',   11,   1,  1, 1, 1,   1, 1, 'g1001',  'بِسْمِ',       'بسم',      'بسم',      'بسم',      FALSE, NULL, NULL),
  (1002, '1:1:2',   11,   1,  1, 2, 1,   1, 2, 'g1002',  'ٱللَّهِ',       'الله',     'الله',     'الله',     FALSE, NULL, NULL),
  (1003, '1:1:3',   11,   1,  1, 3, 1,   1, 3, 'g1003',  'ٱلرَّحْمَٰنِ',    'الرحمن',   'الرحمن',   'الرحمن',   FALSE, NULL, NULL),
  (1004, '1:1:4',   11,   1,  1, 4, 1,   1, 4, 'g1004',  'ٱلرَّحِيمِ',    'الرحيم',   'الرحيم',   'الرحيم',   FALSE, NULL, NULL),
  (2003, '2:25:3',  25,   2, 25, 3, 5,   1, 3, 'g2003',  'ءَامَنُوا۟',     'ءامنوا',   'آمنوا',    'امنوا',    FALSE, NULL, NULL),
  (60041,'114:1:1', 1141, 114, 1, 1, 604, 1, 1, 'g60041', 'قُلْ',         'قل',       'قل',       'قل',       FALSE, NULL, NULL);

-- Unique tashkeel words (deterministic id := first_quran_word_id) -------
-- Counts are precomputed values for the slice; missingSurahsCount = 114 - surahs_count.
INSERT INTO quran_words_unique_tashkeel
  (id, text_uthmani, text_uthmani_simple, text_imlaei_simple,
   occurrences_count, ayahs_count, surahs_count,
   first_quran_word_id, first_location, first_surah_number, first_ayah_number,
   first_word_order_in_mushaf, first_page_number, first_line_number)
VALUES
  (1001,  'بِسْمِ',      'بسم',     'بسم',     3, 3, 3, 1001,  '1:1:1',   1,   1, 1001,  1, 1),
  (1002,  'ٱللَّهِ',      'الله',    'الله',    5, 5, 5, 1002,  '1:1:2',   1,   1, 1002,  1, 1),
  (1003,  'ٱلرَّحْمَٰنِ',  'الرحمن',  'الرحمن',  1, 1, 1, 1003,  '1:1:3',   1,   1, 1003,  1, 1),
  (1004,  'ٱلرَّحِيمِ',  'الرحيم',  'الرحيم',  1, 1, 1, 1004,  '1:1:4',   1,   1, 1004,  1, 1),
  (2003,  'ءَامَنُوا۟',    'ءامنوا',  'آمنوا',   2, 2, 2, 2003,  '2:25:3',  2,  25, 2003,  5, 1),
  (60041, 'قُلْ',        'قل',      'قل',      1, 1, 1, 60041, '114:1:1', 114, 1, 60041, 604, 1);

-- Unique simple words (same ids; word_key_imlaei_simple is the identity) -
INSERT INTO quran_words_unique_simple
  (id, word_key_imlaei_simple, text_uthmani, text_uthmani_simple, text_imlaei_simple, qpc_glyph,
   occurrences_count, ayahs_count, surahs_count,
   first_quran_word_id, first_location, first_surah_number, first_ayah_number,
   first_word_order_in_mushaf, first_page_number, first_line_number)
VALUES
  (1001,  'بسم',     'بِسْمِ',      'بسم',     'بسم',     'g1001',  3, 3, 3, 1001,  '1:1:1',   1,   1, 1001,  1, 1),
  (1002,  'الله',    'ٱللَّهِ',      'الله',    'الله',    'g1002',  5, 5, 5, 1002,  '1:1:2',   1,   1, 1002,  1, 1),
  (1003,  'الرحمن',  'ٱلرَّحْمَٰنِ',  'الرحمن',  'الرحمن',  'g1003',  1, 1, 1, 1003,  '1:1:3',   1,   1, 1003,  1, 1),
  (1004,  'الرحيم',  'ٱلرَّحِيمِ',  'الرحيم',  'الرحيم',  'g1004',  1, 1, 1, 1004,  '1:1:4',   1,   1, 1004,  1, 1),
  (2003,  'امنوا',   'ءَامَنُوا۟',    'ءامنوا',  'آمنوا',   'g2003',  2, 2, 2, 2003,  '2:25:3',  2,  25, 2003,  5, 1),
  (60041, 'قل',      'قُلْ',        'قل',      'قل',      'g60041', 1, 1, 1, 60041, '114:1:1', 114, 1, 60041, 604, 1);

-- Link source words to their unique identity rows ----------------------
UPDATE quran_words SET
    unique_tashkeel_word_id = src.tid,
    unique_simple_word_id = src.sid
FROM (
    SELECT w.id AS wid, t.id AS tid, s.id AS sid
    FROM quran_words w
    JOIN quran_words_unique_tashkeel t ON t.first_quran_word_id = w.id
    JOIN quran_words_unique_simple    s ON s.first_quran_word_id = w.id
) AS src
WHERE quran_words.id = src.wid;
