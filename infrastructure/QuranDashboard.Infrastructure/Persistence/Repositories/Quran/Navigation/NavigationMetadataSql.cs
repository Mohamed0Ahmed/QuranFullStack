namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Navigation;

internal static class NavigationMetadataSql
{
    internal const string ProbeTargetHasData = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM quran_juzs
            UNION ALL SELECT 1 FROM quran_hizbs
            UNION ALL SELECT 1 FROM quran_rubs
            UNION ALL SELECT 1 FROM quran_sajdas
            UNION ALL SELECT 1 FROM quran_ayahs
                WHERE juz_number IS NOT NULL
                   OR hizb_number IS NOT NULL
                   OR rub_number IS NOT NULL
        ) THEN 1 ELSE 0 END
        """;

    internal const string CheckAyahCount = """
        SELECT count(*)::int FROM quran_ayahs
        """;

    internal const string CheckJuzCount = """
        SELECT count(*)::int FROM quran_juzs
        """;

    internal const string CheckHizbCount = """
        SELECT count(*)::int FROM quran_hizbs
        """;

    internal const string CheckRubCount = """
        SELECT count(*)::int FROM quran_rubs
        """;

    internal const string CheckSajdaCount = """
        SELECT count(*)::int FROM quran_sajdas
        """;

    internal const string CheckTaggedAyahCount = """
        SELECT count(*)::int
        FROM quran_ayahs
        WHERE juz_number IS NOT NULL
          AND hizb_number IS NOT NULL
          AND rub_number IS NOT NULL
        """;

    internal const string ClearNavigationData = """
        TRUNCATE quran_sajdas, quran_rubs, quran_hizbs, quran_juzs RESTART IDENTITY CASCADE;
        UPDATE quran_ayahs
        SET juz_number = NULL, hizb_number = NULL, rub_number = NULL;
        """;

    internal const string CreateAyahUpdateTempTable = """
        CREATE TEMP TABLE nav_ayah_updates (
            ayah_id int PRIMARY KEY,
            juz_number smallint NOT NULL,
            hizb_number smallint NOT NULL,
            rub_number smallint NOT NULL
        ) ON COMMIT DROP
        """;

    internal const string ApplyAyahNavigationUpdates = """
        UPDATE quran_ayahs AS ayah
        SET juz_number = update_row.juz_number,
            hizb_number = update_row.hizb_number,
            rub_number = update_row.rub_number
        FROM nav_ayah_updates AS update_row
        WHERE ayah.id = update_row.ayah_id
        """;
}
