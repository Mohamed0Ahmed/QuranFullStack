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
        """;

    internal const string TruncateDerivedTables = """
        TRUNCATE
          quran_words_ordered_tashkeel,
          quran_words_ordered_simple,
          quran_words_unique_tashkeel,
          quran_words_unique_simple
        RESTART IDENTITY
        """;

}
