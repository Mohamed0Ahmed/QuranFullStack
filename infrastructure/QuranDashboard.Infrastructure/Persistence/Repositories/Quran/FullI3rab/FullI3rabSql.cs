namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.FullI3rab;

internal static class FullI3rabSql
{
    internal const string SourcesTable = "quran_full_i3rab_sources";
    internal const string EntriesTable = "quran_full_i3rab_entries";
    internal const string AyahEntriesTable = "quran_full_i3rab_ayah_entries";

    internal const string TruncateFullI3rabTables = """
        TRUNCATE
            quran_full_i3rab_ayah_entries,
            quran_full_i3rab_entries,
            quran_full_i3rab_sources
        RESTART IDENTITY CASCADE
        """;

    internal const string CheckSourceCount = """
        SELECT count(*)::int
        FROM quran_full_i3rab_sources
        """;

    internal const string CheckEntryCount = """
        SELECT count(*)::bigint
        FROM quran_full_i3rab_entries
        """;

    internal const string CheckAyahMappingCount = """
        SELECT count(*)::bigint
        FROM quran_full_i3rab_ayah_entries
        """;

    internal const string CheckDistinctAyahCount = """
        SELECT count(DISTINCT ayah_id)::int
        FROM quran_full_i3rab_ayah_entries
        """;

    internal const string CheckCoverageSum = """
        SELECT COALESCE(SUM(covered_ayah_count), 0)::bigint
        FROM quran_full_i3rab_entries
        """;

    internal const string ReadSourceIdBySourceKey = """
        SELECT id
        FROM quran_full_i3rab_sources
        WHERE source_key = @sourceKey
        """;

    internal const string ReadEntryIdsForSource = """
        SELECT source_entry_key, id
        FROM quran_full_i3rab_entries
        WHERE source_id = @sourceId
        """;

    internal const string ReadPersistedEntryHtml = """
        SELECT s.source_key, e.source_entry_key, e.i3rab_html, e.text_hash
        FROM quran_full_i3rab_entries e
        JOIN quran_full_i3rab_sources s ON s.id = e.source_id
        ORDER BY s.source_key, e.source_entry_key
        """;
}
