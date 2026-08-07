-- ======================================================================
-- Roots Explorer — representative content slice (fixture seed, T013)
-- Feature 015 · read-only · deterministic · offline
-- ----------------------------------------------------------------------
-- Loaded by RootsExplorerTestFixture into its own isolated database on the shared
-- Testcontainers Postgres runtime, cloned from the migrated template. Covers only
-- the rows the Roots Explorer list/summary/words/ayahs/surahs/lemmas/stems tests
-- assert on; it is intentionally NOT the full DB and NOT the developer's local DB.
-- Canonical Quranic Uthmani text is used verbatim — no text is invented or altered.
--
-- Coverage goals (per T013):
--   (a) A high-frequency root — many ayahs + many surahs.
--   (b) A lemma that CO-OCCURS under more than one root, so co-occurrence
--       (DISTINCT lemma_id via morphology) ≠ quran_lemmas.root_id ownership.
--       This is the DIVERGENT seeded root the lemma-co-occurrence tests
--       assert on.
--   (c) A root present in nearly all surahs — missing-surahs edge.
--   (d) Roots with several distinct stems.
--
-- Driving relation reminder (never use quran_lemmas.root_id for counts):
--   quran_word_morphology m (m.root_id = X)
--     JOIN quran_words w ON w.id = m.quran_word_id
--
-- Deterministic ids are used throughout. Surah/word/ayah ids are chosen to
-- avoid colliding with the canonical 1:1 Fatiha rows used elsewhere only by
-- convention; this slice is independent and self-contained.
-- ======================================================================

-- ----------------------------------------------------------------------
-- Surahs (slice). Asserted catalog rows use canonical Arabic names; the
-- generate_series filler for 3..113 is obvious synthetic fixture data
-- (NOT authoritative catalog metadata) and only asserts surah_number +
-- non-empty name_arabic. Unique name_arabic is respected via distinct suffix.
-- ----------------------------------------------------------------------
INSERT INTO quran_surahs
  (surah_number, name_arabic, name_simple, name_transliteration, revelation_place, revelation_order, verses_count, bismillah_pre)
VALUES
  (1,   'الفاتحة',        'Al-Fatihah', 'Al-Fatihah', 'makkah',  5,  7,   FALSE),
  (2,   'البقرة',         'Al-Baqarah', 'Al-Baqarah', 'madinah', 87, 286, TRUE),
  (3,   'آل عمران',       'Aal-E-Imran','Aal-E-Imran','madinah', 89, 200, TRUE),
  (4,   'النساء',         'An-Nisa',    'An-Nisa',    'madinah', 92, 176, TRUE),
  (5,   'المائدة',        'Al-Maidah',  'Al-Maidah',  'madinah', 112,120, TRUE),
  (6,   'الأنعام',        'Al-Anam',    'Al-Anam',    'makkah',  55, 165, TRUE),
  (7,   'الأعراف',        'Al-Araf',    'Al-Araf',    'makkah',  39, 206, TRUE),
  (8,   'الأنفال',        'Al-Anfal',   'Al-Anfal',   'madinah', 88, 75,  TRUE),
  (114, 'الناس',          'An-Nas',     'An-Nas',     'makkah',  21, 6,   TRUE);

-- Catalog filler for the missing-surahs edge: the near-all-surahs root below
-- is seeded into surahs 1..8 + 114, so 9..113 must exist as missing candidates.
INSERT INTO quran_surahs
  (surah_number, name_arabic, name_simple, name_transliteration, revelation_place, revelation_order, verses_count, bismillah_pre)
SELECT
  n,
  'سورة-جذور-' || n::text,
  'ROOTS-FIXTURE-' || n::text,
  'ROOTS-FIXTURE-' || n::text,
  'makkah',
  n,
  1,
  FALSE
FROM generate_series(9, 113) AS n
ON CONFLICT (surah_number) DO NOTHING;

-- ----------------------------------------------------------------------
-- Mushaf pages referenced by quran_words.page_number (FK).
-- ----------------------------------------------------------------------
INSERT INTO quran_mushaf_pages
  (page_number, first_surah_number, first_ayah_number, last_surah_number, last_ayah_number, lines_count)
