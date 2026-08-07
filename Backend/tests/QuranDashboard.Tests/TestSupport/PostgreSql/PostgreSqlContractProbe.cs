namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal static class PostgreSqlContractProbe
{
    internal static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    internal static async Task<object?> ScalarAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }

    internal static async Task<bool> DatabaseExistsAsync(PostgreSqlDatabaseLease lease, string databaseName)
    {
        var maintenanceConnectionString = new NpgsqlConnectionStringBuilder(lease.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        }.ConnectionString;

        await using var connection = new NpgsqlConnection(maintenanceConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM pg_database WHERE datname = @name",
            connection);
        command.Parameters.AddWithValue("name", databaseName);

        return (long)(await command.ExecuteScalarAsync())! > 0;
    }
}
