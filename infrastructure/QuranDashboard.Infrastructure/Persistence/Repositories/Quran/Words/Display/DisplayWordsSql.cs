namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Words.Display;

internal static class DisplayWordsSql
{
    private const string ReadableBase = """
        WITH readable AS (
          SELECT w.id, w.location, w.ayah_id, w.surah_number, w.ayah_number,
                 w.word_number, w.page_number, w.line_number,
                 w.text_uthmani, w.text_uthmani_simple, w.text_imlaei_simple,
                 a.verse_key
          FROM quran_words w
          JOIN quran_ayahs a ON a.id = w.ayah_id
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
          SELECT text_uthmani,
                 COUNT(*) AS occurrences_count,
                 COUNT(DISTINCT ayah_id) AS ayahs_count,
                 COUNT(DISTINCT surah_number) AS surahs_count
          FROM ranked
          GROUP BY text_uthmani
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
        JOIN stats_tashkeel s ON s.text_uthmani = r.text_uthmani
        """;

    internal const string InsertOrderedSimple = ReadableBase + """
        , stats_simple AS (
          SELECT text_uthmani_simple,
                 COUNT(*) AS occurrences_count,
                 COUNT(DISTINCT ayah_id) AS ayahs_count,
                 COUNT(DISTINCT surah_number) AS surahs_count
          FROM ranked
          GROUP BY text_uthmani_simple
        )
        INSERT INTO quran_words_ordered_simple (
          word_order_in_mushaf, quran_word_id, location, verse_key,
          surah_number, ayah_number, page_number, line_number,
          word_order_in_ayah, word_order_in_surah,
          text_uthmani_simple, text_imlaei_simple,
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
          r.text_uthmani_simple,
          r.text_imlaei_simple,
          s.occurrences_count,
          s.ayahs_count,
          s.surahs_count
        FROM ranked r
        JOIN stats_simple s ON s.text_uthmani_simple = r.text_uthmani_simple
        """;

    internal const string InsertUniqueTashkeel = ReadableBase + """
        , stats_tashkeel AS (
          SELECT text_uthmani,
                 COUNT(*) AS occurrences_count,
                 COUNT(DISTINCT ayah_id) AS ayahs_count,
                 COUNT(DISTINCT surah_number) AS surahs_count
          FROM ranked
          GROUP BY text_uthmani
        ),
        first_occ AS (
          SELECT DISTINCT ON (r.text_uthmani)
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
          ORDER BY r.text_uthmani, r.word_order_in_mushaf
        )
        INSERT INTO quran_words_unique_tashkeel (
          text_uthmani, text_uthmani_simple, text_imlaei_simple,
          occurrences_count, ayahs_count, surahs_count,
          first_quran_word_id, first_location, first_surah_number, first_ayah_number,
          first_word_order_in_mushaf, first_page_number, first_line_number
        )
        SELECT
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
        JOIN stats_tashkeel s ON s.text_uthmani = f.text_uthmani
        ORDER BY f.first_word_order_in_mushaf, f.text_uthmani
        """;

    internal const string InsertUniqueSimple = ReadableBase + """
        , stats_simple AS (
          SELECT text_uthmani_simple,
                 COUNT(*) AS occurrences_count,
                 COUNT(DISTINCT ayah_id) AS ayahs_count,
                 COUNT(DISTINCT surah_number) AS surahs_count
          FROM ranked
          GROUP BY text_uthmani_simple
        ),
        first_occ AS (
          SELECT DISTINCT ON (r.text_uthmani_simple)
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
          ORDER BY r.text_uthmani_simple, r.word_order_in_mushaf
        )
        INSERT INTO quran_words_unique_simple (
          text_uthmani_simple, text_imlaei_simple,
          occurrences_count, ayahs_count, surahs_count,
          first_quran_word_id, first_location, first_surah_number, first_ayah_number,
          first_word_order_in_mushaf, first_page_number, first_line_number
        )
        SELECT
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
        JOIN stats_simple s ON s.text_uthmani_simple = f.text_uthmani_simple
        ORDER BY f.first_word_order_in_mushaf, f.text_uthmani_simple
        """;

