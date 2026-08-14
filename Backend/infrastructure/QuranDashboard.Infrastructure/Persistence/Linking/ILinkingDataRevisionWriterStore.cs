namespace QuranDashboard.Infrastructure.Persistence.Linking;

public interface ILinkingDataRevisionWriterStore
{
    Task<long> LockForReadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken);

    Task<long> LockForWriteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken);

    Task<long> IncrementAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken);
}
