using System.Data;
using Npgsql;
using QuranDashboard.Application.Abstractions.Quran.Words.Display;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Words.Display;

public sealed class SqlDisplayWordsRebuilder : IDisplayWordsRebuilder
{
    private const string PassVerdict = "pass";

    private static readonly IReadOnlyList<string> Us1InfoNotes =
    [
        "US1 build: hard validation not yet implemented — verdict is provisional, no integrity checks ran.",
        "Unique counts are derived from the database; informational expectations are 21,210 / 14,783."
    ];

    private readonly QuranDashboardDbContext dbContext;

    public SqlDisplayWordsRebuilder(QuranDashboardDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct)
    {
        return await dbContext.QuranWordsOrderedTashkeel.AnyAsync(ct)
            || await dbContext.QuranWordsOrderedSimple.AnyAsync(ct)
            || await dbContext.QuranWordsUniqueTashkeel.AnyAsync(ct)
            || await dbContext.QuranWordsUniqueSimple.AnyAsync(ct);
    }

    public async Task<DisplayWordsRebuildResult> RebuildAsync(
        bool force,
        int expectedReadableWords,
        CancellationToken ct)
    {
        _ = expectedReadableWords;

        var runAtUtc = DateTimeOffset.UtcNow;
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for display-words rebuild.");
        }

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);
        await dbContext.Database.UseTransactionAsync(transaction, ct);

        try
        {
            if (force)
            {
                await dbContext.Database.ExecuteSqlRawAsync(DisplayWordsSql.TruncateDerivedTables, ct);
            }

            await dbContext.Database.ExecuteSqlRawAsync(DisplayWordsSql.InsertOrderedTashkeel, ct);
            await dbContext.Database.ExecuteSqlRawAsync(DisplayWordsSql.InsertOrderedSimple, ct);
            await dbContext.Database.ExecuteSqlRawAsync(DisplayWordsSql.InsertUniqueTashkeel, ct);
            await dbContext.Database.ExecuteSqlRawAsync(DisplayWordsSql.InsertUniqueSimple, ct);

            var totals = await GatherTotalsAsync(ct);

            await transaction.CommitAsync(ct);

            return new DisplayWordsRebuildResult(
                runAtUtc,
                PassVerdict,
                Persisted: true,
                Forced: force,
                totals,
                Checks: [],
                Warnings: [],
                Errors: [],
                InfoNotes: Us1InfoNotes);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<DisplayWordsTotals> GatherTotalsAsync(CancellationToken ct)
    {
        var orderedTashkeelRows = await dbContext.QuranWordsOrderedTashkeel.CountAsync(ct);
        var orderedSimpleRows = await dbContext.QuranWordsOrderedSimple.CountAsync(ct);
        var uniqueTashkeelRows = await dbContext.QuranWordsUniqueTashkeel.CountAsync(ct);
        var uniqueSimpleRows = await dbContext.QuranWordsUniqueSimple.CountAsync(ct);

        var readableWords = await dbContext.QuranWords.CountAsync(word => !word.IsAyahMarker, ct);

        return new DisplayWordsTotals(
            orderedTashkeelRows,
            orderedSimpleRows,
            uniqueTashkeelRows,
            uniqueSimpleRows,
            readableWords);
    }
}
