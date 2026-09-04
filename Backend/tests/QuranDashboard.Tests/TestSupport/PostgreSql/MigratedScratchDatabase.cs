using QuranDashboard.TestRuntime;
using QuranDashboard.Tests.TestSupport.Execution;

namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal static class MigratedScratchDatabase
{
    internal static Task<string> ResolveAndMigrateAsync(
        string fixtureName,
        DestructiveRehearsalSubtype expectedSubtype,
        CancellationToken cancellationToken = default) =>
        ResolveAndMigrateAsync(fixtureName, [expectedSubtype], cancellationToken);

    internal static async Task<string> ResolveAndMigrateAsync(
        string fixtureName,
        IReadOnlyCollection<DestructiveRehearsalSubtype> expectedSubtypes,
        CancellationToken cancellationToken = default)
    {
        var expectedSubtypeWireValues = expectedSubtypes.Select(expectedSubtype => expectedSubtype switch
        {
            DestructiveRehearsalSubtype.CanonicalImport => "canonical-import",
            DestructiveRehearsalSubtype.CanonicalRebuild => "canonical-rebuild",
            DestructiveRehearsalSubtype.CanonicalGeneration => "canonical-generation",
            DestructiveRehearsalSubtype.PhraseSearchIndexBuild => "phrase-search-index-build",
            _ => throw new ArgumentOutOfRangeException(
                nameof(expectedSubtypes),
                expectedSubtype,
                "The migrated canonical-pipeline fixtures require an import, rebuild, generation, or phrase-search-index-build subtype."),
        }).ToHashSet(StringComparer.Ordinal);

        var scratch = await ScratchDatabaseExecutionContext.ResolveAsync(
            QuranDashboard.Tests.TestRuntime.TestRuntimeTestPaths.ContractPath,
            cancellationToken: cancellationToken);
        if (!expectedSubtypeWireValues.Contains(scratch.Subtype))
        {
            throw new InvalidOperationException(
                $"{fixtureName} requires one of [{string.Join(", ", expectedSubtypeWireValues)}] empty-scratch subtypes, "
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
