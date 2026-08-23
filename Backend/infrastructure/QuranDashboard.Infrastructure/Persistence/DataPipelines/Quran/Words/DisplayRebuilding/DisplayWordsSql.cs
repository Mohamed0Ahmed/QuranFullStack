namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.DisplayRebuilding;

internal static class DisplayWordsSql
{
    internal const string TashkeelIdentityCte = """
        WITH display_word_identity AS (
          SELECT U&'\0640\0653\06D6\06D7\06D8\06D9\06DA\06DB\06DC\06DE\06E9\200F'
                 AS ignored_tashkeel_marks
        )
        """;

    private const string ReadableBase = TashkeelIdentityCte + """
        , readable AS (
          SELECT w.id, w.location, w.ayah_id, w.surah_number, w.ayah_number,
                 w.word_number, w.page_number, w.line_number,
                 w.text_uthmani, w.text_uthmani_simple, w.text_imlaei_simple,
                 w.word_key_imlaei_simple, w.qpc_glyph,
                 btrim(translate(w.text_uthmani, identity.ignored_tashkeel_marks, ''))
                   AS tashkeel_identity,
                 a.verse_key
          FROM quran_words w
          JOIN quran_ayahs a ON a.id = w.ayah_id
          CROSS JOIN display_word_identity identity
          WHERE w.is_ayah_marker = false
        ),
        ranked AS (
          SELECT r.*,
                 ROW_NUMBER() OVER (ORDER BY id) AS word_order_in_mushaf,
                 ROW_NUMBER() OVER (PARTITION BY surah_number ORDER BY id) AS word_order_in_surah,
                 ROW_NUMBER() OVER (PARTITION BY ayah_id ORDER BY word_number) AS word_order_in_ayah
          FROM readable r
        )
        """;

    internal const string InsertOrderedTashkeel = ReadableBase + """
        , stats_tashkeel AS (
          SELECT tashkeel_identity,
                 COUNT(*) AS occurrences_count,
                 COUNT(DISTINCT ayah_id) AS ayahs_count,
                 COUNT(DISTINCT surah_number) AS surahs_count
          FROM ranked
          GROUP BY tashkeel_identity
        )
        INSERT INTO quran_words_ordered_tashkeel (
          word_order_in_mushaf, quran_word_id, location, verse_key,
          surah_number, ayah_number, page_number, line_number,
          word_order_in_ayah, word_order_in_surah,
          text_uthmani, text_uthmani_simple, text_imlaei_simple,
          occurrences_count, ayahs_count, surahs_count
        )
        SELECT
          r.word_order_in_mushaf,
          r.id,
          r.location,
          r.verse_key,
          r.surah_number,
          r.ayah_number,
          r.page_number,
          r.line_number,
          r.word_order_in_ayah,
          r.word_order_in_surah,
          r.text_uthmani,
          r.text_uthmani_simple,
          r.text_imlaei_simple,
          s.occurrences_count,
          s.ayahs_count,
          s.surahs_count
        FROM ranked r
        JOIN stats_tashkeel s ON s.tashkeel_identity = r.tashkeel_identity
        """;

    internal const string InsertOrderedSimple = ReadableBase + """
        , stats_simple AS (
          SELECT word_key_imlaei_simple,
                 COUNT(*) AS occurrences_count,
                 COUNT(DISTINCT ayah_id) AS ayahs_count,
                 COUNT(DISTINCT surah_number) AS surahs_count
          FROM ranked
          GROUP BY word_key_imlaei_simple
        )
        INSERT INTO quran_words_ordered_simple (
          word_order_in_mushaf, quran_word_id, location, verse_key,
          surah_number, ayah_number, page_number, line_number,
          word_order_in_ayah, word_order_in_surah,
          word_key_imlaei_simple, text_uthmani_simple, text_imlaei_simple,
          occurrences_count, ayahs_count, surahs_count
        )
        SELECT
          r.word_order_in_mushaf,
          r.id,
          r.location,
          r.verse_key,
          r.surah_number,
          r.ayah_number,
          r.page_number,
          r.line_number,
          r.word_order_in_ayah,
          r.word_order_in_surah,
          r.word_key_imlaei_simple,
          r.text_uthmani_simple,
          r.text_imlaei_simple,
          s.occurrences_count,
          s.ayahs_count,
          s.surahs_count
        FROM ranked r
        JOIN stats_simple s ON s.word_key_imlaei_simple = r.word_key_imlaei_simple
        """;

