using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseIndexPreActivationFailureFinalizer
{
    private readonly QuranDashboardDbContext dbContext;
    private readonly PhraseIndexBuildDatabase database;
    private readonly PhraseIndexBuildFinalizer finalizer;

    public PhraseIndexPreActivationFailureFinalizer(
        QuranDashboardDbContext dbContext,
        PhraseIndexBuildDatabase database,
        PhraseIndexBuildFinalizer finalizer)
    {
        this.dbContext = dbContext;
        this.database = database;
        this.finalizer = finalizer;
    }

    internal async Task<PhraseIndexBuildExecution> FinishAsync(
        NpgsqlConnection? buildConnection,
        PhraseIndexBuildRun run,
        PhraseIndexBuildOutcome outcome,
        string message,
        string status,
        string verdict,
        string failureSummary)
    {
        if (!run.BuildPersisted)
        {
            return await finalizer.FinishFailureAsync(
                buildConnection,
                run,
                outcome,
                message,
                status);
        }

        var finalizationConnection = buildConnection;
        NpgsqlConnection? ownedFinalizationConnection = null;
        var failureMarked = finalizationConnection?.State == ConnectionState.Open
            && await TryMarkFailedAsync(finalizationConnection, run, verdict, failureSummary);
        if (!failureMarked)
        {
            if (buildConnection is not null)
            {
                await CloseWithoutThrowAsync(buildConnection);
            }

            finalizationConnection = await TryOpenFinalizationConnectionAsync();
            ownedFinalizationConnection = finalizationConnection;
            failureMarked = finalizationConnection is not null
                && await TryMarkFailedAsync(finalizationConnection, run, verdict, failureSummary);
        }

        if (!failureMarked)
        {
            AddError(run, "failure-database-finalization-failed");
            finalizationConnection = null;
        }

        try
        {
            return await finalizer.FinishFailureAsync(
                finalizationConnection,
                run,
                outcome,
                message,
                status);
        }
        finally
        {
            if (ownedFinalizationConnection is not null)
            {
                await DisposeWithoutThrowAsync(ownedFinalizationConnection);
            }
        }
    }

    private async Task<bool> TryMarkFailedAsync(
        NpgsqlConnection connection,
        PhraseIndexBuildRun run,
        string verdict,
        string failureSummary)
    {
        try
        {
            await database.MarkFailedAsync(
                connection,
                run.BuildId,
                verdict,
                failureSummary,
                CancellationToken.None);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<NpgsqlConnection?> TryOpenFinalizationConnectionAsync()
    {
        NpgsqlConnection? connection = null;
        try
        {
            var connectionString = dbContext.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(CancellationToken.None);
            return connection;
        }
        catch (Exception)
        {
            if (connection is not null)
            {
                await DisposeWithoutThrowAsync(connection);
            }

            return null;
        }
    }

    private static async Task CloseWithoutThrowAsync(NpgsqlConnection connection)
    {
        try
        {
            await connection.CloseAsync();
        }
        catch (Exception)
        {
        }
    }

    private static async Task DisposeWithoutThrowAsync(NpgsqlConnection connection)
    {
        try
        {
            await connection.DisposeAsync();
        }
        catch (Exception)
        {
        }
    }

    private static void AddError(PhraseIndexBuildRun run, string code)
    {
        if (!run.Errors.Contains(code, StringComparer.Ordinal))
        {
            run.Errors.Add(code);
        }
    }
}