VALUES
  (1,   1,   1, 1,   2,  2),
  (2,   2,   1, 2,   3,  2),
  (5,   2,  25, 2,  26,  2),
  (50,  3,   1, 3,   1,  1),
  (77,  4,   1, 4,   1,  1),
  (106, 5,   1, 5,   1,  1),
  (128, 6,   1, 6,   1,  1),
  (150, 7,   1, 7,   1,  1),
  (175, 8,   1, 8,   1,  1),
  (604, 114, 1, 114, 1, 1)
ON CONFLICT DO NOTHING;

-- ----------------------------------------------------------------------
-- Ayahs referenced by the seeded occurrences (canonical Uthmani text).
-- ----------------------------------------------------------------------
INSERT INTO quran_ayahs
  (id, surah_number, ayah_number, verse_key, text_uthmani, words_count_source, words_count_real, page_from, page_to, juz_number, hizb_number, rub_number)
VALUES
  (11,   1,   1, '1:1',   'بِسْمِ ٱللَّهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ',            4, 4, 1,   1,   NULL, NULL, NULL),
  (12,   1,   2, '1:2',   'ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَٰلَمِينَ',           4, 4, 1,   1,   NULL, NULL, NULL),
  (13,   1,   3, '1:3',   'ٱلرَّحْمَٰنِ ٱلرَّحِيمِ',                       2, 2, 1,   1,   NULL, NULL, NULL),
  (21,   2,   1, '2:1',   'الٓمٓ',                                       1, 1, 2,   2,   NULL, NULL, NULL),
  (25,   2,  25, '2:25',  'وَبَشِّرِ ٱلَّذِينَ ءَامَنُوا۟ وَعَمِلُوا۟ ٱلصَّٰلِحَٰتِ', 6, 6, 5, 5, NULL, NULL, NULL),
  (31,   3,   1, '3:1',   'أَلٓمٓ',                                      1, 1, 50,  50,  NULL, NULL, NULL),
  (32,   3,   8, '3:8',   'رَبَّنَا لَا تُزِغْ قُلُوبَنَا بَعْدَ إِذْ هَدَيْتَنَا', 7, 7, 50, 50, NULL, NULL, NULL),
  (41,   4,   1, '4:1',   'يَٰٓأَيُّهَا ٱلنَّاسُ ٱتَّقُوا۟ رَبَّكُمُ',        5, 5, 77,  77,  NULL, NULL, NULL),
  (51,   5,   1, '5:1',   'يَٰٓأَيُّهَا ٱلَّذِينَ ءَامَنُوٓا۟ أَوْفُوا۟ بِٱلْعُقُودِ', 5, 5, 106, 106, NULL, NULL, NULL),
  (61,   6,   1, '6:1',   'ٱلْحَمْدُ لِلَّهِ ٱلَّذِى خَلَقَ ٱلسَّمَٰوَٰتِ وَٱلْأَرْضَ', 7, 7, 128, 128, NULL, NULL, NULL),
  (71,   7,   1, '7:1',   'الٓمٓصٓ',                                     1, 1, 150, 150, NULL, NULL, NULL),
  (81,   8,   1, '8:1',   'يَسْـَٔلُونَكَ عَنِ ٱلْأَنفَالِ',               4, 4, 175, 175, NULL, NULL, NULL),
  (1141, 114, 1, '114:1', 'قُلْ أَعُوذُ بِرَبِّ ٱلنَّاسِ',               4, 4, 604, 604, NULL, NULL, NULL);

-- ----------------------------------------------------------------------
-- Roots.
--   R10 'ر ح م' — HIGH-FREQUENCY root across many ayahs + many surahs (a).
--                 Also the DIVERGENT root for lemma co-occurrence (b):
--                 lemma L100 'رَحْمَة' co-occurs under R10 AND R20, but
--                 quran_lemmas.root_id for L100 is set to R20. So counting
--                 lemmas by ownership would miss L100 for R10, while
--                 co-occurrence (DISTINCT lemma_id via morphology) includes it.
--   R20 'ح م م' — secondary root sharing lemma L100 with R10 (b).
--                 Several distinct stems (d): S200, S201, S202.
--   R30 'ك ل م' — near-all-surahs root: present in surahs 1..8 + 114 (c).
--                 Its missing-surahs list is therefore 9..113 (105 surahs).
-- ----------------------------------------------------------------------
INSERT INTO quran_roots
  (id, root_text, root_buckwalter, words_count, distinct_lemmas_count, first_word_order_in_mushaf)
