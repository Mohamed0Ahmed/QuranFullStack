namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal static class PostgreSqlTestProcess
{
    private static int exclusiveLeases;

    internal static async Task<ExclusivePostgreSqlLease> LeaseExclusiveServerAsync(
        string owner,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref exclusiveLeases);
        try
        {
            return await ExclusivePostgreSqlLease.AcquireAsync(
                owner,
                () => Interlocked.Decrement(ref exclusiveLeases),
                password,
                cancellationToken);
        }
        catch
        {
            Interlocked.Decrement(ref exclusiveLeases);
            throw;
        }
    }
}