    internal const string InsertUniqueTashkeel = ReadableBase + """
        , stats_tashkeel AS (
          SELECT tashkeel_identity,
                 COUNT(*) AS occurrences_count,
                 COUNT(DISTINCT ayah_id) AS ayahs_count,
                 COUNT(DISTINCT surah_number) AS surahs_count
          FROM ranked
          GROUP BY tashkeel_identity
        ),
        first_occ AS (
          SELECT DISTINCT ON (r.tashkeel_identity)
            r.tashkeel_identity,
            r.text_uthmani,
            r.text_uthmani_simple,
            r.text_imlaei_simple,
            r.id AS first_quran_word_id,
            r.location AS first_location,
            r.surah_number AS first_surah_number,
            r.ayah_number AS first_ayah_number,
            r.word_order_in_mushaf AS first_word_order_in_mushaf,
            r.page_number AS first_page_number,
            r.line_number AS first_line_number
          FROM ranked r
          ORDER BY r.tashkeel_identity, r.word_order_in_mushaf
        )
        INSERT INTO quran_words_unique_tashkeel (
          id, text_uthmani, text_uthmani_simple, text_imlaei_simple,
          occurrences_count, ayahs_count, surahs_count,
          first_quran_word_id, first_location, first_surah_number, first_ayah_number,
          first_word_order_in_mushaf, first_page_number, first_line_number
        )
        SELECT
          f.first_quran_word_id,
          f.text_uthmani,
          f.text_uthmani_simple,
          f.text_imlaei_simple,
          s.occurrences_count,
          s.ayahs_count,
          s.surahs_count,
          f.first_quran_word_id,
          f.first_location,
          f.first_surah_number,
          f.first_ayah_number,
          f.first_word_order_in_mushaf,
          f.first_page_number,
          f.first_line_number
        FROM first_occ f
        JOIN stats_tashkeel s ON s.tashkeel_identity = f.tashkeel_identity
        ORDER BY f.first_word_order_in_mushaf, f.text_uthmani
        """;

    internal const string InsertUniqueSimple = ReadableBase + """
        , stats_simple AS (
          SELECT word_key_imlaei_simple,
                 COUNT(*) AS occurrences_count,
                 COUNT(DISTINCT ayah_id) AS ayahs_count,
                 COUNT(DISTINCT surah_number) AS surahs_count
          FROM ranked
          GROUP BY word_key_imlaei_simple
        ),
        first_occ AS (
          SELECT DISTINCT ON (r.word_key_imlaei_simple)
            r.word_key_imlaei_simple,
            r.text_uthmani,
            r.text_uthmani_simple,
            r.text_imlaei_simple,
            r.qpc_glyph,
            r.id AS first_quran_word_id,
            r.location AS first_location,
            r.surah_number AS first_surah_number,
            r.ayah_number AS first_ayah_number,
            r.word_order_in_mushaf AS first_word_order_in_mushaf,
            r.page_number AS first_page_number,
            r.line_number AS first_line_number
          FROM ranked r
          ORDER BY r.word_key_imlaei_simple, r.word_order_in_mushaf
        )
        INSERT INTO quran_words_unique_simple (
          id, word_key_imlaei_simple, text_uthmani, text_uthmani_simple, text_imlaei_simple, qpc_glyph,
          occurrences_count, ayahs_count, surahs_count,
          first_quran_word_id, first_location, first_surah_number, first_ayah_number,
          first_word_order_in_mushaf, first_page_number, first_line_number
        )
        SELECT
          f.first_quran_word_id,
          f.word_key_imlaei_simple,
          f.text_uthmani,
          f.text_uthmani_simple,
          f.text_imlaei_simple,
          f.qpc_glyph,
          s.occurrences_count,
          s.ayahs_count,
          s.surahs_count,
          f.first_quran_word_id,
          f.first_location,
          f.first_surah_number,
          f.first_ayah_number,
          f.first_word_order_in_mushaf,
          f.first_page_number,
          f.first_line_number
        FROM first_occ f
        JOIN stats_simple s ON s.word_key_imlaei_simple = f.word_key_imlaei_simple
        ORDER BY f.first_word_order_in_mushaf, f.word_key_imlaei_simple
        """;