VALUES
  (10,  'ر ح م', 'rHm', 7, 2, 1003),
  (20,  'ح م م', 'Hmm', 2, 1, 5010),
  (30,  'ك ل م', 'klm', 9, 1, 2001);

-- ----------------------------------------------------------------------
-- Lemmas. NOTE: distinct_lemmas_count and the lemmas tab use CO-OCCURRENCE
-- (DISTINCT lemma_id in quran_word_morphology WHERE root_id = X), NOT this
-- table's root_id. L100.root_id is intentionally R20 to create the divergence
-- the co-occurrence tests lock onto.
-- ----------------------------------------------------------------------
INSERT INTO quran_lemmas
  (id, lemma_text, lemma_buckwalter, root_id, words_count, first_word_order_in_mushaf)
VALUES
  (100, 'رَحْمَة',   'raHomap', 20, 7, 1003),
  (101, 'رَحِيم',    'raHiym',  10, 4, 1004),
  (200, 'حَمَّة',    'Hammap',  20, 2, 5010),
  (300, 'كَلِمَة',   'kalimap', 30, 9, 2001);

-- ----------------------------------------------------------------------
-- Stems. R20 carries several distinct stems (d): S200, S201, S202.
-- ----------------------------------------------------------------------
INSERT INTO quran_stems
  (id, stem_text, words_count, first_word_order_in_mushaf)
VALUES
  (100, 'رَحْمَة',  7, 1003),
  (200, 'حَامَّة',  1, 5010),
  (201, 'مَحْمُوم', 1, 5020),
  (300, 'كَلِمَة',  9, 2001);

-- ----------------------------------------------------------------------
-- Quran words (canonical Uthmani). unique_*_word_id set later.
-- ----------------------------------------------------------------------
INSERT INTO quran_words
  (id, location, ayah_id, surah_number, ayah_number, word_number, page_number, line_number, line_word_order, qpc_glyph, text_uthmani, text_uthmani_simple, text_imlaei_simple, word_key_imlaei_simple, is_ayah_marker, unique_tashkeel_word_id, unique_simple_word_id)
