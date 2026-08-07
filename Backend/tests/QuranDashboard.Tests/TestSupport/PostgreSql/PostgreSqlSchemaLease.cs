namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal sealed class PostgreSqlSchemaLease : IAsyncDisposable
{
    private readonly string maintenanceConnectionString;

    private int disposed;

    private PostgreSqlSchemaLease(string schemaName, string connectionString, string maintenanceConnectionString)
    {
        SchemaName = schemaName;
        ConnectionString = connectionString;
        this.maintenanceConnectionString = maintenanceConnectionString;
    }

    internal string SchemaName { get; }

    internal string ConnectionString { get; }

    internal static async Task<PostgreSqlSchemaLease> CreateAsync(
        PostgreSqlDatabaseLease database,
        string owner,
        CancellationToken cancellationToken = default)
    {
        var schemaName = PostgreSqlDatabaseName.CreateSchema(owner);
        var maintenanceConnectionString = new NpgsqlConnectionStringBuilder(database.ConnectionString)
        {
            Pooling = false
        }.ConnectionString;

        await ExecuteAsync(
            maintenanceConnectionString,
            $"CREATE SCHEMA {PostgreSqlDatabaseName.Quote(schemaName)}",
            cancellationToken);

        var connectionString = new NpgsqlConnectionStringBuilder(database.ConnectionString)
        {
            SearchPath = schemaName
        }.ConnectionString;

        return new PostgreSqlSchemaLease(schemaName, connectionString, maintenanceConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        NpgsqlConnection.ClearPool(new NpgsqlConnection(ConnectionString));
        await ExecuteAsync(
            maintenanceConnectionString,
            $"DROP SCHEMA IF EXISTS {PostgreSqlDatabaseName.Quote(SchemaName)} CASCADE",
            CancellationToken.None);
    }

    private static async Task ExecuteAsync(
        string connectionString,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