    internal const string TruncateDerivedTables = """
        TRUNCATE
          quran_words_ordered_tashkeel,
          quran_words_ordered_simple,
          quran_words_unique_tashkeel,
          quran_words_unique_simple
        RESTART IDENTITY
        """;

    internal const string NullQuranWordLinks = """
        UPDATE quran_words
        SET unique_tashkeel_word_id = NULL,
            unique_simple_word_id = NULL
        """;

    internal const string UpdateUniqueTashkeelLinks = TashkeelIdentityCte + """
        UPDATE quran_words AS w
        SET unique_tashkeel_word_id = u.id
        FROM quran_words_unique_tashkeel AS u
        CROSS JOIN display_word_identity identity
        WHERE w.is_ayah_marker = false
          AND btrim(translate(u.text_uthmani, identity.ignored_tashkeel_marks, ''))
              = btrim(translate(w.text_uthmani, identity.ignored_tashkeel_marks, ''))
        """;

    internal const string UpdateUniqueSimpleLinks = """
        UPDATE quran_words AS w
        SET unique_simple_word_id = u.id
        FROM quran_words_unique_simple AS u
        WHERE w.is_ayah_marker = false
          AND u.word_key_imlaei_simple = w.word_key_imlaei_simple
        """;

    internal const string CheckOrdCountOrderedTashkeel = """
        SELECT COUNT(*)::int FROM quran_words_ordered_tashkeel
        """;

    internal const string CheckOrdCountOrderedSimple = """
        SELECT COUNT(*)::int FROM quran_words_ordered_simple
        """;

    internal const string CheckOrdReadable = """
        SELECT COUNT(*)::int FROM quran_words WHERE is_ayah_marker = false
        """;

    internal const string CheckOrdNoMarkers = """
        SELECT (
          SELECT COUNT(*)::int
          FROM quran_words_ordered_tashkeel o
          JOIN quran_words w ON w.id = o.quran_word_id
          WHERE w.is_ayah_marker = true
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_ordered_simple o
          JOIN quran_words w ON w.id = o.quran_word_id
          WHERE w.is_ayah_marker = true
        )
        """;

    internal const string CheckOrdBijectionViolations = """
        SELECT (
          SELECT CASE
            WHEN COUNT(*) = @expected AND COUNT(DISTINCT quran_word_id) = @expected THEN 0
            ELSE 1
          END
          FROM quran_words_ordered_tashkeel
        ) + (
          SELECT CASE
            WHEN COUNT(*) = @expected AND COUNT(DISTINCT quran_word_id) = @expected THEN 0
            ELSE 1
          END
          FROM quran_words_ordered_simple
        )
        """;

    internal const string CheckOrdMushafContigViolations = """
        SELECT (
          SELECT CASE
            WHEN MIN(word_order_in_mushaf) = 1
             AND MAX(word_order_in_mushaf) = @expected
             AND COUNT(DISTINCT word_order_in_mushaf) = @expected
             AND COUNT(*) = @expected THEN 0
            ELSE 1
          END
          FROM quran_words_ordered_tashkeel
        ) + (
          SELECT CASE
            WHEN MIN(word_order_in_mushaf) = 1
             AND MAX(word_order_in_mushaf) = @expected
             AND COUNT(DISTINCT word_order_in_mushaf) = @expected
             AND COUNT(*) = @expected THEN 0
            ELSE 1
          END
          FROM quran_words_ordered_simple
        )
        """;

    internal const string CheckOrdSurahContigViolations = """
        SELECT (
          SELECT COUNT(*)::int FROM (
            SELECT surah_number
            FROM quran_words_ordered_tashkeel
            GROUP BY surah_number
            HAVING MIN(word_order_in_surah) <> 1
                OR MAX(word_order_in_surah) <> COUNT(*)
                OR COUNT(DISTINCT word_order_in_surah) <> COUNT(*)
          ) v
        ) + (
          SELECT COUNT(*)::int FROM (
            SELECT surah_number
            FROM quran_words_ordered_simple
            GROUP BY surah_number
            HAVING MIN(word_order_in_surah) <> 1
                OR MAX(word_order_in_surah) <> COUNT(*)
                OR COUNT(DISTINCT word_order_in_surah) <> COUNT(*)
          ) v
        )
        """;

