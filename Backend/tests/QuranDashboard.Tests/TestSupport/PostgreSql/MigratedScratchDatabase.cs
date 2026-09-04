using QuranDashboard.TestRuntime;
using QuranDashboard.Tests.TestSupport.Execution;

namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal static class MigratedScratchDatabase
{
    internal static async Task<string> ResolveAndMigrateAsync(
        string fixtureName,
        DestructiveRehearsalSubtype expectedSubtype,
        CancellationToken cancellationToken = default)
    {
        var expectedSubtypeWireValue = expectedSubtype switch
        {
            DestructiveRehearsalSubtype.CanonicalImport => "canonical-import",
            DestructiveRehearsalSubtype.CanonicalRebuild => "canonical-rebuild",
            DestructiveRehearsalSubtype.CanonicalGeneration => "canonical-generation",
            _ => throw new ArgumentOutOfRangeException(
                nameof(expectedSubtype),
                expectedSubtype,
                "The migrated canonical-pipeline fixtures require an import, rebuild, or generation subtype."),
        };

        var scratch = await ScratchDatabaseExecutionContext.ResolveAsync(
            QuranDashboard.Tests.TestRuntime.TestRuntimeTestPaths.ContractPath,
            cancellationToken: cancellationToken);
        if (!string.Equals(scratch.Subtype, expectedSubtypeWireValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{fixtureName} requires the '{expectedSubtypeWireValue}' empty-scratch subtype, "
                + $"but the repository runner supplied '{scratch.Subtype}'.");
        }

        await using var dbContext = new QuranDashboardDbContext(
            new DbContextOptionsBuilder<QuranDashboardDbContext>()
                .UseNpgsql(scratch.ConnectionString)
                .Options);
        await dbContext.Database.MigrateAsync(cancellationToken);

        return scratch.ConnectionString;
    }

    internal static async Task ResetAndMigrateAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext(connectionString);
        await dbContext.Database.ExecuteSqlRawAsync(
            "DROP SCHEMA public CASCADE; CREATE SCHEMA public;",
            cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private static QuranDashboardDbContext CreateDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(connectionString)
            .Options);
}
