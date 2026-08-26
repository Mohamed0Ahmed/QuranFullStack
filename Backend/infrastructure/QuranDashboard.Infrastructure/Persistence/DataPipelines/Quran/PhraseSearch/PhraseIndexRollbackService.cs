using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseIndexRollbackService : IPhraseIndexRollback
{
    private readonly QuranDashboardDbContext dbContext;
    private readonly PhraseSourceStateCoordinator sourceStateCoordinator;
    private readonly PhraseSourceSnapshotReader sourceReader;

    public PhraseIndexRollbackService(
        QuranDashboardDbContext dbContext,
        PhraseSourceStateCoordinator sourceStateCoordinator,
        PhraseSourceSnapshotReader sourceReader)
    {
        this.dbContext = dbContext;
        this.sourceStateCoordinator = sourceStateCoordinator;
        this.sourceReader = sourceReader;
    }

    public async Task<PhraseIndexRollbackExecution> RollbackAsync(CancellationToken ct)
    {
        var connection = await OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await sourceStateCoordinator.LockSourceMutationAsync(connection, transaction, ct);
        var state = await sourceStateCoordinator.LockStateAsync(connection, transaction, ct);

        if (state.IsStale
            || state.ActiveBuildId is null
            || state.PreviousBuildId is null)
        {
            await transaction.RollbackAsync(ct);
            return Failure(state, "No compatible previous phrase generation is available.");
        }

        var source = await sourceReader.ReadAsync(connection, transaction, ct);
        if (!source.Passed)
        {
            await transaction.RollbackAsync(ct);
            return Failure(state, "Phrase source integrity checks failed; rollback was not applied.");
        }

        var fingerprint = PhraseSourceFingerprint.Compute(source.Tokens);
        if (!string.Equals(state.SourceFingerprint, fingerprint, StringComparison.Ordinal))
        {
            await transaction.RollbackAsync(ct);
            return Failure(state, "Phrase source fingerprint changed; rollback was not applied.");
        }

        var active = await ReadBuildAsync(
            connection,
            transaction,
            state.ActiveBuildId.Value,
            ct);
        var previous = await ReadBuildAsync(
            connection,
            transaction,
            state.PreviousBuildId.Value,
            ct);
        if (active is null
            || active.Status != 3
            || previous is null
            || previous.Status != 4
            || previous.FormatVersion != PhraseIndexBuildConstants.FormatVersion
            || !previous.ExactReady
            || !previous.SimilarityReady
            || previous.SourceRevision != state.SourceRevision
            || !string.Equals(previous.SourceFingerprint, fingerprint, StringComparison.Ordinal))
        {
            await transaction.RollbackAsync(ct);
            return Failure(state, "The previous phrase generation is not source-compatible and ready.");
        }

        var now = DateTimeOffset.UtcNow;
        await ExecuteAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_builds
            SET status = 4,
                completed_at_utc = COALESCE(completed_at_utc, @now)
            WHERE id = @active_build_id
              AND status = 3;
            UPDATE quran_phrase_index_builds
            SET status = 3,
                activated_at_utc = @now,
                completed_at_utc = @now
            WHERE id = @previous_build_id
              AND status = 4;
            UPDATE quran_phrase_index_state
            SET active_build_id = @previous_build_id,
                previous_build_id = @active_build_id,
                is_stale = false,
                stale_reason = NULL,
                updated_at_utc = @now
            WHERE id = 1
            """,
            ct,
            new NpgsqlParameter("now", now),
            new NpgsqlParameter("active_build_id", state.ActiveBuildId.Value),
            new NpgsqlParameter("previous_build_id", state.PreviousBuildId.Value));
        await transaction.CommitAsync(ct);

        return new PhraseIndexRollbackExecution(
            true,
            "Phrase index rollback activated the compatible previous generation.",
            state.PreviousBuildId,
            state.ActiveBuildId,
            state.SourceRevision,
            fingerprint);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for phrase index rollback.");
        }

        if (npgsqlConnection.State != ConnectionState.Open)
        {
            await npgsqlConnection.OpenAsync(ct);
        }

        return npgsqlConnection;
    }

    private static async Task<CompatibleBuild?> ReadBuildAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT status,
                   format_version,
                   exact_ready,
                   similarity_ready,
                   source_revision,
                   source_fingerprint
            FROM quran_phrase_index_builds
            WHERE id = @build_id
            FOR UPDATE
            """,
            connection,
            transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("build_id", buildId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new CompatibleBuild(
            reader.GetInt16(0),
            reader.GetInt32(1),
            reader.GetBoolean(2),
            reader.GetBoolean(3),
            reader.GetInt64(4),
            reader.GetString(5));
    }

    private static PhraseIndexRollbackExecution Failure(
        PhraseSourceState state,
        string message) => new(
            false,
            message,
            state.ActiveBuildId,
            state.PreviousBuildId,
            state.SourceRevision,
            state.SourceFingerprint ?? string.Empty);

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync(ct);
    }

    private sealed record CompatibleBuild(
        short Status,
        int FormatVersion,
        bool ExactReady,
        bool SimilarityReady,
        long SourceRevision,
        string SourceFingerprint);
}
