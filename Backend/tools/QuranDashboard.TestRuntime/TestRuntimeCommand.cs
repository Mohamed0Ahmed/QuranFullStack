using System.Net.Sockets;
using System.Text.Json;
using Npgsql;

namespace QuranDashboard.TestRuntime;

internal static class TestRuntimeCommand
{
    internal const string DefaultConnectionStringEnvironmentVariable = "ConnectionStrings__QuranDashboardTest";

    private const int SuccessExitCode = 0;
    private const int UsageExitCode = 2;
    private const int ValidationFailureExitCode = 3;
    private const int OperationalFailureExitCode = 4;

    private static readonly JsonSerializerOptions ReportJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    internal static async Task<int> ExecuteAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        Func<string, string?>? readEnvironment = null,
        CancellationToken cancellationToken = default)
    {
        var request = Parse(args);
        if (request is null)
        {
            WriteUsage(error);
            return UsageExitCode;
        }

        DatabaseContract contract;
        ContractValidationResult validation;
        try
        {
            contract = DatabaseContractReader.Read(request.ContractPath);
            validation = DatabaseContractValidator.Validate(contract);
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException
                                          or JsonException
                                          or NotSupportedException)
        {
            var code = exception is JsonException or NotSupportedException
                ? "contract.malformed"
                : "contract.unavailable";
            WriteReport(output, new TestRuntimeReport(
                request.Command,
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                [new ContractViolation(code)],
                exception.GetType().Name));
            return ValidationFailureExitCode;
        }

        if (!validation.IsValid)
        {
            WriteReport(output, new TestRuntimeReport(
                request.Command,
                false,
                DatabaseInspector.ToContractReport(contract, validation),
                null,
                null,
                null,
                null,
                null,
                validation.Violations));
            return ValidationFailureExitCode;
        }

        if (request.Command == "contract-validate")
        {
            WriteReport(output, new TestRuntimeReport(
                request.Command,
                true,
                DatabaseInspector.ToContractReport(contract, validation),
                null,
                null,
                null,
                null,
                null,
                []));
            return SuccessExitCode;
        }

        var environment = readEnvironment ?? Environment.GetEnvironmentVariable;
        var connectionString = environment(DefaultConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            WriteReport(output, new TestRuntimeReport(
                request.Command,
                false,
                DatabaseInspector.ToContractReport(contract, validation),
                null,
                null,
                null,
                null,
                null,
                [new ContractViolation("target.connection-string.missing")]));
            return ValidationFailureExitCode;
        }

        var targetValidation = InspectionTargetValidator.Validate(connectionString, contract);
        if (!targetValidation.IsValid)
        {
            WriteReport(output, new TestRuntimeReport(
                request.Command,
                false,
                DatabaseInspector.ToContractReport(contract, validation),
                new TargetReport(
                    targetValidation.Database,
                    targetValidation.EndpointKind,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null),
                null,
                null,
                null,
                null,
                targetValidation.Violations));
            return ValidationFailureExitCode;
        }

        try
        {
            var report = request.AdministrationMode is null
                ? await DatabaseInspector.InspectAsync(
                    contract,
                    validation,
                    targetValidation,
                    cancellationToken)
                : await CapabilityAdministrator.ExecuteAsync(
                    contract,
                    validation,
                    targetValidation,
                    request.AdministrationMode.Value,
                    request.SelectedLogin!,
                    cancellationToken);
            WriteReport(output, report);
            return report.Succeeded ? SuccessExitCode : ValidationFailureExitCode;
        }
        catch (Exception exception) when (exception is NpgsqlException
                                          or SocketException
                                          or IOException
                                          or InvalidOperationException)
        {
            WriteReport(output, new TestRuntimeReport(
                request.Command,
                false,
                DatabaseInspector.ToContractReport(contract, validation),
                new TargetReport(
                    targetValidation.Database,
                    targetValidation.EndpointKind,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null),
                null,
                null,
                null,
                null,
                [new ContractViolation(request.AdministrationMode is null
                    ? "inspection.database-unavailable"
                    : "administration.database-operation-failed",
                    exception is PostgresException postgresException ? postgresException.SqlState : null)],
                exception.GetType().Name));
            return OperationalFailureExitCode;
        }
    }

    private static CommandRequest? Parse(IReadOnlyList<string> args)
    {
        var command = args switch
        {
            ["contract", "validate", ..] => "contract-validate",
            ["inspect", ..] => "inspect",
            ["admin", "inspect", ..] => "admin-inspect",
            ["admin", "dry-run", ..] => "admin-dry-run",
            ["admin", "apply", ..] => "admin-apply",
            ["admin", "verify", ..] => "admin-verify",
            _ => null,
        };
        if (command is null)
        {
            return null;
        }

        var optionStart = command == "contract-validate" || command.StartsWith("admin-", StringComparison.Ordinal) ? 2 : 1;
        var contractPath = Path.Combine(AppContext.BaseDirectory, "test-database-contract.json");
        string? selectedLogin = null;
        for (var index = optionStart; index < args.Count; index += 2)
        {
            if (index + 1 >= args.Count)
            {
                return null;
            }

            if (args[index] == "--contract")
            {
                contractPath = args[index + 1];
            }
            else if (args[index] == "--login" && command.StartsWith("admin-", StringComparison.Ordinal))
            {
                selectedLogin = args[index + 1];
            }
            else
            {
                return null;
            }
        }

        var administrationMode = command switch
        {
            "admin-inspect" => CapabilityAdministrationMode.Inspect,
            "admin-dry-run" => CapabilityAdministrationMode.DryRun,
            "admin-apply" => CapabilityAdministrationMode.Apply,
            "admin-verify" => CapabilityAdministrationMode.Verify,
            _ => (CapabilityAdministrationMode?)null,
        };
        return string.IsNullOrWhiteSpace(contractPath)
               || (administrationMode is not null && string.IsNullOrWhiteSpace(selectedLogin))
            ? null
            : new CommandRequest(command, Path.GetFullPath(contractPath), administrationMode, selectedLogin);
    }

    private static void WriteReport(TextWriter output, TestRuntimeReport report)
    {
        output.WriteLine(JsonSerializer.Serialize(report, ReportJsonOptions));
    }

    private static void WriteUsage(TextWriter error)
    {
        error.WriteLine("Usage:");
        error.WriteLine("  QuranDashboard.TestRuntime contract validate [--contract <path>]");
        error.WriteLine($"  QuranDashboard.TestRuntime inspect [--contract <path>]  # reads {DefaultConnectionStringEnvironmentVariable}");
        error.WriteLine($"  QuranDashboard.TestRuntime admin inspect|dry-run|apply|verify --login <local-login> [--contract <path>]  # reads {DefaultConnectionStringEnvironmentVariable}");
    }

    private sealed record CommandRequest(
        string Command,
        string ContractPath,
        CapabilityAdministrationMode? AdministrationMode = null,
        string? SelectedLogin = null);
}
