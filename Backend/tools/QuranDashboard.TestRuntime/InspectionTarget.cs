using Npgsql;

namespace QuranDashboard.TestRuntime;

internal sealed record InspectionTargetValidation(
    bool IsValid,
    string? Database,
    string? EndpointKind,
    NpgsqlConnectionStringBuilder? Connection,
    IReadOnlyList<ContractViolation> Violations);

internal static class InspectionTargetValidator
{
    internal static InspectionTargetValidation Validate(string connectionString, DatabaseContract contract)
    {
        NpgsqlConnectionStringBuilder connection;
        try
        {
            connection = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            return Invalid("target.connection-string.invalid");
        }

        var violations = new List<ContractViolation>();
        var database = connection.Database;
        if (database == contract.Targets.DevelopmentDatabase)
        {
            violations.Add(new ContractViolation("target.development-database"));
        }
        else if (database != contract.Targets.TestDatabase)
        {
            violations.Add(new ContractViolation("target.unknown-database"));
        }

        var endpointKind = LocalEndpointKind(connection.Host);
        if (endpointKind is null)
        {
            violations.Add(new ContractViolation("target.remote"));
        }

        return new InspectionTargetValidation(
            violations.Count == 0,
            string.IsNullOrWhiteSpace(database) ? null : database,
            endpointKind,
            violations.Count == 0 ? connection : null,
            violations);
    }

    private static string? LocalEndpointKind(string? host)
    {
        if (host is "localhost" or "127.0.0.1" or "::1")
        {
            return "loopback";
        }

        return !string.IsNullOrWhiteSpace(host)
               && Path.IsPathRooted(host)
               && !host.Contains(',', StringComparison.Ordinal)
            ? "unix-socket"
            : null;
    }

    private static InspectionTargetValidation Invalid(string code) => new(
        false,
        null,
        null,
        null,
        [new ContractViolation(code)]);
}
