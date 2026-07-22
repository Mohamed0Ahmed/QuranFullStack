using Microsoft.EntityFrameworkCore.Storage;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

// Test-only structural rows; every coordinate/payload is non-canonical, never real Quran data.
internal static class WordTypesSyntheticStructuralData
{
    public const string NounChildCode = "SYN";
    public const int FoldFirstLemmaId = 193101;
    public const int FoldSecondLemmaId = 193102;
    public const int MushafFirstLemmaId = 193103;
    public const int SegmentOnlyLemmaId = 193104;
    public const int MushafFirstWordId = 193010;
    public const int MushafFirstTashkeelId = 193201;

    public static async Task<IDbContextTransaction> BeginSeededTransactionAsync(
        QuranDashboardDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(SeedSql, cancellationToken);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private const string SeedSql = """
        INSERT INTO quran_surahs
          (surah_number, name_arabic, name_simple, name_transliteration, revelation_place,
           revelation_order, verses_count, bismillah_pre)
        VALUES
          (32000, 'اختبار هيكلي غير قرآني', 'Synthetic Structural', 'Synthetic Structural',
           'makkah', 32000, 1, FALSE);

        INSERT INTO quran_mushaf_pages
          (page_number, first_surah_number, first_ayah_number, last_surah_number, last_ayah_number, lines_count)
        VALUES
          (32000, 32000, 1, 32000, 1, 1);

        INSERT INTO quran_ayahs
          (id, surah_number, ayah_number, verse_key, text_uthmani, words_count_source,
           words_count_real, page_from, page_to, juz_number, hizb_number, rub_number)
        VALUES
          (193000, 32000, 1, 'synthetic-alpha', 'SYNTHETIC NONRELIGIOUS STRUCTURAL TEST ROW',
           5, 5, 32000, 32000, NULL, NULL, NULL);

        INSERT INTO quran_pos_tags (code, arabic_label, english_label, category, sort_order)
        VALUES
          ('SYN', 'نوع هيكلي غير قرآني', 'Synthetic structural', 'noun', 32000);

        INSERT INTO quran_lemmas
          (id, lemma_text, lemma_buckwalter, root_id, words_count, first_word_order_in_mushaf)
        VALUES
          (193101, 'إ-هيكلي', 'synthetic-fold-first', NULL, 1, 193030),
          (193102, 'أ-هيكلي', 'synthetic-fold-second', NULL, 1, 193020),
          (193103, 'ب-هيكلي', 'synthetic-mushaf-first', NULL, 1, 193010),
          (193104, 'ج-مقطع-هيكلي', 'synthetic-segment-only', NULL, 1, 193050);

        INSERT INTO quran_words
          (id, location, ayah_id, surah_number, ayah_number, word_number, page_number,
           line_number, line_word_order, qpc_glyph, text_uthmani, text_uthmani_simple,
           text_imlaei_simple, word_key_imlaei_simple, is_ayah_marker,
           unique_tashkeel_word_id, unique_simple_word_id)
        VALUES
          (193010, 'synthetic-alpha:1', 193000, 32000, 1, 1, 32000, 1, 1,
           'SYNTH_ALPHA_C', 'SYNTH_ALPHA_C', 'SYNTH_ALPHA_C', 'SYNTH_ALPHA_C', 'SYNTH_ALPHA_C',
           FALSE, NULL, NULL),
          (193020, 'synthetic-alpha:2', 193000, 32000, 1, 2, 32000, 1, 2,
           'SYNTH_ALPHA_A', 'SYNTH_ALPHA_A', 'SYNTH_ALPHA_A', 'SYNTH_ALPHA_A', 'SYNTH_ALPHA_A',
           FALSE, NULL, NULL),
          (193030, 'synthetic-alpha:3', 193000, 32000, 1, 3, 32000, 1, 3,
           'SYNTH_ALPHA_B', 'SYNTH_ALPHA_B', 'SYNTH_ALPHA_B', 'SYNTH_ALPHA_B', 'SYNTH_ALPHA_B',
           FALSE, NULL, NULL),
          (193040, 'synthetic-inl:1', 193000, 32000, 1, 4, 32000, 1, 4,
           'SYNTH_INL', 'SYNTH_INL', 'SYNTH_INL', 'SYNTH_INL', 'SYNTH_INL',
           FALSE, NULL, NULL),
          (193050, 'synthetic-segment:1', 193000, 32000, 1, 5, 32000, 1, 5,
           'SYNTH_SEGMENT', 'SYNTH_SEGMENT', 'SYNTH_SEGMENT', 'SYNTH_SEGMENT', 'SYNTH_SEGMENT',
           FALSE, NULL, NULL);

        INSERT INTO quran_words_unique_tashkeel
          (id, text_uthmani, text_uthmani_simple, text_imlaei_simple, occurrences_count,
           ayahs_count, surahs_count, first_quran_word_id, first_location, first_surah_number,
           first_ayah_number, first_word_order_in_mushaf, first_page_number, first_line_number)
        VALUES
          (193201, 'SYNTH_ALPHA_C', 'SYNTH_ALPHA_C', 'SYNTH_ALPHA_C', 1, 1, 1,
           193010, 'synthetic-alpha:1', 32000, 1, 193010, 32000, 1),
          (193202, 'SYNTH_ALPHA_A', 'SYNTH_ALPHA_A', 'SYNTH_ALPHA_A', 1, 1, 1,
           193020, 'synthetic-alpha:2', 32000, 1, 193020, 32000, 1),
          (193203, 'SYNTH_ALPHA_B', 'SYNTH_ALPHA_B', 'SYNTH_ALPHA_B', 1, 1, 1,
           193030, 'synthetic-alpha:3', 32000, 1, 193030, 32000, 1),
          (193204, 'SYNTH_INL', 'SYNTH_INL', 'SYNTH_INL', 1, 1, 1,
           193040, 'synthetic-inl:1', 32000, 1, 193040, 32000, 1),
          (193205, 'SYNTH_SEGMENT', 'SYNTH_SEGMENT', 'SYNTH_SEGMENT', 1, 1, 1,
           193050, 'synthetic-segment:1', 32000, 1, 193050, 32000, 1);

        UPDATE quran_words SET unique_tashkeel_word_id = 193201 WHERE id = 193010;
        UPDATE quran_words SET unique_tashkeel_word_id = 193202 WHERE id = 193020;
        UPDATE quran_words SET unique_tashkeel_word_id = 193203 WHERE id = 193030;
        UPDATE quran_words SET unique_tashkeel_word_id = 193204 WHERE id = 193040;
        UPDATE quran_words SET unique_tashkeel_word_id = 193205 WHERE id = 193050;

        INSERT INTO quran_word_morphology
          (quran_word_id, location, head_pos, segment_count, root_id, lemma_id, stem_id,
           is_verb, verb_tense, verb_voice, case_feature, head_features_json)
        VALUES
          (193010, 'synthetic-alpha:1', 'SYN', 1, NULL, 193103, NULL,
           FALSE, NULL, NULL, NULL, NULL),
          (193020, 'synthetic-alpha:2', 'SYN', 1, NULL, 193102, NULL,
           FALSE, NULL, NULL, NULL, NULL),
          (193030, 'synthetic-alpha:3', 'SYN', 1, NULL, 193101, NULL,
           FALSE, NULL, NULL, NULL, NULL),
          (193040, 'synthetic-inl:1', 'INL', 1, NULL, NULL, NULL,
           FALSE, NULL, NULL, NULL, NULL),
          (193050, 'synthetic-segment:1', 'SYN', 1, NULL, NULL, NULL,
           FALSE, NULL, NULL, NULL, NULL);

        INSERT INTO quran_word_morphology_segments
          (quran_word_id, segment_location, segment_number, kind, pos, form_buckwalter,
           arabic_render_source, features_raw, root_id, lemma_id, stem_id)
        VALUES
          (193050, 'synthetic-segment:1:1', 1, 'STEM', 'SYN', 'synthetic',
           'synthetic-nonreligious', 'SYNTHETIC_HEAD_GRAIN_GUARD', NULL, 193104, NULL);
        """;
}
