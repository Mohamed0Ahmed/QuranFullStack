using System.Diagnostics;
using System.Text.RegularExpressions;
using Npgsql;

namespace QuranDashboard.TestRuntime;

internal enum AdvisoryLockMode
{
    Shared,
    Exclusive,
}

internal sealed record AdvisoryLockHolderReport(
    int ProcessId,
    string Mode,
    string? RunId,
    string? Command,
    string? State,
    string? WaitEventType,
    string? WaitEvent);

internal sealed record AdvisoryLockReport(
    long Key,
    string Mode,
    string RunId,
    string Command,
    string Status,
    long TimeoutMilliseconds,
    long WaitMilliseconds,
    int? KeeperProcessId,
    IReadOnlyList<AdvisoryLockHolderReport> Holders);

internal sealed record AdvisoryLockOwnership(
    long Key,
    AdvisoryLockMode Mode,
    string RunId,
    string Command,
    int KeeperProcessId,
    string ApplicationName);

internal sealed record AdvisoryLockAcquisition(
    AdvisoryLockLease? Lease,
    AdvisoryLockReport Report);

internal sealed class AdvisoryLockLease(
    NpgsqlConnection keeperConnection,
    AdvisoryLockOwnership ownership) : IAsyncDisposable
{
    private int disposed;

    internal AdvisoryLockOwnership Ownership { get; } = ownership;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
        {
            await keeperConnection.DisposeAsync();
        }
    }
}

internal static partial class AdvisoryLockProtocol
{
    internal static readonly TimeSpan DefaultAcquisitionTimeout = TimeSpan.FromMinutes(15);
    internal const string LockDatabase = "postgres";

    private const string ApplicationNamePrefix = "qdtr:";
    private const int PollIntervalMilliseconds = 50;
    private const int MaximumRunIdLength = 32;
    private const int MaximumCommandLength = 24;

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex MetadataTokenPattern();

    internal static bool IsValidRunId(string? value) => IsValidMetadataToken(value, MaximumRunIdLength);

    internal static bool IsValidCommand(string? value) => IsValidMetadataToken(value, MaximumCommandLength);

    internal static async Task<AdvisoryLockAcquisition> AcquireAsync(
        string connectionString,
        long key,
        AdvisoryLockMode mode,
        string runId,
        string command,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidRunId(runId))
        {
            throw new ArgumentException("The lock run ID is invalid.", nameof(runId));
        }

        if (!IsValidCommand(command))
        {
            throw new ArgumentException("The lock command is invalid.", nameof(command));
        }

        var acquisitionTimeout = timeout ?? DefaultAcquisitionTimeout;
        if (acquisitionTimeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        var applicationName = $"{ApplicationNamePrefix}{runId}:{command}";
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = applicationName,
            Database = LockDatabase,
            Pooling = false,
        };
        var connection = new NpgsqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            var keeperProcessId = connection.ProcessID;
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                if (await TryAcquireAsync(connection, key, mode, cancellationToken))
                {
                    var ownership = new AdvisoryLockOwnership(
                        key,
                        mode,
                        runId,
                        command,
                        keeperProcessId,
                        applicationName);
                    var report = CreateReport(
                        ownership,
                        "acquired",
                        acquisitionTimeout,
                        stopwatch.Elapsed,
                        []);
                    RecordLease(report);
                    return new AdvisoryLockAcquisition(new AdvisoryLockLease(connection, ownership), report);
                }

                var elapsed = stopwatch.Elapsed;
                if (elapsed >= acquisitionTimeout)
                {
                    var holders = await ReadHoldersAsync(connection, key, cancellationToken);
                    var report = new AdvisoryLockReport(
                        key,
                        ModeName(mode),
                        runId,
                        command,
                        "timeout",
                        (long)acquisitionTimeout.TotalMilliseconds,
                        stopwatch.ElapsedMilliseconds,
                        null,
                        holders);
                    await connection.DisposeAsync();
                    return new AdvisoryLockAcquisition(null, report);
                }

