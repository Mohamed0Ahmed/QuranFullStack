using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Navigation;

namespace QuranDashboard.Tests.Quran.Navigation;

public sealed class NavigationMetadataWriteIsolationTests
{
    [Theory]
    [InlineData("DELETE FROM quran_ayahs;", "DELETE", "quran_ayahs")]
    [InlineData("TRUNCATE quran_ayahs;", "TRUNCATE", "quran_ayahs")]
    [InlineData("COPY quran_ayahs (id, text_uthmani) FROM STDIN (FORMAT BINARY)", "COPY", "quran_ayahs")]
    [InlineData("INSERT INTO quran_surahs (surah_number) VALUES (1);", "INSERT", "quran_surahs")]
    [InlineData("UPDATE quran_ayahs SET text_uthmani = 'tampered' WHERE id = 1;", "non-navigation quran_ayahs column", "text_uthmani")]
    [InlineData("UPDATE quran_surahs SET name_simple = 'tampered' WHERE surah_number = 901;", "UPDATE", "quran_surahs")]
    public void Rejects_disallowed_write_sql(string sql, string reasonFragment, string tableFragment)
    {
        var act = () => NavigationMetadataCommandExecutor.EnsureWriteIsolation(sql);

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain(tableFragment, $"expected rejection for {reasonFragment}");
    }

    [Fact]
    public void Allows_navigation_owned_clear_and_ayah_navigation_update_sql()
    {
        var act = () =>
        {
            NavigationMetadataCommandExecutor.EnsureWriteIsolation(NavigationMetadataSql.ClearNavigationData);
            NavigationMetadataCommandExecutor.EnsureWriteIsolation(NavigationMetadataSql.CreateAyahUpdateTempTable);
            NavigationMetadataCommandExecutor.EnsureWriteIsolation(NavigationMetadataSql.ApplyAyahNavigationUpdates);
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Allows_copy_targets_for_navigation_tables_only()
    {
        const string juzCopy = """
            COPY quran_juzs (
                juz_number,
                verses_count,
                first_ayah_id,
                last_ayah_id,
                first_verse_key,
                last_verse_key)
            FROM STDIN (FORMAT BINARY)
            """;

        const string tempCopy = """
            COPY nav_ayah_updates (
                ayah_id,
                juz_number,
                hizb_number,
                rub_number)
            FROM STDIN (FORMAT BINARY)
            """;

        var act = () =>
        {
            NavigationMetadataCommandExecutor.EnsureAllowedBinaryCopyTarget(juzCopy);
            NavigationMetadataCommandExecutor.EnsureAllowedBinaryCopyTarget(tempCopy);
        };

        act.Should().NotThrow();
    }
}