    internal const string CheckOrdAyahContigViolations = """
        SELECT (
          SELECT COUNT(*)::int FROM (
            SELECT surah_number, ayah_number
            FROM quran_words_ordered_tashkeel
            GROUP BY surah_number, ayah_number
            HAVING MIN(word_order_in_ayah) <> 1
                OR MAX(word_order_in_ayah) <> COUNT(*)
                OR COUNT(DISTINCT word_order_in_ayah) <> COUNT(*)
          ) v
        ) + (
          SELECT COUNT(*)::int FROM (
            SELECT surah_number, ayah_number
            FROM quran_words_ordered_simple
            GROUP BY surah_number, ayah_number
            HAVING MIN(word_order_in_ayah) <> 1
                OR MAX(word_order_in_ayah) <> COUNT(*)
                OR COUNT(DISTINCT word_order_in_ayah) <> COUNT(*)
          ) v
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_ordered_tashkeel o
          JOIN quran_words w ON w.id = o.quran_word_id
          WHERE o.word_order_in_ayah <> w.word_number
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_ordered_simple o
          JOIN quran_words w ON w.id = o.quran_word_id
          WHERE o.word_order_in_ayah <> w.word_number
        )
        """;

    internal const string CheckUnqCountUniqueTashkeel = """
        SELECT COUNT(*)::int FROM quran_words_unique_tashkeel
        """;

    internal const string CheckUnqCountDistinctTashkeelText = TashkeelIdentityCte + """
        SELECT COUNT(DISTINCT btrim(translate(text_uthmani, identity.ignored_tashkeel_marks, '')))::int
        FROM quran_words
        CROSS JOIN display_word_identity identity
        WHERE is_ayah_marker = false
        """;

    internal const string CheckUnqCountUniqueSimple = """
        SELECT COUNT(*)::int FROM quran_words_unique_simple
        """;

    internal const string CheckUnqCountDistinctSimpleText = """
        SELECT COUNT(DISTINCT word_key_imlaei_simple)::int FROM quran_words WHERE is_ayah_marker = false
        """;

    internal const string CheckUnqIdDeterministicViolations = """
        SELECT (
          SELECT COUNT(*)::int
          FROM quran_words_unique_tashkeel
          WHERE id <> first_quran_word_id
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_unique_simple
          WHERE id <> first_quran_word_id
        )
        """;

    internal const string CheckUnqIdDuplicateViolations = """
        SELECT (
          SELECT COUNT(*)::int - COUNT(DISTINCT id)::int
          FROM quran_words_unique_tashkeel
        ) + (
          SELECT COUNT(*)::int - COUNT(DISTINCT id)::int
          FROM quran_words_unique_simple
        )
        """;

