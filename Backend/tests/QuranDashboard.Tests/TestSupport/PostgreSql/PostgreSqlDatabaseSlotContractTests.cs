namespace QuranDashboard.Tests.TestSupport.PostgreSql;

[Collection(nameof(PostgreSqlDatabaseSlotCollection))]
public sealed class PostgreSqlDatabaseSlotContractTests(PostgreSqlDatabaseSlotFixture runtime)
{
    private const string Owner = nameof(PostgreSqlDatabaseSlotContractTests);

    // A slot that was never released cannot be observed as a wrong count here - it starves the wait instead.
    // The budget turns that into a named failure in this class rather than a hang the blame timeout kills the
    // whole shard for, and cancelling refuses the queued request instead of stranding a lease nobody disposes.
    private static readonly TimeSpan SlotWaitBudget = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task DatabaseSlots_MakeAnExtraLeaseWait_UntilOneIsReleased()
    {
        var held = new List<PostgreSqlDatabaseLease>();
        try
        {
            await DrainEverySlotAsync(held);

            (await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync())
                .Should().Be(0, "holding the configured cap must leave no slot free");

            using var waitBudget = new CancellationTokenSource(SlotWaitBudget);
            var blocked = PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner, waitBudget.Token);
            var firstCompleted = await Task.WhenAny(blocked, Task.Delay(TimeSpan.FromSeconds(2)));
            firstCompleted.Should().NotBeSameAs(blocked, "the exhausted slot cap must hold the extra lease back");

            var released = held[^1];
            held.RemoveAt(held.Count - 1);
            await released.DisposeAsync();

            held.Add(await blocked);
        }
        finally
        {
            foreach (var lease in held)
            {
                await lease.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task CanceledCaller_IsRefused_AndLeavesTheRuntimeUsable()
    {
        var slotsBefore = await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync();
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();

        var lease = async () => await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner, canceled.Token);

        await lease.Should().ThrowAsync<OperationCanceledException>();
        (await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync()).Should().Be(slotsBefore);

        await using var survivor = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        (await PostgreSqlContractProbe.ScalarAsync(survivor.ConnectionString, "SELECT 1")).Should().Be(1);
    }

    [Fact]
    public async Task QueuedCaller_ThatIsCanceledWhileWaiting_LeavesTheSlotCountIntact()
    {
        var slotsBefore = await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync();
        var held = new List<PostgreSqlDatabaseLease>();
        try
        {
            await DrainEverySlotAsync(held);

            using var abandoned = new CancellationTokenSource();
            var queued = PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner, abandoned.Token);
            var firstCompleted = await Task.WhenAny(queued, Task.Delay(TimeSpan.FromSeconds(1)));
            firstCompleted.Should().NotBeSameAs(queued, "the caller must be queued before it is canceled");

            await abandoned.CancelAsync();
            var awaitQueued = async () => await queued;
            await awaitQueued.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            foreach (var lease in held)
            {
                await lease.DisposeAsync();
            }
        }

        (await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync())
            .Should().Be(slotsBefore, "a canceled waiter must not consume or leak a slot");
    }

    [Fact]
    public async Task LeaseDisposal_DropsTheDatabase_AndIsIdempotent()
    {
        await using var maintenance = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        var slotsBefore = await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync();

        var lease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        var databaseName = lease.DatabaseName;
        await lease.DisposeAsync();
        await lease.DisposeAsync();

        (await PostgreSqlContractProbe.DatabaseExistsAsync(maintenance, databaseName)).Should().BeFalse();
        (await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync()).Should().Be(slotsBefore);
    }

    private async Task DrainEverySlotAsync(List<PostgreSqlDatabaseLease> held)
    {
        using var drainBudget = new CancellationTokenSource(SlotWaitBudget);

        for (var slot = 0; slot < runtime.DatabaseSlotCapacity; slot++)
        {
            held.Add(await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner, drainBudget.Token));
        }
    }
}