    internal const string TruncateDerivedTables = """
        TRUNCATE
          quran_words_ordered_tashkeel,
          quran_words_ordered_simple,
          quran_words_unique_tashkeel,
          quran_words_unique_simple
        RESTART IDENTITY
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

    internal const string CheckUnqCountDistinctTashkeelText = """
        SELECT COUNT(DISTINCT text_uthmani)::int FROM quran_words WHERE is_ayah_marker = false
        """;

    internal const string CheckUnqCountUniqueSimple = """
        SELECT COUNT(*)::int FROM quran_words_unique_simple
        """;

    internal const string CheckUnqCountDistinctSimpleText = """
        SELECT COUNT(DISTINCT text_uthmani_simple)::int FROM quran_words WHERE is_ayah_marker = false
        """;

    internal const string CheckStatMatchViolations = """
        SELECT (
          SELECT CASE WHEN COALESCE(SUM(occurrences_count), 0) = @expected THEN 0 ELSE 1 END
          FROM quran_words_unique_tashkeel
        ) + (
          SELECT CASE WHEN COALESCE(SUM(occurrences_count), 0) = @expected THEN 0 ELSE 1 END
          FROM quran_words_unique_simple
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_ordered_tashkeel o
          LEFT JOIN (
            SELECT text_uthmani,
                   COUNT(*)::int AS occurrences_count,
                   COUNT(DISTINCT ayah_id)::int AS ayahs_count,
                   COUNT(DISTINCT surah_number)::int AS surahs_count
            FROM quran_words
            WHERE is_ayah_marker = false
            GROUP BY text_uthmani
          ) g ON g.text_uthmani = o.text_uthmani
          WHERE g.text_uthmani IS NULL
             OR g.occurrences_count <> o.occurrences_count
             OR g.ayahs_count <> o.ayahs_count
             OR g.surahs_count <> o.surahs_count
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_ordered_simple o
          LEFT JOIN (
            SELECT text_uthmani_simple,
                   COUNT(*)::int AS occurrences_count,
                   COUNT(DISTINCT ayah_id)::int AS ayahs_count,
                   COUNT(DISTINCT surah_number)::int AS surahs_count
            FROM quran_words
            WHERE is_ayah_marker = false
            GROUP BY text_uthmani_simple
          ) g ON g.text_uthmani_simple = o.text_uthmani_simple
          WHERE g.text_uthmani_simple IS NULL
             OR g.occurrences_count <> o.occurrences_count
             OR g.ayahs_count <> o.ayahs_count
             OR g.surahs_count <> o.surahs_count
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_unique_tashkeel u
          LEFT JOIN (
            SELECT text_uthmani,
                   COUNT(*)::int AS occurrences_count,
                   COUNT(DISTINCT ayah_id)::int AS ayahs_count,
                   COUNT(DISTINCT surah_number)::int AS surahs_count
            FROM quran_words
            WHERE is_ayah_marker = false
            GROUP BY text_uthmani
          ) g ON g.text_uthmani = u.text_uthmani
          WHERE g.text_uthmani IS NULL
             OR g.occurrences_count <> u.occurrences_count
             OR g.ayahs_count <> u.ayahs_count
             OR g.surahs_count <> u.surahs_count
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_unique_simple u
          LEFT JOIN (
            SELECT text_uthmani_simple,
                   COUNT(*)::int AS occurrences_count,
                   COUNT(DISTINCT ayah_id)::int AS ayahs_count,
                   COUNT(DISTINCT surah_number)::int AS surahs_count
            FROM quran_words
            WHERE is_ayah_marker = false
            GROUP BY text_uthmani_simple
          ) g ON g.text_uthmani_simple = u.text_uthmani_simple
          WHERE g.text_uthmani_simple IS NULL
             OR g.occurrences_count <> u.occurrences_count
             OR g.ayahs_count <> u.ayahs_count
             OR g.surahs_count <> u.surahs_count
        )
        """;

    internal const string CheckFirstOccViolations = """
        SELECT (
          SELECT COUNT(*)::int
          FROM quran_words_unique_tashkeel u
          WHERE u.first_word_order_in_mushaf <> (
            SELECT MIN(o.word_order_in_mushaf)
            FROM quran_words_ordered_tashkeel o
            WHERE o.text_uthmani = u.text_uthmani
          )
          OR NOT EXISTS (
            SELECT 1
            FROM quran_words_ordered_tashkeel o
            WHERE o.text_uthmani = u.text_uthmani
              AND o.word_order_in_mushaf = u.first_word_order_in_mushaf
              AND o.quran_word_id = u.first_quran_word_id
              AND o.location = u.first_location
              AND o.surah_number = u.first_surah_number
              AND o.ayah_number = u.first_ayah_number
              AND o.page_number = u.first_page_number
              AND o.line_number = u.first_line_number
          )
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_unique_simple u
          WHERE u.first_word_order_in_mushaf <> (
            SELECT MIN(o.word_order_in_mushaf)
            FROM quran_words_ordered_simple o
            WHERE o.text_uthmani_simple = u.text_uthmani_simple
          )
          OR NOT EXISTS (
            SELECT 1
            FROM quran_words_ordered_simple o
            WHERE o.text_uthmani_simple = u.text_uthmani_simple
              AND o.word_order_in_mushaf = u.first_word_order_in_mushaf
              AND o.quran_word_id = u.first_quran_word_id
              AND o.location = u.first_location
              AND o.surah_number = u.first_surah_number
              AND o.ayah_number = u.first_ayah_number
              AND o.page_number = u.first_page_number
              AND o.line_number = u.first_line_number
          )
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
