using System.Globalization;

namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal static class PostgreSqlResourceLabels
{
    internal const string RunIdVariable = "QURAN_DASHBOARD_TEST_RUN_ID";

    internal const string OwnerLabel = "com.qurandashboard.test.owner";
    internal const string RepositoryLabel = "com.qurandashboard.test.repository";
    internal const string RunIdLabel = "com.qurandashboard.test.run-id";
    internal const string KindLabel = "com.qurandashboard.test.kind";
    internal const string HostProcessIdLabel = "com.qurandashboard.test.host-pid";

    internal const string OwnerValue = "backend-tests";
    internal const string RepositoryValue = "quran-dashboard";
    internal const string PostgreSqlKindValue = "postgresql";

    private static readonly Lazy<string> RunIdLoader = new(ResolveRunId);

    internal static string RunId => RunIdLoader.Value;

    internal static IReadOnlyDictionary<string, string> ForPostgreSql()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OwnerLabel] = OwnerValue,
            [RepositoryLabel] = RepositoryValue,
            [RunIdLabel] = RunId,
            [KindLabel] = PostgreSqlKindValue,
            [HostProcessIdLabel] = Environment.ProcessId.ToString(CultureInfo.InvariantCulture)
        };
    }

    internal static bool IsRunId(string? candidate)
    {
        return candidate is { Length: 32 }
            && candidate.All(character => character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
    }

    private static string ResolveRunId()
    {
        var configured = Environment.GetEnvironmentVariable(RunIdVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return Guid.NewGuid().ToString("N");
        }

        if (!IsRunId(configured))
        {
            throw new InvalidOperationException(
                $"{RunIdVariable} must be 32 lowercase hexadecimal characters, found '{configured}'. "
                + "Cleanup selects containers by that exact run ID.");
        }

        return configured;
    }
}
