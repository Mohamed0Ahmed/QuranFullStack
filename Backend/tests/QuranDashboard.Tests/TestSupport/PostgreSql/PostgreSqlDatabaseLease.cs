namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal sealed class PostgreSqlDatabaseLease : IAsyncDisposable
{
    private readonly Func<PostgreSqlDatabaseLease, ValueTask>? release;

    private int disposed;

    private PostgreSqlDatabaseLease(
        string owner,
        string databaseName,
        string connectionString,
        Guid serverInstanceId,
        Func<PostgreSqlDatabaseLease, ValueTask>? release)
    {
        Owner = owner;
        DatabaseName = databaseName;
        ConnectionString = connectionString;
        ServerInstanceId = serverInstanceId;
        this.release = release;
    }

    internal string Owner { get; }

    internal string DatabaseName { get; }

    internal string ConnectionString { get; }

    internal Guid ServerInstanceId { get; }

    internal bool IsExternal => release is null;

    internal static PostgreSqlDatabaseLease Owned(
        string owner,
        string databaseName,
        string connectionString,
        Guid serverInstanceId,
        Func<PostgreSqlDatabaseLease, ValueTask> release)
    {
        return new PostgreSqlDatabaseLease(owner, databaseName, connectionString, serverInstanceId, release);
    }

    internal static PostgreSqlDatabaseLease External(string databaseName, string connectionString)
    {
        return new PostgreSqlDatabaseLease(
            "external-read-only",
            databaseName,
            connectionString,
            Guid.Empty,
            release: null);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0 || release is null)
        {
            return;
        }

        await release(this);
    }
}
