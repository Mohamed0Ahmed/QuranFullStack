namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Mutashabihat;

internal static class MutashabihatSql
{
    internal const string TruncateMutashabihatTables = """
        TRUNCATE
            quran_mutashabihat_groups,
            quran_mutashabihat_occurrences,
            quran_similar_ayah_links
        RESTART IDENTITY CASCADE
        """;

    internal const string CheckGroupCount = """
        SELECT count(*)::int
        FROM quran_mutashabihat_groups
        """;

    internal const string CheckStoredOccurrenceCount = """
        SELECT count(*)::int
        FROM quran_mutashabihat_occurrences
        """;

    internal const string CheckOccurrenceUniqueViolations = """
        SELECT count(*)::int
        FROM (
          SELECT group_id, ayah_id, word_from, word_to, count(*)::int AS duplicate_count
          FROM quran_mutashabihat_occurrences
          GROUP BY group_id, ayah_id, word_from, word_to
          HAVING count(*) > 1
        ) duplicates
        """;

    internal const string CheckGroupMinSizeViolations = """
        SELECT count(*)::int
        FROM quran_mutashabihat_groups
        WHERE distinct_ayah_count < 2
        """;

    internal const string CheckOccurrenceWordRangeViolations = """
        SELECT count(*)::int
        FROM quran_mutashabihat_occurrences
        WHERE word_from < 1
           OR word_to < word_from
        """;

    internal const string CheckUnresolvedGroupRepresentativeAyahs = """
        SELECT count(*)::int
        FROM quran_mutashabihat_groups g
        LEFT JOIN quran_ayahs a ON a.id = g.representative_ayah_id
        WHERE a.id IS NULL
        """;

    internal const string CheckUnresolvedOccurrenceAyahs = """
        SELECT count(*)::int
        FROM quran_mutashabihat_occurrences o
        LEFT JOIN quran_ayahs a ON a.id = o.ayah_id
        WHERE a.id IS NULL
        """;

    internal const string CheckSimilarSourceCount = """
        SELECT count(DISTINCT source_ayah_id)::int
        FROM quran_similar_ayah_links
        """;

    internal const string CheckSimilarLinkCount = """
        SELECT count(*)::int
        FROM quran_similar_ayah_links
        """;

    internal const string CheckSelfLinkViolations = """
        SELECT count(*)::int
        FROM quran_similar_ayah_links
        WHERE source_ayah_id = target_ayah_id
        """;

    internal const string CheckScoreRangeViolations = """
        SELECT count(*)::int
        FROM quran_similar_ayah_links
        WHERE score < 50 OR score > 100
        """;

    internal const string CheckLinkWordRangeViolations = """
        SELECT count(*)::int
        FROM quran_similar_ayah_links l,
        LATERAL jsonb_array_elements(l.match_words) AS range_elem
        WHERE (
          CASE
            WHEN jsonb_array_length(range_elem) = 1 THEN
              (range_elem->>0)::int < 1
            WHEN jsonb_array_length(range_elem) = 2 THEN
              (range_elem->>0)::int < 1 OR (range_elem->>1)::int < (range_elem->>0)::int
            ELSE true
          END
        )
        """;

    internal const string CheckUnresolvedLinkSourceAyahs = """
        SELECT count(*)::int
        FROM quran_similar_ayah_links l
        LEFT JOIN quran_ayahs a ON a.id = l.source_ayah_id
        WHERE a.id IS NULL
        """;

    internal const string CheckUnresolvedLinkTargetAyahs = """
        SELECT count(*)::int
        FROM quran_similar_ayah_links l
        LEFT JOIN quran_ayahs a ON a.id = l.target_ayah_id
        WHERE a.id IS NULL
        """;
}
