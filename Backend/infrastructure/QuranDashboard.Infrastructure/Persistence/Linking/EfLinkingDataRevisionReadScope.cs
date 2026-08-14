using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Linking;

internal sealed class EfLinkingDataRevisionReadScope(
    QuranDashboardDbContext db,
    ILinkingDataRevisionWriterStore store) : ILinkingDataRevisionReadScope
{
    public async Task<TResult> ExecuteAsync<TResult>(
        int maximumAttempts,
        Func<long, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumAttempts, 1);
        ArgumentNullException.ThrowIfNull(operation);

        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            try
            {
                return await ExecuteAttemptAsync(operation, cancellationToken);
            }
            catch (PostgresException exception)
                when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
            {
                db.ChangeTracker.Clear();

                if (attempt == maximumAttempts)
                {
                    throw new LinkingDataRevisionReadRetryExhaustedException(exception);
                }
            }
        }

        throw new InvalidOperationException("The linking data revision read scope exhausted without a result.");
    }

    private async Task<TResult> ExecuteAttemptAsync<TResult>(
        Func<long, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);
        var connection = db.Database.GetDbConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException("Expected an Npgsql connection for linking revision reads.");
        var npgsqlTransaction = transaction.GetDbTransaction() as NpgsqlTransaction
            ?? throw new InvalidOperationException("Expected an Npgsql transaction for linking revision reads.");
        var revision = await store.LockForReadAsync(connection, npgsqlTransaction, cancellationToken);
        var result = await operation(revision, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