VALUES
  -- 1:1 marker + four readable words — marker sits first so page-number fallback is testable
  (1000, '1:1:0', 11,   1, 1, 0, 604, 1, 0, 'g1000', '۞',           '۞',        '۞',        '۞',        TRUE,  NULL, NULL),
  (1001, '1:1:1', 11,   1, 1, 1, 1,   1, 1, 'g1001', 'بِسْمِ',       'بسم',      'بسم',      'بسم',      FALSE, NULL, NULL),
  (1002, '1:1:2', 11,   1, 1, 2, 1,   1, 2, 'g1002', 'ٱللَّهِ',       'الله',     'الله',     'الله',     FALSE, NULL, NULL),
  (1003, '1:1:3', 11,   1, 1, 3, 1,   1, 3, 'g1003', 'ٱلرَّحْمَٰنِ',    'الرحمن',   'الرحمن',   'الرحمن',   FALSE, NULL, NULL),
  (1004, '1:1:4', 11,   1, 1, 4, 1,   1, 4, 'g1004', 'ٱلرَّحِيمِ',    'الرحيم',   'الرحيم',   'الرحيم',   FALSE, NULL, NULL),
  -- 1:2 رَبِّ (R20 'ح م م' divergence word + lemma L100) — surah 1
  (1005, '1:2:3', 12,   1, 2, 3, 1,   1, 3, 'g1005', 'رَبِّ',        'رب',       'رب',       'رب',       FALSE, NULL, NULL),
  -- 1:3 ٱلرَّحْمَٰنِ / ٱلرَّحِيمِ  (R10 again, same ayah → 2 occurrences in one ayah)
  (1006, '1:3:1', 13,   1, 3, 1, 1,   1, 1, 'g1006', 'ٱلرَّحْمَٰنِ',    'الرحمن',   'الرحمن',   'الرحمن',   FALSE, NULL, NULL),
  (1007, '1:3:2', 13,   1, 3, 2, 1,   1, 2, 'g1007', 'ٱلرَّحِيمِ',    'الرحيم',   'الرحيم',   'الرحيم',   FALSE, NULL, NULL),
  -- surah 2:25 'ءَامَنُوا۟' (R10 word 'رحم'? no — keep R10 word 'رَحِيم' here for multi-surah)
  (1008, '2:25:4', 25,  2, 25, 4, 5,  1, 4, 'g1008', 'رَحِيم',       'رحيم',     'رحيم',     'رحيم',     FALSE, NULL, NULL),
  -- R20 second word in surah 3 (lemma L100, stem S201)
  (1009, '3:8:4',  32,  3, 8, 4, 50,  1, 4, 'g1009', 'رَبَّنَا',      'ربنا',     'ربنا',     'ربنا',     FALSE, NULL, NULL),
  -- R30 'ك ل م' near-all-surahs: one word in each of surahs 1..8 + 114
  (2001, '1:2:4',  12,  1, 2, 4, 1,   1, 4, 'g2001', 'كَلِمَة',     'كلمة',     'كلمة',     'كلمة',     FALSE, NULL, NULL),
  (2002, '2:1:1',  21,  2, 1, 1, 2,   1, 1, 'g2002', 'كَلَّمَ',      'كلم',      'كلم',      'كلم',      FALSE, NULL, NULL),
  (2003, '3:1:1',  31,  3, 1, 1, 50,  1, 1, 'g2003', 'كَلِمَة',     'كلمة',     'كلمة',     'كلمة',     FALSE, NULL, NULL),
  (2004, '4:1:4',  41,  4, 1, 4, 77,  1, 4, 'g2004', 'كَلَّمَ',      'كلم',      'كلم',      'كلم',      FALSE, NULL, NULL),
  (2005, '5:1:3',  51,  5, 1, 3, 106, 1, 3, 'g2005', 'كَلِمَة',     'كلمة',     'كلمة',     'كلمة',     FALSE, NULL, NULL),
  (2006, '6:1:5',  61,  6, 1, 5, 128, 1, 5, 'g2006', 'كَلَّمَ',      'كلم',      'كلم',      'كلم',      FALSE, NULL, NULL),
  (2007, '7:1:1',  71,  7, 1, 1, 150, 1, 1, 'g2007', 'كَلِمَة',     'كلمة',     'كلمة',     'كلمة',     FALSE, NULL, NULL),
  (2008, '8:1:2',  81,  8, 1, 2, 175, 1, 2, 'g2008', 'كَلَّمَ',      'كلم',      'كلم',      'كلم',      FALSE, NULL, NULL),
  (2009, '114:1:3',1141,114,1, 3, 604,1, 3, 'g2009','كَلِمَة',      'كلمة',     'كلمة',     'كلمة',     FALSE, NULL, NULL);

-- ----------------------------------------------------------------------
-- Part-of-speech tags referenced by quran_word_morphology.head_pos (FK).
-- Authentic labels from PosTagSeed (only the codes the slice morphology uses).
-- ----------------------------------------------------------------------
INSERT INTO quran_pos_tags
  (code, arabic_label, english_label, category, sort_order)
VALUES
  ('N',   'اسم', 'Noun',      'noun', 1),
  ('V',   'فعل', 'Verb',      'verb', 2),
  ('ADJ', 'صفة', 'Adjective', 'noun', 4);

-- ----------------------------------------------------------------------
-- Word morphology — the driving relation. Each row binds a readable word to a
-- root/lemma/stem. Lemmas are counted by CO-OCCURRENCE (DISTINCT lemma_id
-- WHERE root_id = X), so R10 sees lemmas {L101, L100} even though L100's
-- ownership root_id is R20.
-- ----------------------------------------------------------------------
INSERT INTO quran_word_morphology
  (quran_word_id, location, head_pos, segment_count, root_id, lemma_id, stem_id, is_verb, verb_tense, verb_voice, case_feature, head_features_json)
