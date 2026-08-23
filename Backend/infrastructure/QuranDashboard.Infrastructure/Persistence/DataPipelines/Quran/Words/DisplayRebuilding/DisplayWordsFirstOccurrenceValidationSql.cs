namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.DisplayRebuilding;

internal static class DisplayWordsFirstOccurrenceValidationSql
{
    internal const string CheckViolations = DisplayWordsSql.TashkeelIdentityCte + """
        , first_tashkeel_occurrence AS (
          SELECT DISTINCT ON (
                   btrim(translate(o.text_uthmani, identity.ignored_tashkeel_marks, '')))
                 btrim(translate(o.text_uthmani, identity.ignored_tashkeel_marks, ''))
                   AS tashkeel_identity,
                 o.word_order_in_mushaf,
                 o.quran_word_id,
                 o.location,
                 o.surah_number,
                 o.ayah_number,
                 o.page_number,
                 o.line_number
          FROM quran_words_ordered_tashkeel o
          CROSS JOIN display_word_identity identity
          ORDER BY btrim(translate(o.text_uthmani, identity.ignored_tashkeel_marks, '')),
                   o.word_order_in_mushaf
        ),
        first_simple_occurrence AS (
          SELECT DISTINCT ON (o.word_key_imlaei_simple)
                 o.word_key_imlaei_simple,
                 o.word_order_in_mushaf,
                 o.quran_word_id,
                 o.location,
                 o.surah_number,
                 o.ayah_number,
                 o.page_number,
                 o.line_number
          FROM quran_words_ordered_simple o
          ORDER BY o.word_key_imlaei_simple, o.word_order_in_mushaf
        )
        SELECT (
          SELECT COUNT(*)::int
          FROM quran_words_unique_tashkeel u
          CROSS JOIN display_word_identity identity
          LEFT JOIN first_tashkeel_occurrence first_occurrence
            ON first_occurrence.tashkeel_identity
              = btrim(translate(u.text_uthmani, identity.ignored_tashkeel_marks, ''))
          WHERE first_occurrence.tashkeel_identity IS NULL
             OR u.first_word_order_in_mushaf <> first_occurrence.word_order_in_mushaf
             OR u.first_quran_word_id <> first_occurrence.quran_word_id
             OR u.first_location <> first_occurrence.location
             OR u.first_surah_number <> first_occurrence.surah_number
             OR u.first_ayah_number <> first_occurrence.ayah_number
             OR u.first_page_number <> first_occurrence.page_number
             OR u.first_line_number <> first_occurrence.line_number
        ) + (
          SELECT COUNT(*)::int
          FROM quran_words_unique_simple u
          LEFT JOIN first_simple_occurrence first_occurrence
            ON first_occurrence.word_key_imlaei_simple = u.word_key_imlaei_simple
          WHERE first_occurrence.word_key_imlaei_simple IS NULL
             OR u.first_word_order_in_mushaf <> first_occurrence.word_order_in_mushaf
             OR u.first_quran_word_id <> first_occurrence.quran_word_id
             OR u.first_location <> first_occurrence.location
             OR u.first_surah_number <> first_occurrence.surah_number
             OR u.first_ayah_number <> first_occurrence.ayah_number
             OR u.first_page_number <> first_occurrence.page_number
             OR u.first_line_number <> first_occurrence.line_number
        )
        """;
}
