namespace QuranDashboard.Infrastructure.Persistence.Linking;

public sealed class LinkingDataRevisionStore : ILinkingDataRevisionWriterStore
{
    private const string SharedLockSql =
        "SELECT generation FROM linking_data_state WHERE id = 1 FOR SHARE";

    private const string ExclusiveLockSql =
        "SELECT generation FROM linking_data_state WHERE id = 1 FOR UPDATE";

    private const string IncrementSql =
        """
        UPDATE linking_data_state
        SET generation = generation + 1,
            updated_at_utc = CURRENT_TIMESTAMP
        WHERE id = 1
        RETURNING generation
        """;

    public Task<long> LockForReadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        ExecuteRequiredScalarAsync(connection, transaction, SharedLockSql, cancellationToken);

    public Task<long> LockForWriteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        ExecuteRequiredScalarAsync(connection, transaction, ExclusiveLockSql, cancellationToken);

    public Task<long> IncrementAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        ExecuteRequiredScalarAsync(connection, transaction, IncrementSql, cancellationToken);

    private static async Task<long> ExecuteRequiredScalarAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull
            ? throw new InvalidOperationException("The linking data revision singleton row is missing.")
            : Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }
}
