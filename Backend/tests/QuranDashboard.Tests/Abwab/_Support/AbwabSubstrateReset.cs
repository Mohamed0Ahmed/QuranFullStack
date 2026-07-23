using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab._Support;

internal static class AbwabSubstrateReset
{
    public static Task FullResetAsync(PostgresFixture fixture) => FullResetAsync(fixture.ConnectionString);

    public static async Task FullResetAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await ExecuteAsync(connection, "SET session_replication_role = replica");
        await ExecuteAsync(connection, "TRUNCATE abwab_audit_events, abwab_change_sets, security_audit_events RESTART IDENTITY");
        await ExecuteAsync(connection, "TRUNCATE abwab_notification_read_states, abwab_notification_records CASCADE");
        await ExecuteAsync(connection, "DELETE FROM abwab_category_search_aliases");
        await ExecuteAsync(connection, "DELETE FROM abwab_manual_protections");
        await ExecuteAsync(connection, "DELETE FROM abwab_categories");
        await ExecuteAsync(
            connection,
            "DELETE FROM abwab_sections WHERE id <> '00000000-0000-0000-0000-000000000001'");
        await ExecuteAsync(connection, "DELETE FROM permission_assignments");
        await ExecuteAsync(connection, "DELETE FROM system_owner_memberships");
        await ExecuteAsync(connection, "DELETE FROM abwab_timeline_generation_boundaries WHERE is_root = false");
        await ExecuteAsync(
            connection,
            "UPDATE abwab_revision_state SET audit_head_sequence = 0, timeline_generation = 0, tree_revision = 0 WHERE id = 1");
        await ExecuteAsync(connection, "UPDATE abwab_write_barrier SET state = 0 WHERE id = 1");
        await ExecuteAsync(connection, "SET session_replication_role = origin");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
