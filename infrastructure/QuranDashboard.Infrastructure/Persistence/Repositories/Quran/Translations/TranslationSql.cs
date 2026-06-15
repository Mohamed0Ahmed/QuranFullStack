namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Translations;

internal static class TranslationSql
{
    internal const string SourcesTable = "quran_translation_sources";
    internal const string AyahEntriesTable = "quran_translation_ayah_entries";

    internal const string TruncateTranslationTables = """
        TRUNCATE
            quran_translation_ayah_entries,
            quran_translation_sources
        RESTART IDENTITY CASCADE
        """;

    internal const string CheckSourceCount = """
        SELECT count(*)::int
        FROM quran_translation_sources
        """;

    internal const string CheckAyahMappingCount = """
        SELECT count(*)::bigint
        FROM quran_translation_ayah_entries
        """;

    internal const string CheckDistinctAyahCount = """
        SELECT count(DISTINCT ayah_id)::int
        FROM quran_translation_ayah_entries
        """;

    internal const string ReadSourceIdBySourceKey = """
        SELECT id
        FROM quran_translation_sources
        WHERE source_key = @sourceKey
        """;

    internal const string CheckSimpleSourceCount = """
        SELECT count(*)::int
        FROM quran_translation_sources
        WHERE translation_type = 'simple'
        """;

    internal const string CheckWithFootnotesSourceCount = """
        SELECT count(*)::int
        FROM quran_translation_sources
        WHERE translation_type = 'with_footnotes'
        """;

    internal const string ReadPersistedAyahTexts = """
        SELECT s.source_key, e.verse_key, e.text
        FROM quran_translation_ayah_entries e
        JOIN quran_translation_sources s ON s.id = e.source_id
        ORDER BY s.source_key, e.verse_key
        """;
}