    internal const string CheckStatMatchViolations = TashkeelIdentityCte + """
        , source_tashkeel_stats AS (
          SELECT btrim(translate(text_uthmani, identity.ignored_tashkeel_marks, '')) AS tashkeel_identity,
                 COUNT(*)::int AS occurrences_count,
                 COUNT(DISTINCT ayah_id)::int AS ayahs_count,
                 COUNT(DISTINCT surah_number)::int AS surahs_count
          FROM quran_words
          CROSS JOIN display_word_identity identity
          WHERE is_ayah_marker = false
          GROUP BY btrim(translate(text_uthmani, identity.ignored_tashkeel_marks, ''))
        )
        SELECT (
          SELECT CASE WHEN COALESCE(SUM(occurrences_count), 0) = @expected THEN 0 ELSE 1 END
          FROM quran_words_unique_tashkeel
        ) + (
          SELECT CASE WHEN COALESCE(SUM(occurrences_count), 0) = @expected THEN 0 ELSE 1 END
          FROM quran_words_unique_simple
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_ordered_tashkeel o
          CROSS JOIN display_word_identity identity
          LEFT JOIN source_tashkeel_stats g
            ON g.tashkeel_identity
              = btrim(translate(o.text_uthmani, identity.ignored_tashkeel_marks, ''))
          WHERE g.tashkeel_identity IS NULL
             OR g.occurrences_count <> o.occurrences_count
             OR g.ayahs_count <> o.ayahs_count
             OR g.surahs_count <> o.surahs_count
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_ordered_simple o
          LEFT JOIN (
            SELECT word_key_imlaei_simple,
                   COUNT(*)::int AS occurrences_count,
                   COUNT(DISTINCT ayah_id)::int AS ayahs_count,
                   COUNT(DISTINCT surah_number)::int AS surahs_count
            FROM quran_words
            WHERE is_ayah_marker = false
            GROUP BY word_key_imlaei_simple
          ) g ON g.word_key_imlaei_simple = o.word_key_imlaei_simple
          WHERE g.word_key_imlaei_simple IS NULL
             OR g.occurrences_count <> o.occurrences_count
             OR g.ayahs_count <> o.ayahs_count
             OR g.surahs_count <> o.surahs_count
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_unique_tashkeel u
          CROSS JOIN display_word_identity identity
          LEFT JOIN source_tashkeel_stats g
            ON g.tashkeel_identity
              = btrim(translate(u.text_uthmani, identity.ignored_tashkeel_marks, ''))
          WHERE g.tashkeel_identity IS NULL
             OR g.occurrences_count <> u.occurrences_count
             OR g.ayahs_count <> u.ayahs_count
             OR g.surahs_count <> u.surahs_count
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_unique_simple u
          LEFT JOIN (
            SELECT word_key_imlaei_simple,
                   COUNT(*)::int AS occurrences_count,
                   COUNT(DISTINCT ayah_id)::int AS ayahs_count,
                   COUNT(DISTINCT surah_number)::int AS surahs_count
            FROM quran_words
            WHERE is_ayah_marker = false
            GROUP BY word_key_imlaei_simple
          ) g ON g.word_key_imlaei_simple = u.word_key_imlaei_simple
          WHERE g.word_key_imlaei_simple IS NULL
             OR g.occurrences_count <> u.occurrences_count
             OR g.ayahs_count <> u.ayahs_count
             OR g.surahs_count <> u.surahs_count
        )
        """;

    internal const string CheckLinkReadableCompleteViolations = """
        SELECT COUNT(*)::int
        FROM quran_words
        WHERE is_ayah_marker = false
          AND (
              unique_tashkeel_word_id IS NULL
              OR unique_simple_word_id IS NULL
          )
        """;

    internal const string CheckLinkMarkersNullViolations = """
        SELECT COUNT(*)::int
        FROM quran_words
        WHERE is_ayah_marker = true
          AND (
              unique_tashkeel_word_id IS NOT NULL
              OR unique_simple_word_id IS NOT NULL
          )
        """;

    internal const string CheckLinkResolvesViolations = """
        SELECT (
          SELECT COUNT(*)::int
          FROM quran_words w
          WHERE w.unique_tashkeel_word_id IS NOT NULL
            AND NOT EXISTS (
              SELECT 1
              FROM quran_words_unique_tashkeel u
              WHERE u.id = w.unique_tashkeel_word_id
            )
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words w
          WHERE w.unique_simple_word_id IS NOT NULL
            AND NOT EXISTS (
              SELECT 1
              FROM quran_words_unique_simple u
              WHERE u.id = w.unique_simple_word_id
            )
        )
        """;

    internal const string CheckLinkConsistentViolations = TashkeelIdentityCte + """
        SELECT (
          SELECT COUNT(*)::int
          FROM quran_words w
          JOIN quran_words_unique_tashkeel u ON u.id = w.unique_tashkeel_word_id
          CROSS JOIN display_word_identity identity
          WHERE w.is_ayah_marker = false
            AND btrim(translate(u.text_uthmani, identity.ignored_tashkeel_marks, ''))
                <> btrim(translate(w.text_uthmani, identity.ignored_tashkeel_marks, ''))
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words w
          JOIN quran_words_unique_simple u ON u.id = w.unique_simple_word_id
          WHERE w.is_ayah_marker = false
            AND u.word_key_imlaei_simple <> w.word_key_imlaei_simple
        )
        """;

    internal const string CheckSourceWordsCount = """
        SELECT COUNT(*)::int FROM quran_words
        """;

    internal const string CheckSourceAyahsCount = """
        SELECT COUNT(*)::int FROM quran_ayahs
        """;

    internal const string CheckSourceSurahsCount = """
        SELECT COUNT(*)::int FROM quran_surahs
        """;

}
