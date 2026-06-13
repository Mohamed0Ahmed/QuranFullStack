namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Mutashabihat;

internal static class MutashabihatSql
{
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
}
