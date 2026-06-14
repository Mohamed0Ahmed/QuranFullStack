namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Tafsirs;

internal static class TafsirSql
{
    internal const string SourcesTable = "quran_tafsir_sources";
    internal const string EntriesTable = "quran_tafsir_entries";
    internal const string AyahEntriesTable = "quran_tafsir_ayah_entries";

    internal const string TruncateTafsirTables = """
        TRUNCATE
            quran_tafsir_ayah_entries,
            quran_tafsir_entries,
            quran_tafsir_sources
        RESTART IDENTITY CASCADE
        """;

    internal const string CheckSourceCount = """
        SELECT count(*)::int
        FROM quran_tafsir_sources
        """;

    internal const string CheckTextBlockCount = """
        SELECT count(*)::bigint
        FROM quran_tafsir_entries
        """;

    internal const string CheckAyahMappingCount = """
        SELECT count(*)::bigint
        FROM quran_tafsir_ayah_entries
        """;

    internal const string CheckDistinctAyahCount = """
        SELECT count(DISTINCT ayah_id)::int
        FROM quran_tafsir_ayah_entries
        """;

    internal const string ReadSourceIdsBySourceKey = """
        SELECT id, source_key
        FROM quran_tafsir_sources
        """;

    internal const string ReadEntryIdsForSource = """
        SELECT source_entry_key, id
        FROM quran_tafsir_entries
        WHERE source_id = @sourceId
        """;

    internal const string ReadSourceIdBySourceKey = """
        SELECT id
        FROM quran_tafsir_sources
        WHERE source_key = @sourceKey
        """;
}