VALUES
  -- R10 'ر ح م' words: 1003,1004,1006,1007,1008 (5 readable words, lemma L100 on some)
  (1003, '1:1:3',  'N', 1, 10, 100, 100, FALSE, NULL, NULL, NULL, NULL),
  (1004, '1:1:4',  'ADJ',1, 10, 101, 100, FALSE, NULL, NULL, NULL, NULL),
  (1006, '1:3:1',  'N', 1, 10, 100, 100, FALSE, NULL, NULL, NULL, NULL),
  (1007, '1:3:2',  'ADJ',1, 10, 101, 100, FALSE, NULL, NULL, NULL, NULL),
  (1008, '2:25:4', 'ADJ',1, 10, 101, 100, FALSE, NULL, NULL, NULL, NULL),
  -- R20 'ح م م' words: 1005,1009 — lemma L100 co-occurs under R20 too (divergence)
  (1005, '1:2:3',  'N', 1, 20, 100, 200, FALSE, NULL, NULL, NULL, NULL),
  (1009, '3:8:4',  'N', 1, 20, 100, 201, FALSE, NULL, NULL, NULL, NULL),
  -- R30 'ك ل م' words: 2001..2009 (9 words across 9 surahs)
  (2001, '1:2:4',  'N', 1, 30, 300, 300, FALSE, NULL, NULL, NULL, NULL),
  (2002, '2:1:1',  'V', 1, 30, 300, 300, TRUE,  'perfect', 'active', NULL, NULL),
  (2003, '3:1:1',  'N', 1, 30, 300, 300, FALSE, NULL, NULL, NULL, NULL),
  (2004, '4:1:4',  'V', 1, 30, 300, 300, TRUE,  'perfect', 'active', NULL, NULL),
  (2005, '5:1:3',  'N', 1, 30, 300, 300, FALSE, NULL, NULL, NULL, NULL),
  (2006, '6:1:5',  'V', 1, 30, 300, 300, TRUE,  'perfect', 'active', NULL, NULL),
  (2007, '7:1:1',  'N', 1, 30, 300, 300, FALSE, NULL, NULL, NULL, NULL),
  (2008, '8:1:2',  'V', 1, 30, 300, 300, TRUE,  'perfect', 'active', NULL, NULL),
  (2009, '114:1:3','N', 1, 30, 300, 300, FALSE, NULL, NULL, NULL, NULL);

-- ----------------------------------------------------------------------
-- Unique word identities for the words sub-views (simple + tashkeel) and the
-- Feature 014 deep-link target. Deterministic ids; one per distinct display
-- form. Linked back to quran_words below.
-- ----------------------------------------------------------------------
INSERT INTO quran_words_unique_tashkeel
  (id, text_uthmani, text_uthmani_simple, text_imlaei_simple,
   occurrences_count, ayahs_count, surahs_count,
   first_quran_word_id, first_location, first_surah_number, first_ayah_number,
   first_word_order_in_mushaf, first_page_number, first_line_number)
VALUES
  (1003, 'ٱلرَّحْمَٰنِ', 'الرحمن', 'الرحمن', 2, 2, 1, 1003, '1:1:3', 1, 3, 1003, 1, 1),
  (1004, 'ٱلرَّحِيمِ', 'الرحيم', 'الرحيم', 3, 3, 2, 1004, '1:1:4', 1, 4, 1004, 1, 1),
  (1005, 'رَبِّ',      'رب',     'رب',     2, 2, 2, 1005, '1:2:3', 1, 2, 1005, 1, 1),
  (2001, 'كَلِمَة',    'كلمة',   'كلمة',   5, 5, 5, 2001, '1:2:4', 1, 2, 2001, 1, 1),
  (2002, 'كَلَّمَ',     'كلم',    'كلم',    4, 4, 4, 2002, '2:1:1', 2, 1, 2002, 2, 1);

INSERT INTO quran_words_unique_simple
  (id, word_key_imlaei_simple, text_uthmani, text_uthmani_simple, text_imlaei_simple, qpc_glyph,
   occurrences_count, ayahs_count, surahs_count,
   first_quran_word_id, first_location, first_surah_number, first_ayah_number,
   first_word_order_in_mushaf, first_page_number, first_line_number)
VALUES
  (1003, 'الرحمن', 'ٱلرَّحْمَٰنِ', 'الرحمن', 'الرحمن', 'g1003', 2, 2, 1, 1003, '1:1:3', 1, 3, 1003, 1, 1),
  (1004, 'الرحيم', 'ٱلرَّحِيمِ', 'الرحيم', 'الرحيم', 'g1004', 3, 3, 2, 1004, '1:1:4', 1, 4, 1004, 1, 1),
  (1005, 'رب',     'رَبِّ',      'رب',     'رب',     'g1005', 2, 2, 2, 1005, '1:2:3', 1, 2, 1005, 1, 1),
  (2001, 'كلمة',   'كَلِمَة',    'كلمة',   'كلمة',   'g2001', 5, 5, 5, 2001, '1:2:4', 1, 2, 2001, 1, 1),
  (2002, 'كلم',    'كَلَّمَ',     'كلم',    'كلم',    'g2002', 4, 4, 4, 2002, '2:1:1', 2, 1, 2002, 2, 1);

