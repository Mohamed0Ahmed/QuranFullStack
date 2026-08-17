namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed class AbwabDoorInclusionSyncLock(QuranDashboardDbContext db)
{
    private const int LockNamespace = 193648322;
    private const int LockKey = 1;

    public async Task TakeAfterGlobalLocksBeforeDoorAndUnitLocksAsync(CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "The door inclusion synchronization lock requires an active database transaction.");
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({LockNamespace}, {LockKey})",
            cancellationToken);
    }
}
