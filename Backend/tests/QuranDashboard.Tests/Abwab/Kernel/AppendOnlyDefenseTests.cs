using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

[Collection(nameof(AbwabDbCollection))]
public sealed class AppendOnlyDefenseTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string RestrictedRole = "abwab_app";

    private static readonly string[] AuditTables = ["abwab_audit_events", "abwab_change_sets"];

    public async Task InitializeAsync() => await AbwabKernelHarness.ResetKernelStateAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DirectUpdateDeleteTruncate_OnAuditTables_IsBlockedByTheTrigger()
    {
        await AbwabKernelHarness.CommitAsync(fixture, AbwabKernelHarness.Request(expectedGeneration: 0));

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        foreach (var table in AuditTables)
        {
            await AssertBlockedAsync(connection, $"UPDATE {table} SET id = id");
            await AssertBlockedAsync(connection, $"DELETE FROM {table}");
            await AssertBlockedAsync(connection, $"TRUNCATE {table}");
        }
    }

    [Fact]
    public async Task RestrictedApplicationRole_HasAppendButNotMutatePrivileges()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        foreach (var table in AuditTables)
        {
            (await HasPrivilegeAsync(connection, table, "SELECT")).Should().BeTrue();
            (await HasPrivilegeAsync(connection, table, "INSERT")).Should().BeTrue();
            (await HasPrivilegeAsync(connection, table, "UPDATE")).Should().BeFalse();
            (await HasPrivilegeAsync(connection, table, "DELETE")).Should().BeFalse();
            (await HasPrivilegeAsync(connection, table, "TRUNCATE")).Should().BeFalse();
        }
    }

    private static async Task AssertBlockedAsync(NpgsqlConnection connection, string sql)
    {
        var act = () => AbwabKernelHarness.ExecuteAsync(connection, sql);
        await act.Should().ThrowAsync<PostgresException>($"'{sql}' must be blocked on an append-only audit table");
    }

    private static async Task<bool> HasPrivilegeAsync(NpgsqlConnection connection, string table, string privilege)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT has_table_privilege(@role, @table, @privilege)";
        command.Parameters.AddWithValue("role", RestrictedRole);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("privilege", privilege);
        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
