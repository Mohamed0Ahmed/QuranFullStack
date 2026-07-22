using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab._Support;

// The single authoritative full-substrate reset for the shared AbwabDbCollection database. Every Abwab test
// class shares ONE serial DB (PostgresFixture), so a class must start from a clean substrate REGARDLESS of
// which class ran before it. This clears ALL abwab_* mutable tables — the append-only product audit stream
// (change_sets / audit_events), the append-only security-audit stream, the owner/permission tables, and
// non-root timeline boundaries — then reseeds the singleton revision-state and write-barrier. The three
// append-only streams are wiped under `session_replication_role = replica`, which suspends the append-only
// triggers for THIS session only (teardown), exactly as production never does. Both the kernel harness and
// the security harness route through here so any AbwabDbCollection class is order-independent.
internal static class AbwabSubstrateReset
{
    public static Task FullResetAsync(PostgresFixture fixture) => FullResetAsync(fixture.ConnectionString);

    public static async Task FullResetAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await ExecuteAsync(connection, "SET session_replication_role = replica");
        await ExecuteAsync(connection, "TRUNCATE abwab_audit_events, abwab_change_sets, security_audit_events RESTART IDENTITY");
        // Notification storage (US6): plain mutable tables, not append-only — cleared so notification classes
        // stay order-independent on the shared serial DB. read_states first (FK -> records), or CASCADE.
        await ExecuteAsync(connection, "TRUNCATE abwab_notification_read_states, abwab_notification_records CASCADE");
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