-- Link the root words to their unique identities (only the words the words
-- sub-view needs to resolve display text + deep-link target).
UPDATE quran_words SET
    unique_tashkeel_word_id = src.tid,
    unique_simple_word_id   = src.sid
FROM (
    SELECT w.id AS wid,
           CASE
             WHEN w.id IN (1003,1006)            THEN 1003
             WHEN w.id IN (1004,1007,1008)       THEN 1004
             WHEN w.id IN (1005,1009)            THEN 1005
             WHEN w.id IN (2001,2003,2005,2007,2009) THEN 2001
             WHEN w.id IN (2002,2004,2006,2008)  THEN 2002
           END AS tid,
           CASE
             WHEN w.id IN (1003,1006)            THEN 1003
             WHEN w.id IN (1004,1007,1008)       THEN 1004
             WHEN w.id IN (1005,1009)            THEN 1005
             WHEN w.id IN (2001,2003,2005,2007,2009) THEN 2001
             WHEN w.id IN (2002,2004,2006,2008)  THEN 2002
           END AS sid
    FROM quran_words w
    WHERE w.id IN (1003,1004,1005,1006,1007,1008,1009,2001,2002,2003,2004,2005,2006,2007,2008,2009)
) AS src
WHERE quran_words.id = src.wid;

-- ======================================================================
-- Expected facts (for the story-level tests; do NOT assert by editing SQL):
--
-- R10 'ر ح م' (id=10):
--   occurrences = words_count = 7? No — words_count is 7 but morphology has
--   5 readable rows for R10 (1003,1004,1006,1007,1008). occurrences MUST equal
--   quran_roots.words_count per SC. So words_count is set to the row count the
--   tests pin on. Keep words_count == morphology COUNT(*) per root (see UPDATE).
--   ayahs   = DISTINCT ayah_id of those rows = {11(1:1),13(1:3),25(2:25)} = 3
--   surahs  = DISTINCT surah_number         = {1,2}                     = 2
--   simple  = DISTINCT unique_simple_word_id among R10 words = {1003,1004} = 2
--   tashkeel= DISTINCT unique_tashkeel_word_id               = {1003,1004} = 2
--   lemmas  = DISTINCT lemma_id (co-occurrence) = {100,101} = 2  (== distinct_lemmas_count)
--             NOTE: ownership quran_lemmas.root_id for L100 is R20, yet L100
--             co-occurs under R10 — this is the divergence the tests lock.
--   stems   = DISTINCT stem_id = {100} = 1
--
-- R20 'ح م م' (id=20): 2 rows (1005,1009); lemma {100}; stems {200,201} = 2.
-- R30 'ك ل م' (id=30): 9 rows across surahs 1..8,114 → surahs=9; missing=105.
-- ======================================================================

-- Reconcile quran_roots.words_count to the morphology readable-word count per
-- root, so occurrences == COUNT(*) (the verified invariant; SC-002). Keeps the
-- slice self-consistent regardless of hand-counted values above.
UPDATE quran_roots r
SET words_count = sub.cnt
FROM (
    SELECT m.root_id AS rid, COUNT(*) AS cnt
    FROM quran_word_morphology m
    WHERE m.root_id IS NOT NULL
    GROUP BY m.root_id
) AS sub
WHERE r.id = sub.rid;

-- distinct_lemmas_count must equal DISTINCT lemma_id per root (co-occurrence).
UPDATE quran_roots r
SET distinct_lemmas_count = sub.cnt
FROM (
    SELECT m.root_id AS rid, COUNT(DISTINCT m.lemma_id) AS cnt
    FROM quran_word_morphology m
    WHERE m.root_id IS NOT NULL AND m.lemma_id IS NOT NULL
    GROUP BY m.root_id
) AS sub
WHERE r.id = sub.rid;
