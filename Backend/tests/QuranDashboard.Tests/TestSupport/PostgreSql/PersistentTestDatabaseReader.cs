using QuranDashboard.Infrastructure.Testing.DatabaseActivity;
using QuranDashboard.TestRuntime;

namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal sealed class PersistentTestDatabaseReader(bool guarded = false) : IAsyncDisposable
{
    internal const string RunnerContextVariable = "QURAN_DASHBOARD_TEST_RUNTIME_READER_CONTEXT";
    internal const string RunnerContextValue = "verified-v1";

    private static int runSequence;

    private AdvisoryLockLease? sharedLock;

    internal string BaseConnectionString { get; private set; } = string.Empty;

    internal string ReadOnlyConnectionString { get; private set; } = string.Empty;

    internal async Task InitializeAsync()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(RunnerContextVariable),
                RunnerContextValue,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Persistent Test Database readers require the repository runner. Run the focused selection through scripts/test.");
        }

        BaseConnectionString = Environment.GetEnvironmentVariable(
            TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"Persistent Test Database readers require {TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable}.");

        using var output = new StringWriter();
        using var error = new StringWriter();
        var inspectionExitCode = await TestRuntimeCommand.ExecuteAsync(
            ["inspect"],
            output,
            error);
        if (inspectionExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Persistent Test Database capability inspection failed with exit code {inspectionExitCode}: {output}{error}");
        }

        var readOnlyPolicy = DatabaseActivityPolicy.Testing(DatabaseActivityProfile.ReadOnly, []);
        ReadOnlyConnectionString = readOnlyPolicy.ApplyToConnectionString(BaseConnectionString);

        if (!guarded)
        {
            return;
        }

        var contract = DatabaseContractReader.Read(Path.Combine(
            AppContext.BaseDirectory,
            "test-database-contract.json"));
        var runId = $"reader-{Environment.ProcessId}-{Interlocked.Increment(ref runSequence)}";
        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            BaseConnectionString,
            contract.AdvisoryLock.Key,
            AdvisoryLockMode.Shared,
            runId,
            "guarded-reader");
        sharedLock = acquisition.Lease
            ?? throw new InvalidOperationException(
                $"GuardedReader could not acquire the shared TestRuntime lock after {acquisition.Report.WaitMilliseconds} ms.");
    }

    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrWhiteSpace(ReadOnlyConnectionString))
        {
            NpgsqlConnection.ClearPool(new NpgsqlConnection(ReadOnlyConnectionString));
        }

        if (sharedLock is not null)
        {
            await sharedLock.DisposeAsync();
            sharedLock = null;
        }
    }
}
