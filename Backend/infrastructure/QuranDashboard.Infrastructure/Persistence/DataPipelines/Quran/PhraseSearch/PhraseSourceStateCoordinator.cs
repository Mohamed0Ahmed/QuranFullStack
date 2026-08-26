namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

public sealed class PhraseSourceStateCoordinator
{
    private const string LockSourceMutationSql =
        "SELECT pg_advisory_xact_lock(@namespace, @key)";

    private const string LockStateSql = """
        SELECT source_revision,
               source_fingerprint,
               active_build_id,
               previous_build_id,
               is_stale,
               stale_reason
        FROM quran_phrase_index_state
        WHERE id = 1
        FOR UPDATE
        """;

    private readonly PhraseSourceSnapshotReader sourceReader;

    public PhraseSourceStateCoordinator(PhraseSourceSnapshotReader sourceReader)
    {
        this.sourceReader = sourceReader;
    }

    internal async Task<PhraseSourceBootstrapResult> BootstrapAsync(
        NpgsqlConnection connection,
        CancellationToken ct)
    {
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await LockSourceMutationAsync(connection, transaction, ct);
        var state = await LockStateAsync(connection, transaction, ct);
        var source = await sourceReader.ReadAsync(connection, transaction, ct);

        if (!source.Passed)
        {
            await transaction.RollbackAsync(ct);
            return new PhraseSourceBootstrapResult(state, source, string.Empty);
        }

        var fingerprint = PhraseSourceFingerprint.Compute(source.Tokens);
        if (state.SourceFingerprint is null)
        {
            await UpdateInitializedStateAsync(connection, transaction, state.SourceRevision + 1, fingerprint, ct);
            state = state with
            {
                SourceRevision = state.SourceRevision + 1,
                SourceFingerprint = fingerprint,
                IsStale = false,
                StaleReason = null,
            };
        }
        else if (!string.Equals(state.SourceFingerprint, fingerprint, StringComparison.Ordinal))
        {
            await InvalidateBuildPointersAsync(
                connection,
                transaction,
                state.SourceRevision + 1,
                fingerprint,
                "source-fingerprint-changed",
                ct);
            state = state with
            {
                SourceRevision = state.SourceRevision + 1,
                SourceFingerprint = fingerprint,
                ActiveBuildId = null,
                PreviousBuildId = null,
                IsStale = true,
                StaleReason = "source-fingerprint-changed",
            };
        }

        await transaction.CommitAsync(ct);
        return new PhraseSourceBootstrapResult(state, source, fingerprint);
    }

    internal async Task LockSourceMutationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(LockSourceMutationSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue("namespace", PhraseSourceStateCoordinatorContract.AdvisoryLockNamespace);
        command.Parameters.AddWithValue("key", PhraseSourceStateCoordinatorContract.SourceMutationLockKey);
        await command.ExecuteNonQueryAsync(ct);
    }

    internal async Task<PhraseSourceState> LockStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(LockStateSql, connection, transaction)
        {
            CommandTimeout = PhraseIndexBuildConstants.CommandTimeoutSeconds,
        };
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException("PhraseSearch source state singleton is missing.");
        }

        return new PhraseSourceState(
            reader.GetInt64(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetBoolean(4),
            reader.IsDBNull(5) ? null : reader.GetString(5));
    }

    internal async Task BeginFoundationResetAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        var state = await LockStateAsync(connection, transaction, ct);

        await ExecuteAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_state
            SET active_build_id = NULL,
                previous_build_id = NULL,
                is_stale = true,
                stale_reason = 'foundation-import',
                updated_at_utc = @now
            WHERE id = 1;
            DELETE FROM quran_phrase_index_builds;
            """,
            ct,
            new NpgsqlParameter("now", DateTimeOffset.UtcNow));

        if (state.ActiveBuildId == state.PreviousBuildId && state.ActiveBuildId is not null)
        {
            throw new InvalidOperationException("PhraseSearch state contains identical active and previous builds.");
        }
    }

    internal Task CompleteFoundationResetAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct) => ExecuteAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_state
            SET source_revision = source_revision + 1,
                source_fingerprint = NULL,
                active_build_id = NULL,
                previous_build_id = NULL,
                is_stale = true,
                stale_reason = 'foundation-import',
                updated_at_utc = @now
            WHERE id = 1
            """,
            ct,
            new NpgsqlParameter("now", DateTimeOffset.UtcNow));

    internal async Task<PhraseSourceReadResult> RefreshAfterDisplayRebuildAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int expectedReadableWords,
        CancellationToken ct)
    {
        var state = await LockStateAsync(connection, transaction, ct);
        var source = await sourceReader.ReadAsync(
            connection,
            transaction,
            ct,
            expectedReadableWords,
            expectedAyahs: null);
        if (!source.Passed)
        {
            return source;
        }

        var fingerprint = PhraseSourceFingerprint.Compute(source.Tokens);
        if (string.Equals(state.SourceFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return source;
        }

        await InvalidateBuildPointersAsync(
            connection,
            transaction,
            state.SourceRevision + 1,
            fingerprint,
            "display-word-source-changed",
            ct);
        return source;
    }

    private static Task UpdateInitializedStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long revision,
        string fingerprint,
        CancellationToken ct) => ExecuteAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_state
            SET source_revision = @revision,
                source_fingerprint = @fingerprint,
                is_stale = false,
                stale_reason = NULL,
                updated_at_utc = @now
            WHERE id = 1
            """,
            ct,
            new NpgsqlParameter("revision", revision),
            new NpgsqlParameter("fingerprint", fingerprint),
            new NpgsqlParameter("now", DateTimeOffset.UtcNow));

    private static Task InvalidateBuildPointersAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long revision,
        string fingerprint,
        string reason,
        CancellationToken ct) => ExecuteAsync(
            connection,
            transaction,
            """
            UPDATE quran_phrase_index_state
            SET source_revision = @revision,
                source_fingerprint = @fingerprint,
                active_build_id = NULL,
                previous_build_id = NULL,
                is_stale = true,
                stale_reason = @reason,
                updated_at_utc = @now
            WHERE id = 1;
            UPDATE quran_phrase_index_builds
            SET status = 4,
                completed_at_utc = COALESCE(completed_at_utc, @now)
            WHERE status IN (2, 3);
            """,
            ct,
            new NpgsqlParameter("revision", revision),
            new NpgsqlParameter("fingerprint", fingerprint),
            new NpgsqlParameter("reason", reason),
            new NpgsqlParameter("now", DateTimeOffset.UtcNow));

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
}
