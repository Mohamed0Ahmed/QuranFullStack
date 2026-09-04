using QuranDashboard.TestRuntime;

namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal static class MigratedScratchDatabase
{
    internal static async Task<string> ResolveAsync(
        string fixtureName,
        string expectedSubtype,
        CancellationToken cancellationToken = default)
    {
        var scratch = await ScratchDatabaseExecutionContext.ResolveAsync(
            QuranDashboard.Tests.TestRuntime.TestRuntimeTestPaths.ContractPath,
            cancellationToken: cancellationToken);
        if (!string.Equals(scratch.Subtype, expectedSubtype, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{fixtureName} requires the '{expectedSubtype}' empty-scratch subtype, "
                + $"but the repository runner supplied '{scratch.Subtype}'.");
        }

        await using var dbContext = new QuranDashboardDbContext(
            new DbContextOptionsBuilder<QuranDashboardDbContext>()
                .UseNpgsql(scratch.ConnectionString)
                .Options);
        await dbContext.Database.MigrateAsync(cancellationToken);

        return scratch.ConnectionString;
    }
}
