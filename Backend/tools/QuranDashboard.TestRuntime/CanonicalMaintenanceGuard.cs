using System.Globalization;
using Npgsql;

namespace QuranDashboard.TestRuntime;

internal static class CanonicalMaintenanceGuard
{
    private const string GuardEnvironmentVariable = "QURAN_DASHBOARD_TEST_RUNTIME_GUARD";
    private const string RunIdEnvironmentVariable = "QURAN_DASHBOARD_TEST_RUN_ID";
    private const string CommandEnvironmentVariable = "QURAN_DASHBOARD_TEST_LOCK_COMMAND";
    private const string LockKeyEnvironmentVariable = "QURAN_DASHBOARD_TEST_LOCK_KEY";

    internal static async Task<bool> VerifyIfRequiredAsync(
        string? connectionString,
        TextWriter error,
        Func<string, string?>? readEnvironment = null,
        CancellationToken cancellationToken = default)
    {
        var environment = readEnvironment ?? Environment.GetEnvironmentVariable;
        var guard = environment(GuardEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (guard is not null)
            {
                error.WriteLine("ERROR: the TestRuntime maintenance lock context is invalid.");
                return false;
            }

            return true;
        }

        try
        {
            var contract = DatabaseContractReader.Read(Path.Combine(
                AppContext.BaseDirectory,
                "test-database-contract.json"));
            var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Pooling = false,
            };
            var database = connectionBuilder.Database ?? string.Empty;
            var protectedTarget = database == contract.Targets.TestDatabase
                                  || database.StartsWith(
                                      contract.Targets.RefreshPrefix,
                                      StringComparison.Ordinal);
            if (!protectedTarget && guard is null)
            {
                return true;
            }

            var runId = environment(RunIdEnvironmentVariable);
            var commandName = environment(CommandEnvironmentVariable);
            if (guard != "exclusive-v1"
                || !long.TryParse(
                    environment(LockKeyEnvironmentVariable),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var lockKey)
                || lockKey != contract.AdvisoryLock.Key)
            {
                error.WriteLine("ERROR: the TestRuntime maintenance lock context is invalid.");
                return false;
            }

            await using var connection = new NpgsqlConnection(connectionBuilder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            if (await AdvisoryLockProtocol.VerifyOwnershipAsync(
                    connection,
                    contract.AdvisoryLock.Key,
                    runId!,
                    commandName!,
                    AdvisoryLockMode.Exclusive,
                    cancellationToken))
            {
                return true;
            }
        }
        catch (Exception exception) when (exception is NpgsqlException
                                          or ArgumentException
                                          or IOException
                                          or System.Text.Json.JsonException)
        {
            // The caller receives only a sanitized refusal; credentials and raw settings stay private.
        }

        error.WriteLine("ERROR: the expected TestRuntime exclusive keeper is not active; canonical mutation was refused.");
        return false;
    }
}
