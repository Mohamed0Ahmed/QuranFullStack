using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter(
    QuranDashboardDbContext db,
    ILinkingDataRevisionWriterStore revisionStore,
    LinkingWriteLockProtocol lockProtocol,
    AbwabDoorInclusionReconciler inclusionReconciler) : ILinkingConfirmationWriter
{
    private async Task<long> LockRevisionAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException("Expected an Npgsql linking confirmation connection.");
        var npgsqlTransaction = transaction.GetDbTransaction() as NpgsqlTransaction
            ?? throw new InvalidOperationException("Expected an Npgsql linking confirmation transaction.");
        return await revisionStore.LockForReadAsync(connection, npgsqlTransaction, cancellationToken);
    }

    private async Task<DateTimeOffset> DatabaseNowAsync(CancellationToken cancellationToken) =>
        await db.Database.SqlQuery<DateTimeOffset>($"SELECT CURRENT_TIMESTAMP AS \"Value\"")
            .SingleAsync(cancellationToken);

    private async Task SaveTranslatingWriteExceptionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LinkingStaleVersionException();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new LinkingDuplicateContributionException();
        }
    }
}
