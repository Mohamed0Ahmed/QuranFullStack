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

    internal const string CheckEntrySourceMismatch = """
        SELECT count(*)::int
        FROM quran_full_i3rab_ayah_entries j
        JOIN quran_full_i3rab_entries e ON e.id = j.entry_id
        WHERE j.source_id <> e.source_id
        """;

    internal const string CheckEmptyHtml = """
        SELECT count(*)::int
        FROM quran_full_i3rab_entries
        WHERE i3rab_html = '' OR btrim(i3rab_html) = ''
        """;

    internal const string CheckUnresolvedJunctionAyahIds = """
        SELECT count(*)::int
        FROM quran_full_i3rab_ayah_entries j
        LEFT JOIN quran_ayahs a ON a.id = j.ayah_id
        WHERE a.id IS NULL
        """;

    internal const string CheckUnresolvedLeaderAyahIds = """
        SELECT count(*)::int
        FROM quran_full_i3rab_entries e
        LEFT JOIN quran_ayahs a ON a.id = e.leader_ayah_id
        WHERE a.id IS NULL
        """;

    internal const string CheckPerSourceAyahMappingViolations = """
        SELECT count(*)::int
        FROM (
            SELECT source_id
            FROM quran_full_i3rab_ayah_entries
            GROUP BY source_id
            HAVING count(*)::bigint <> @expectedPerSource
                OR count(DISTINCT verse_key)::int <> @expectedPerSource
        ) violations
        """;

    internal const string CheckPerSourceCoverageViolations = """
        SELECT count(*)::int
        FROM (
            SELECT e.source_id
            FROM quran_full_i3rab_entries e
            GROUP BY e.source_id
            HAVING COALESCE(SUM(covered_ayah_count), 0)::bigint <> @expectedPerSource
        ) violations
        """;
}
