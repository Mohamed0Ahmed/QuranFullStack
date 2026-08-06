namespace QuranDashboard.Tests.TestSupport.PostgreSql;

public sealed class PostgreSqlDatabaseSlotFixture : IAsyncLifetime
{
    public int DatabaseSlotCapacity { get; private set; }

    public async Task InitializeAsync()
    {
        DatabaseSlotCapacity = await PostgreSqlTestProcess.ConfiguredDatabaseSlotsAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

// Nonparallel because the slot counter these cases assert on is one process-wide SemaphoreSlim shared by
// every collection in the test host: a lease taken or released by another collection between two readings
// of it makes the comparison meaningless, which is what made this family pass alone and fail in a full run.
// Serialization is also what makes taking the whole cap safe — no concurrent collection can be starved by a
// drain that runs while nothing else is running.
[CollectionDefinition(nameof(PostgreSqlDatabaseSlotCollection), DisableParallelization = true)]
public sealed class PostgreSqlDatabaseSlotCollection : ICollectionFixture<PostgreSqlDatabaseSlotFixture>;