                var remaining = acquisitionTimeout - elapsed;
                var delay = remaining < TimeSpan.FromMilliseconds(PollIntervalMilliseconds)
                    ? remaining
                    : TimeSpan.FromMilliseconds(PollIntervalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    internal static async Task<bool> VerifyOwnershipAsync(
        NpgsqlConnection connection,
        long expectedKey,
        string expectedRunId,
        string expectedCommand,
        AdvisoryLockMode requiredMode,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidRunId(expectedRunId) || !IsValidCommand(expectedCommand))
        {
            return false;
        }

        var expectedApplicationName = $"{ApplicationNamePrefix}{expectedRunId}:{expectedCommand}";
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM pg_catalog.pg_locks AS advisory_lock
                INNER JOIN pg_catalog.pg_stat_activity AS activity ON activity.pid = advisory_lock.pid
                WHERE advisory_lock.locktype = 'advisory'
                  AND advisory_lock.database = (
                      SELECT oid FROM pg_catalog.pg_database WHERE datname = 'postgres')
                  AND advisory_lock.granted
                  AND advisory_lock.objsubid = 1
                  AND ((advisory_lock.classid::bigint << 32) | advisory_lock.objid::bigint) = @key
                  AND advisory_lock.mode = @mode
                  AND activity.application_name = @application_name)
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("key", expectedKey);
        command.Parameters.AddWithValue("mode", PostgreSqlModeName(requiredMode));
        command.Parameters.AddWithValue("application_name", expectedApplicationName);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<bool> TryAcquireAsync(
        NpgsqlConnection connection,
        long key,
        AdvisoryLockMode mode,
        CancellationToken cancellationToken)
    {
        var sql = mode == AdvisoryLockMode.Shared
            ? "SELECT pg_catalog.pg_try_advisory_lock_shared(@key)"
            : "SELECT pg_catalog.pg_try_advisory_lock(@key)";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("key", key);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<IReadOnlyList<AdvisoryLockHolderReport>> ReadHoldersAsync(
        NpgsqlConnection connection,
        long key,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT advisory_lock.pid,
                   advisory_lock.mode,
                   activity.application_name,
                   activity.state,
                   activity.wait_event_type,
                   activity.wait_event
            FROM pg_catalog.pg_locks AS advisory_lock
            LEFT JOIN pg_catalog.pg_stat_activity AS activity ON activity.pid = advisory_lock.pid
            WHERE advisory_lock.locktype = 'advisory'
              AND advisory_lock.database = (
                  SELECT oid FROM pg_catalog.pg_database WHERE datname = 'postgres')
              AND advisory_lock.granted
              AND advisory_lock.objsubid = 1
              AND ((advisory_lock.classid::bigint << 32) | advisory_lock.objid::bigint) = @key
            ORDER BY advisory_lock.pid
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("key", key);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var holders = new List<AdvisoryLockHolderReport>();
        while (await reader.ReadAsync(cancellationToken))
        {
            ParseApplicationName(reader.IsDBNull(2) ? null : reader.GetString(2), out var runId, out var holderCommand);
            holders.Add(new AdvisoryLockHolderReport(
                reader.GetInt32(0),
                PostgreSqlModeName(reader.GetString(1)),
                runId,
                holderCommand,
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5)));
        }

        return holders;
    }

    private static AdvisoryLockReport CreateReport(
        AdvisoryLockOwnership ownership,
        string status,
        TimeSpan timeout,
        TimeSpan wait,
        IReadOnlyList<AdvisoryLockHolderReport> holders) => new(
        ownership.Key,
        ModeName(ownership.Mode),
        ownership.RunId,
        ownership.Command,
        status,
        (long)timeout.TotalMilliseconds,
        wait.Ticks == 0 ? 0 : Math.Max(1, (long)wait.TotalMilliseconds),
        ownership.KeeperProcessId,
        holders);

    private static void RecordLease(AdvisoryLockReport report) =>
        RunEvidenceTelemetry.RecordLease(report.Mode, report.WaitMilliseconds, report.Command);

    private static bool IsValidMetadataToken(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= maximumLength
        && MetadataTokenPattern().IsMatch(value);

    private static void ParseApplicationName(string? applicationName, out string? runId, out string? command)
    {
        runId = null;
        command = null;
        if (applicationName is null || !applicationName.StartsWith(ApplicationNamePrefix, StringComparison.Ordinal))
        {
            return;
        }

        var values = applicationName[ApplicationNamePrefix.Length..].Split(':');
        if (values.Length == 2 && IsValidRunId(values[0]) && IsValidCommand(values[1]))
        {
            runId = values[0];
            command = values[1];
        }
    }

    private static string ModeName(AdvisoryLockMode mode) => mode == AdvisoryLockMode.Shared ? "shared" : "exclusive";

    private static string PostgreSqlModeName(AdvisoryLockMode mode) =>
        mode == AdvisoryLockMode.Shared ? "ShareLock" : "ExclusiveLock";

    private static string PostgreSqlModeName(string mode) => mode switch
    {
        "ShareLock" => "shared",
        "ExclusiveLock" => "exclusive",
        _ => "unknown",
    };
}
