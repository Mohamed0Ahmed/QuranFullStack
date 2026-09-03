using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    private static readonly JsonSerializerOptions StreamJsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task<int> ExecuteAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        Func<string, string?>? readEnvironment = null,
        TextReader? input = null,
        CancellationToken cancellationToken = default,
        CapabilityRefreshDependencies? refreshDependencies = null)
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
            if (request.Command == "fingerprint")
            {
                await using var connection = new NpgsqlConnection(targetValidation.Connection!.ConnectionString);
                await connection.OpenAsync(cancellationToken);
                var fingerprint = await ProtectedStateFingerprint.ComputeAsync(
                    connection,
                    contract,
                    cancellationToken);
                var fingerprintReportEnvelope = new TestRuntimeReport(
                    request.Command,
                    true,
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
                    [],
                    ProtectedStateFingerprint: fingerprint);
                WriteReport(output, fingerprintReportEnvelope);
                return SuccessExitCode;
            }

            if (request.ScratchAction is not null)
            {
                var scratch = request.ScratchAction switch
                {
                    "create" => await ScratchDatabaseLifecycle.CreateAsync(
                        contract,
                        targetValidation,
                        request.RunId!,
                        request.LockCommand!,
                        request.ScratchSubtype!,
                        cancellationToken),
                    "resolve" => await ScratchDatabaseLifecycle.ResolveAsync(
                        contract,
                        targetValidation,
                        request.RunId!,
                        request.LockCommand!,
                        request.ScratchSubtype!,
                        cancellationToken),
                    "cleanup" => await ScratchDatabaseLifecycle.CleanupAsync(
                        contract,
                        targetValidation,
                        request.RunId!,
                        request.LockCommand!,
                        cancellationToken),
                    "reap" => await ScratchDatabaseLifecycle.ReapAsync(
                        contract,
                        targetValidation,
                        request.RunId!,
                        request.LockCommand!,
                        cancellationToken),
                    _ => throw new InvalidOperationException("Unsupported scratch lifecycle action."),
                };
                WriteReport(output, new TestRuntimeReport(
                    request.Command,
                    scratch.Succeeded,
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
                    scratch.Violations,
                    Scratch: scratch.Report));
                return scratch.Succeeded ? SuccessExitCode : ValidationFailureExitCode;
            }

            if (request.Command == "reset")
            {
                var inspection = await DatabaseInspector.InspectAsync(
                    contract,
                    validation,
                    targetValidation,
                    cancellationToken);
                var resetReport = await MutableStateResetter.ExecuteAsync(
                    contract,
                    validation,
                    targetValidation,
                    inspection,
                    request.RunId!,
                    request.LockCommand!,
                    request.ExpectedFingerprint!,
                    request.ApiPort!.Value,
                    request.ApiProcessId,
                    request.ResetPhase!,
                    cancellationToken);
                WriteReport(output, resetReport);
                return resetReport.Succeeded ? SuccessExitCode : ValidationFailureExitCode;
            }

            if (request.RefreshMode is not null)
            {
                var refreshReport = await CapabilityRefresher.ExecuteAsync(
                    contract,
                    validation,
                    targetValidation,
                    new CapabilityRefreshRequest(
                        request.RefreshMode.Value,
                        request.SelectedLogin!,
                        request.RunId,
                        request.RefreshReason,
                        request.RefreshConfirmed,
                        request.LockTimeout),
                    refreshDependencies ?? CapabilityRefreshDependencies.CreateDefault(request.ContractPath),
                    cancellationToken);
                WriteReport(output, refreshReport);
                return refreshReport.Succeeded ? SuccessExitCode : ValidationFailureExitCode;
            }

            if (request.LockMode is not null)
            {
                return await HoldLockAsync(
                    request,
                    contract,
                    validation,
                    targetValidation,
                    output,
                    input,
                    cancellationToken);
            }

            TestRuntimeReport report;
            if (request.AdministrationMode == CapabilityAdministrationMode.Apply)
            {
                var acquisition = await AdvisoryLockProtocol.AcquireAsync(
                    targetValidation.Connection!.ConnectionString,
                    contract.AdvisoryLock.Key,
                    AdvisoryLockMode.Exclusive,
                    request.RunId!,
                    "capability-admin",
                    request.LockTimeout,
                    cancellationToken);
                if (acquisition.Lease is null)
                {
                    report = CreateLockReport(
                        request.Command,
                        contract,
                        validation,
                        targetValidation,
                        acquisition.Report,
                        succeeded: false,
                        [new ContractViolation("lock.acquisition.timeout")]);
                }
                else
                {
                    await using (acquisition.Lease)
                    {
                        report = await CapabilityAdministrator.ExecuteAsync(
                            contract,
                            validation,
                            targetValidation,
                            request.AdministrationMode.Value,
                            request.SelectedLogin!,
                            request.RunId,
                            "capability-admin",
                            cancellationToken);
                        report = report with { AdvisoryLock = acquisition.Report };
                    }
                }
            }
            else
            {
                report = request.AdministrationMode is null
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
                        null,
                        null,
                        cancellationToken);
            }

            WriteReport(output, report);
            return report.Succeeded ? SuccessExitCode : ValidationFailureExitCode;
        }
        catch (Exception exception) when (exception is NpgsqlException
                                          or SocketException
                                          or IOException
                                          or JsonException
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
                [new ContractViolation(request.LockMode is not null
                    ? "lock.database-unavailable"
                    : request.ScratchAction is not null
                        ? exception is JsonException
                            ? "scratch.receipt.malformed"
                            : "scratch.database-operation-failed"
                        : request.Command == "reset"
                            ? "mutable-reset.database-operation-failed"
                            : request.Command == "fingerprint"
                                ? "fingerprint.database-operation-failed"
                                : request.RefreshMode is not null
                                    ? "refresh.database-operation-failed"
                                    : request.AdministrationMode is null
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
            ["fingerprint", ..] => "fingerprint",
            ["reset", ..] => "reset",
            ["inspect", ..] => "inspect",
            ["admin", "inspect", ..] => "admin-inspect",
            ["admin", "dry-run", ..] => "admin-dry-run",
            ["admin", "apply", ..] => "admin-apply",
            ["admin", "verify", ..] => "admin-verify",
            ["refresh", "inspect", ..] => "refresh-inspect",
            ["refresh", "dry-run", ..] => "refresh-dry-run",
            ["refresh", "apply", ..] => "refresh-apply",
            ["refresh", "verify", ..] => "refresh-verify",
            ["lock", "hold", ..] => "lock-hold",
            ["scratch", "create", ..] => "scratch-create",
            ["scratch", "resolve", ..] => "scratch-resolve",
            ["scratch", "cleanup", ..] => "scratch-cleanup",
            ["scratch", "reap", ..] => "scratch-reap",
            _ => null,
        };
        if (command is null)
        {
            return null;
        }

        var optionStart = command is "contract-validate" or "lock-hold"
                          || command.StartsWith("admin-", StringComparison.Ordinal)
                          || command.StartsWith("refresh-", StringComparison.Ordinal)
                          || command.StartsWith("scratch-", StringComparison.Ordinal)
            ? 2
            : 1;
        var contractPath = Path.Combine(AppContext.BaseDirectory, "test-database-contract.json");
        string? selectedLogin = null;
        string? runId = null;
        string? lockCommand = null;
        string? refreshReason = null;
        var refreshConfirmed = false;
        AdvisoryLockMode? lockMode = null;
        TimeSpan? lockTimeout = null;
        string? expectedFingerprint = null;
        int? apiPort = null;
        int? apiProcessId = null;
        var apiProcessProofProvided = false;
        string? resetPhase = null;
        string? scratchSubtype = null;
        var releaseOnStdinClose = false;
        for (var index = optionStart; index < args.Count;)
        {
            if (args[index] == "--yes" && command == "refresh-apply" && !refreshConfirmed)
            {
                refreshConfirmed = true;
                index++;
                continue;
            }

            if (args[index] == "--release-on-stdin-close"
                && command == "lock-hold"
                && !releaseOnStdinClose)
            {
                releaseOnStdinClose = true;
                index++;
                continue;
            }

            if (index + 1 >= args.Count)
            {
                return null;
            }

            if (args[index] == "--contract"
                && command is not "admin-apply" and not "lock-hold" and not "reset"
                && !command.StartsWith("refresh-", StringComparison.Ordinal)
                && !command.StartsWith("scratch-", StringComparison.Ordinal))
            {
                contractPath = args[index + 1];
            }
            else if (args[index] == "--login" && command.StartsWith("admin-", StringComparison.Ordinal))
            {
                selectedLogin = args[index + 1];
            }
            else if (args[index] == "--login" && command.StartsWith("refresh-", StringComparison.Ordinal))
            {
                selectedLogin = args[index + 1];
            }
            else if (args[index] == "--run-id"
                     && (command is "admin-apply" or "lock-hold" or "reset" or "refresh-apply"
                         || command.StartsWith("scratch-", StringComparison.Ordinal)))
            {
                runId = args[index + 1];
            }
            else if (args[index] == "--reason" && command == "refresh-apply")
            {
                refreshReason = args[index + 1];
            }
            else if (args[index] == "--command"
                     && (command is "lock-hold" or "reset"
                         || command.StartsWith("scratch-", StringComparison.Ordinal)))
            {
                lockCommand = args[index + 1];
            }
            else if (args[index] == "--mode" && command == "lock-hold")
            {
                lockMode = args[index + 1] switch
                {
                    "shared" => AdvisoryLockMode.Shared,
                    "exclusive" => AdvisoryLockMode.Exclusive,
                    _ => null,
                };
            }
            else if (args[index] == "--timeout-seconds"
                     && (command == "admin-apply" || command == "lock-hold")
                     && int.TryParse(args[index + 1], out var timeoutSeconds)
                     && timeoutSeconds >= 0)
            {
                lockTimeout = TimeSpan.FromSeconds(timeoutSeconds);
            }
            else if (args[index] == "--expected-fingerprint"
                     && command == "reset"
                     && Regex.IsMatch(args[index + 1], "^[A-Fa-f0-9]{64}$", RegexOptions.CultureInvariant))
            {
                expectedFingerprint = args[index + 1];
            }
            else if (args[index] == "--api-port"
                     && command == "reset"
                     && int.TryParse(args[index + 1], out var parsedPort)
                     && parsedPort is >= 1 and <= 65535)
            {
                apiPort = parsedPort;
            }
            else if (args[index] == "--api-process-id" && command == "reset")
            {
                apiProcessProofProvided = true;
                if (args[index + 1] == "none")
                {
                    apiProcessId = null;
                }
                else if (int.TryParse(args[index + 1], out var parsedProcessId) && parsedProcessId > 0)
                {
                    apiProcessId = parsedProcessId;
                }
                else
                {
                    return null;
                }
            }
            else if (args[index] == "--phase"
                     && command == "reset"
                     && args[index + 1] is "initial" or "final")
            {
                resetPhase = args[index + 1];
            }
            else if (args[index] == "--subtype"
                     && command is "scratch-create" or "scratch-resolve")
            {
                scratchSubtype = args[index + 1];
            }
            else
            {
                return null;
            }

            index += 2;
        }

        var administrationMode = command switch
        {
            "admin-inspect" => CapabilityAdministrationMode.Inspect,
            "admin-dry-run" => CapabilityAdministrationMode.DryRun,
            "admin-apply" => CapabilityAdministrationMode.Apply,
            "admin-verify" => CapabilityAdministrationMode.Verify,
            _ => (CapabilityAdministrationMode?)null,
        };
        var refreshMode = command switch
        {
            "refresh-inspect" => CapabilityRefreshMode.Inspect,
            "refresh-dry-run" => CapabilityRefreshMode.DryRun,
            "refresh-apply" => CapabilityRefreshMode.Apply,
            "refresh-verify" => CapabilityRefreshMode.Verify,
            _ => (CapabilityRefreshMode?)null,
        };
        var invalidAdministration = administrationMode is not null && string.IsNullOrWhiteSpace(selectedLogin);
        var invalidApply = command == "admin-apply" && !AdvisoryLockProtocol.IsValidRunId(runId);
        var invalidRefresh = command.StartsWith("refresh-", StringComparison.Ordinal)
                             && (string.IsNullOrWhiteSpace(selectedLogin)
                                 || (command == "refresh-apply"
                                     && (!AdvisoryLockProtocol.IsValidRunId(runId)
                                         || string.IsNullOrWhiteSpace(refreshReason)
                                         || refreshReason.Length > 256
                                         || !refreshConfirmed)));
        var invalidLock = command == "lock-hold"
                          && (lockMode is null
                              || !AdvisoryLockProtocol.IsValidRunId(runId)
                              || !AdvisoryLockProtocol.IsValidCommand(lockCommand));
        var invalidReset = command == "reset"
                           && (!AdvisoryLockProtocol.IsValidRunId(runId)
                               || !AdvisoryLockProtocol.IsValidCommand(lockCommand)
                               || expectedFingerprint is null
                               || apiPort is null
                               || !apiProcessProofProvided
                               || (resetPhase == "final" && apiProcessId is null)
                               || resetPhase is null);
        var scratchAction = command.StartsWith("scratch-", StringComparison.Ordinal)
            ? command["scratch-".Length..]
            : null;
        var invalidScratch = scratchAction is not null
                             && (!AdvisoryLockProtocol.IsValidRunId(runId)
                                 || !AdvisoryLockProtocol.IsValidCommand(lockCommand)
                                 || (scratchAction is "create" or "resolve"
                                     && string.IsNullOrWhiteSpace(scratchSubtype))
                                 || (scratchAction is "cleanup" or "reap"
                                     && scratchSubtype is not null));
        return string.IsNullOrWhiteSpace(contractPath)
               || invalidAdministration
               || invalidApply
               || invalidRefresh
               || invalidLock
               || invalidReset
               || invalidScratch
            ? null
            : new CommandRequest(
                command,
                Path.GetFullPath(contractPath),
                administrationMode,
                selectedLogin,
                runId,
                lockMode,
                lockCommand,
                lockTimeout,
                expectedFingerprint,
                apiPort,
                apiProcessId,
                resetPhase,
                refreshReason,
                refreshConfirmed,
                refreshMode,
                scratchAction,
                scratchSubtype,
                releaseOnStdinClose);
    }

    private static async Task<int> HoldLockAsync(
        CommandRequest request,
        DatabaseContract contract,
        ContractValidationResult validation,
        InspectionTargetValidation targetValidation,
        TextWriter output,
        TextReader? input,
        CancellationToken cancellationToken)
    {
        var acquisition = await AdvisoryLockProtocol.AcquireAsync(
            targetValidation.Connection!.ConnectionString,
            contract.AdvisoryLock.Key,
            request.LockMode!.Value,
            request.RunId!,
            request.LockCommand!,
            request.LockTimeout,
            cancellationToken);
        if (acquisition.Lease is null)
        {
            WriteReport(
                output,
                CreateLockReport(
                    request.Command,
                    contract,
                    validation,
                    targetValidation,
                    acquisition.Report,
                    succeeded: false,
                    [new ContractViolation("lock.acquisition.timeout")]),
                stream: true);
            await output.FlushAsync();
            return ValidationFailureExitCode;
        }

        await using (acquisition.Lease)
        {
            WriteReport(
                output,
                CreateLockReport(
                    request.Command,
                    contract,
                    validation,
                    targetValidation,
                    acquisition.Report,
                    succeeded: true,
                    []),
                stream: true);
            await output.FlushAsync();
            try
            {
                if (request.ReleaseOnStdinClose)
                {
                    await (input ?? TextReader.Null).ReadLineAsync(cancellationToken);
                }
                else
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Closing the keeper connection below releases the session-level lock.
            }
        }

        var released = acquisition.Report with { Status = "released" };
        WriteReport(
            output,
            CreateLockReport(
                request.Command,
                contract,
                validation,
                targetValidation,
                released,
                succeeded: true,
                []),
            stream: true);
        await output.FlushAsync();
        return SuccessExitCode;
    }

    private static TestRuntimeReport CreateLockReport(
        string command,
        DatabaseContract contract,
        ContractValidationResult validation,
        InspectionTargetValidation targetValidation,
        AdvisoryLockReport advisoryLock,
        bool succeeded,
        IReadOnlyList<ContractViolation> violations) => new(
        command,
        succeeded,
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
        violations,
        AdvisoryLock: advisoryLock);

    private static void WriteReport(TextWriter output, TestRuntimeReport report, bool stream = false)
    {
        output.WriteLine(JsonSerializer.Serialize(report, stream ? StreamJsonOptions : ReportJsonOptions));
    }

    private static void WriteUsage(TextWriter error)
    {
        error.WriteLine("Usage:");
        error.WriteLine("  QuranDashboard.TestRuntime contract validate [--contract <path>]");
        error.WriteLine($"  QuranDashboard.TestRuntime fingerprint [--contract <path>]  # reads {DefaultConnectionStringEnvironmentVariable}");
        error.WriteLine($"  QuranDashboard.TestRuntime reset --run-id <run-id> --command <command> --expected-fingerprint <sha256> --api-port <port> --api-process-id <pid|none> --phase initial|final  # reads {DefaultConnectionStringEnvironmentVariable}");
        error.WriteLine($"  QuranDashboard.TestRuntime inspect [--contract <path>]  # reads {DefaultConnectionStringEnvironmentVariable}");
        error.WriteLine($"  QuranDashboard.TestRuntime admin inspect|dry-run|verify --login <local-login> [--contract <path>]  # reads {DefaultConnectionStringEnvironmentVariable}");
        error.WriteLine($"  QuranDashboard.TestRuntime admin apply --login <local-login> --run-id <run-id> [--timeout-seconds <seconds>]  # reads {DefaultConnectionStringEnvironmentVariable}");
        error.WriteLine($"  QuranDashboard.TestRuntime refresh inspect|dry-run|verify --login <local-login>  # reads {DefaultConnectionStringEnvironmentVariable}");
        error.WriteLine($"  QuranDashboard.TestRuntime refresh apply --login <local-login> --run-id <run-id> --reason <reason> --yes  # reads {DefaultConnectionStringEnvironmentVariable}");
        error.WriteLine($"  QuranDashboard.TestRuntime lock hold --mode shared|exclusive --run-id <run-id> --command <command> [--timeout-seconds <seconds>] [--release-on-stdin-close]  # reads {DefaultConnectionStringEnvironmentVariable}");
        error.WriteLine($"  QuranDashboard.TestRuntime scratch create|resolve --run-id <run-id> --command <command> --subtype <subtype>  # reads {DefaultConnectionStringEnvironmentVariable}");
        error.WriteLine($"  QuranDashboard.TestRuntime scratch cleanup|reap --run-id <run-id> --command <command>  # reads {DefaultConnectionStringEnvironmentVariable}");
    }

    private sealed record CommandRequest(
        string Command,
        string ContractPath,
        CapabilityAdministrationMode? AdministrationMode = null,
        string? SelectedLogin = null,
        string? RunId = null,
        AdvisoryLockMode? LockMode = null,
        string? LockCommand = null,
        TimeSpan? LockTimeout = null,
        string? ExpectedFingerprint = null,
        int? ApiPort = null,
        int? ApiProcessId = null,
        string? ResetPhase = null,
        string? RefreshReason = null,
        bool RefreshConfirmed = false,
        CapabilityRefreshMode? RefreshMode = null,
        string? ScratchAction = null,
        string? ScratchSubtype = null,
        bool ReleaseOnStdinClose = false);
}
