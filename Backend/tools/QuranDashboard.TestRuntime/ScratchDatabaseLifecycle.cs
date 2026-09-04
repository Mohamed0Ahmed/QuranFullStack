using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;

namespace QuranDashboard.TestRuntime;

internal sealed record ScratchDatabaseReceipt(
    int Version,
    string Database,
    string Owner,
    string RunId,
    string Command,
    string Subtype,
    string Receipt,
    string ServerScope,
    DateTimeOffset CreatedAtUtc);

internal sealed record ScratchLifecycleResult(
    bool Succeeded,
    ScratchLifecycleReport Report,
    IReadOnlyList<ContractViolation> Violations,
    string? ConnectionString = null);

internal sealed record ScratchDatabaseExecutionContext(
    string Database,
    string ConnectionString,
    string RunId,
    string Command,
    string Subtype)
{
    internal const string RunIdEnvironmentVariable = "QURAN_DASHBOARD_TEST_RUN_ID";
    internal const string CommandEnvironmentVariable = "QURAN_DASHBOARD_TEST_LOCK_COMMAND";
    internal const string SubtypeEnvironmentVariable = "QURAN_DASHBOARD_TEST_SCRATCH_SUBTYPE";

    internal static async Task<ScratchDatabaseExecutionContext> ResolveAsync(
        string contractPath,
        Func<string, string?>? readEnvironment = null,
        CancellationToken cancellationToken = default)
    {
        var environment = readEnvironment ?? Environment.GetEnvironmentVariable;
        var connectionString = environment(TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable);
        var runId = environment(RunIdEnvironmentVariable);
        var command = environment(CommandEnvironmentVariable);
        var subtype = environment(SubtypeEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString)
            || string.IsNullOrWhiteSpace(runId)
            || string.IsNullOrWhiteSpace(command)
            || string.IsNullOrWhiteSpace(subtype))
        {
            throw new InvalidOperationException(
                "Empty-scratch execution requires the repository test runner and its complete TestRuntime context.");
        }

        var contract = DatabaseContractReader.Read(contractPath);
        var validation = DatabaseContractValidator.Validate(contract);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("The committed TestRuntime database contract is invalid.");
        }

        var target = InspectionTargetValidator.Validate(connectionString, contract);
        if (!target.IsValid)
        {
            throw new InvalidOperationException(
                "The TestRuntime base connection must resolve the exact local persistent Test Database.");
        }

        var resolved = await ScratchDatabaseLifecycle.ResolveAsync(
            contract,
            target,
            runId,
            command,
            subtype,
            cancellationToken);
        if (!resolved.Succeeded || resolved.ConnectionString is null || resolved.Report.Database is null)
        {
            throw new InvalidOperationException(
                "The empty-scratch target was refused: "
                + string.Join(",", resolved.Violations.Select(violation => violation.Code)));
        }

        return new ScratchDatabaseExecutionContext(
            resolved.Report.Database,
            resolved.ConnectionString,
            runId,
            command,
            subtype);
    }
}

internal static class ScratchDatabaseReceiptStore
{
    private const int ReceiptVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    internal static string RootPath { get; } = Path.Combine(
        Path.GetTempPath(),
        "quran-dashboard-test-runtime",
        "scratch-receipts");

    internal static ScratchDatabaseReceipt Create(
        string database,
        string owner,
        string runId,
        string command,
        string subtype,
        string serverScope) => new(
        ReceiptVersion,
        database,
        owner,
        runId,
        command,
        subtype,
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
        serverScope,
        DateTimeOffset.UtcNow);

    internal static async Task WriteAsync(
        ScratchDatabaseReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(RootPath);
        var path = PathFor(receipt.RunId);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 4096,
                             FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, receipt, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(
                    temporaryPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            File.Move(temporaryPath, path, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    internal static ScratchDatabaseReceipt? Read(string runId)
    {
        var path = PathFor(runId);
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<ScratchDatabaseReceipt>(stream, JsonOptions);
    }

    internal static IReadOnlyList<ScratchDatabaseReceipt> ReadAll()
    {
        if (!Directory.Exists(RootPath))
        {
            return [];
        }

        var receipts = new List<ScratchDatabaseReceipt>();
        foreach (var path in Directory.EnumerateFiles(RootPath, "*.json").Order(StringComparer.Ordinal))
        {
            using var stream = File.OpenRead(path);
            var receipt = JsonSerializer.Deserialize<ScratchDatabaseReceipt>(stream, JsonOptions)
                ?? throw new JsonException("A scratch receipt was empty.");
            receipts.Add(receipt);
        }

        return receipts;
    }

    internal static void Delete(string runId)
    {
        var path = PathFor(runId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string PathFor(string runId)
    {
        if (!AdvisoryLockProtocol.IsValidRunId(runId))
        {
            throw new ArgumentException("The scratch run ID is invalid.", nameof(runId));
        }

        return Path.Combine(RootPath, $"{runId}.json");
    }
}

internal static class ScratchDatabaseLifecycle
{
    private const string TemplateDatabase = "template0";
    private const int ReceiptVersion = 1;

    private static readonly HashSet<string> EmptyScratchSubtypes = new(StringComparer.Ordinal)
    {
        "canonical-import",
        "canonical-rebuild",
        "canonical-generation",
        "migration",
        "system-catalogue-reconciliation",
        "schema-drift",
    };

    internal static async Task<ScratchLifecycleResult> CreateAsync(
        DatabaseContract contract,
        InspectionTargetValidation target,
        string runId,
        string command,
        string subtype,
        CancellationToken cancellationToken = default)
    {
        var report = CreateReport("create", contract, runId, subtype);
        var violations = ValidateRequest(contract, runId, command, subtype);
        if (violations.Count != 0)
        {
            return Failed(report, violations);
        }

        await using var maintenance = await OpenMaintenanceConnectionAsync(target, cancellationToken);
        violations.AddRange(await ValidateServerAndOwnerAsync(maintenance, contract, cancellationToken));
        if (!await AdvisoryLockProtocol.VerifyOwnershipAsync(
                maintenance,
                contract.AdvisoryLock.Key,
                runId,
                command,
                AdvisoryLockMode.Exclusive,
                cancellationToken))
        {
            violations.Add(new ContractViolation("scratch.lock.not-owned"));
        }

        var database = ScratchDatabaseName(contract, runId);
        var serverScope = ServerScope(target);
        if (await DatabaseExistsAsync(maintenance, database, cancellationToken))
        {
            violations.Add(new ContractViolation("scratch.database.already-exists"));
        }

        if (ScratchDatabaseReceiptStore.Read(runId) is not null)
        {
            violations.Add(new ContractViolation("scratch.receipt.already-exists"));
        }

        if (violations.Count != 0)
        {
            return Failed(report, violations);
        }

        var receipt = ScratchDatabaseReceiptStore.Create(
            database,
            contract.Roles.ScratchAdministrator,
            runId,
            command,
            subtype,
            serverScope);
        await ScratchDatabaseReceiptStore.WriteAsync(receipt, cancellationToken);

        try
        {
            await ExecuteAsScratchOwnerAsync(
                maintenance,
                contract.Roles.ScratchAdministrator,
                $"CREATE DATABASE {PostgreSqlIdentifier.Quote(database)} "
                + $"OWNER {PostgreSqlIdentifier.Quote(contract.Roles.ScratchAdministrator)} "
                + $"TEMPLATE {PostgreSqlIdentifier.Quote(TemplateDatabase)}",
                cancellationToken);
        }
        catch (PostgresException exception)
        {
            return Failed(
                report with { ReceiptRecorded = true },
                [new ContractViolation("scratch.database.create-failed", exception.SqlState)]);
        }

        try
        {
            foreach (var marker in ScratchMarkers(contract, receipt))
            {
                await using var markerCommand = new NpgsqlCommand(
                    $"ALTER DATABASE {PostgreSqlIdentifier.Quote(database)} SET {marker.Key} TO {QuoteLiteral(marker.Value)}",
                    maintenance);
                await markerCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch (PostgresException exception)
        {
            return Failed(
                report with { ReceiptRecorded = true },
                [new ContractViolation("scratch.marker.write-failed", exception.SqlState)]);
        }

        var resolved = await ResolveReceiptAsync(
            contract,
            target,
            receipt,
            runId,
            command,
            requireReceiptRunToOwnLock: true,
            cancellationToken);
        return resolved.Succeeded
            ? resolved with { Report = report with { ReceiptRecorded = true, Validated = true } }
            : resolved with { Report = report with { ReceiptRecorded = true } };
    }

    internal static async Task<ScratchLifecycleResult> ResolveAsync(
        DatabaseContract contract,
        InspectionTargetValidation target,
        string runId,
        string command,
        string subtype,
        CancellationToken cancellationToken = default)
    {
        var report = CreateReport("resolve", contract, runId, subtype);
        var violations = ValidateRequest(contract, runId, command, subtype);
        var receipt = ScratchDatabaseReceiptStore.Read(runId);
        if (receipt is null)
        {
            violations.Add(new ContractViolation("scratch.receipt.missing"));
        }

        if (violations.Count != 0)
        {
            return Failed(report, violations);
        }

        var resolved = await ResolveReceiptAsync(
            contract,
            target,
            receipt!,
            runId,
            command,
            requireReceiptRunToOwnLock: true,
            cancellationToken);
        return resolved with { Report = resolved.Report with { Mode = "resolve" } };
    }

    internal static async Task<ScratchLifecycleResult> CleanupAsync(
        DatabaseContract contract,
        InspectionTargetValidation target,
        string runId,
        string command,
        CancellationToken cancellationToken = default)
    {
        var report = CreateReport("cleanup", contract, runId, null);
        var receipt = ScratchDatabaseReceiptStore.Read(runId);
        if (receipt is null)
        {
            return Failed(report, [new ContractViolation("scratch.receipt.missing")]);
        }

        return await CleanupReceiptAsync(
            contract,
            target,
            receipt,
            runId,
            command,
            requireReceiptRunToOwnLock: true,
            cancellationToken);
    }

    internal static async Task<ScratchLifecycleResult> ReapAsync(
        DatabaseContract contract,
        InspectionTargetValidation target,
        string currentRunId,
        string command,
        CancellationToken cancellationToken = default)
    {
        var report = CreateReport("reap", contract, currentRunId, null);
        var serverScope = ServerScope(target);
        await using var maintenance = await OpenMaintenanceConnectionAsync(target, cancellationToken);
        if (!await AdvisoryLockProtocol.VerifyOwnershipAsync(
                maintenance,
                contract.AdvisoryLock.Key,
                currentRunId,
                command,
                AdvisoryLockMode.Exclusive,
                cancellationToken))
        {
            return Failed(report, [new ContractViolation("scratch.lock.not-owned")]);
        }

        var removed = new List<string>();
        var violations = new List<ContractViolation>();
        foreach (var receipt in ScratchDatabaseReceiptStore.ReadAll()
                     .Where(receipt => receipt.ServerScope == serverScope))
        {
            var cleanup = await CleanupReceiptAsync(
                contract,
                target,
                receipt,
                currentRunId,
                command,
                requireReceiptRunToOwnLock: false,
                cancellationToken);
            if (cleanup.Succeeded)
            {
                removed.AddRange(cleanup.Report.RemovedDatabases);
            }
            else
            {
                violations.AddRange(cleanup.Violations);
            }
        }

        var orderedViolations = OrderViolations(violations);
        return new ScratchLifecycleResult(
            orderedViolations.Count == 0,
            report with
            {
                Validated = orderedViolations.Count == 0,
                Removed = removed.Count != 0,
                RemovedDatabases = removed.Order(StringComparer.Ordinal).ToArray(),
            },
            orderedViolations);
    }

    private static async Task<ScratchLifecycleResult> CleanupReceiptAsync(
        DatabaseContract contract,
        InspectionTargetValidation target,
        ScratchDatabaseReceipt receipt,
        string currentRunId,
        string command,
        bool requireReceiptRunToOwnLock,
        CancellationToken cancellationToken)
    {
        var report = CreateReport("cleanup", contract, receipt.RunId, receipt.Subtype);
        var violations = ValidateReceipt(contract, receipt, ServerScope(target));
        if (requireReceiptRunToOwnLock && receipt.RunId != currentRunId)
        {
            violations.Add(new ContractViolation("scratch.run-id.mismatch"));
        }
        if (receipt.Command != command)
        {
            violations.Add(new ContractViolation("scratch.command.mismatch"));
        }

        await using var maintenance = await OpenMaintenanceConnectionAsync(target, cancellationToken);
        if (!await AdvisoryLockProtocol.VerifyOwnershipAsync(
                maintenance,
                contract.AdvisoryLock.Key,
                currentRunId,
                command,
                AdvisoryLockMode.Exclusive,
                cancellationToken))
        {
            violations.Add(new ContractViolation("scratch.lock.not-owned"));
        }

        if (violations.Count != 0)
        {
            return Failed(report, violations);
        }

        if (!await DatabaseExistsAsync(maintenance, receipt.Database, cancellationToken))
        {
            ScratchDatabaseReceiptStore.Delete(receipt.RunId);
            return new ScratchLifecycleResult(true, report with { Validated = true }, []);
        }

        var resolved = await ResolveReceiptAsync(
            contract,
            target,
            receipt,
            currentRunId,
            command,
            requireReceiptRunToOwnLock,
            cancellationToken);
        if (!resolved.Succeeded)
        {
            return resolved with { Report = resolved.Report with { Mode = "cleanup" } };
        }

        NpgsqlConnection.ClearPool(new NpgsqlConnection(resolved.ConnectionString!));
        await ExecuteAsScratchOwnerAsync(
            maintenance,
            contract.Roles.ScratchAdministrator,
            $"DROP DATABASE {PostgreSqlIdentifier.Quote(receipt.Database)}",
            cancellationToken);
        ScratchDatabaseReceiptStore.Delete(receipt.RunId);
        return new ScratchLifecycleResult(
            true,
            report with
            {
                ReceiptRecorded = true,
                Validated = true,
                Removed = true,
                RemovedDatabases = [receipt.Database],
            },
            []);
    }

    private static async Task<ScratchLifecycleResult> ResolveReceiptAsync(
        DatabaseContract contract,
        InspectionTargetValidation target,
        ScratchDatabaseReceipt receipt,
        string currentRunId,
        string command,
        bool requireReceiptRunToOwnLock,
        CancellationToken cancellationToken)
    {
        var report = CreateReport("resolve", contract, receipt.RunId, receipt.Subtype) with
        {
            ReceiptRecorded = true,
        };
        var violations = ValidateReceipt(contract, receipt, ServerScope(target));
        if (requireReceiptRunToOwnLock && receipt.RunId != currentRunId)
        {
            violations.Add(new ContractViolation("scratch.run-id.mismatch"));
        }
        if (receipt.Command != command)
        {
            violations.Add(new ContractViolation("scratch.command.mismatch"));
        }
        if (violations.Count != 0)
        {
            return Failed(report, violations);
        }

        await using var maintenance = await OpenMaintenanceConnectionAsync(target, cancellationToken);
        if (!await AdvisoryLockProtocol.VerifyOwnershipAsync(
                maintenance,
                contract.AdvisoryLock.Key,
                currentRunId,
                command,
                AdvisoryLockMode.Exclusive,
                cancellationToken))
        {
            violations.Add(new ContractViolation("scratch.lock.not-owned"));
        }

        var owner = await ReadDatabaseOwnerAsync(maintenance, receipt.Database, cancellationToken);
        if (owner is null)
        {
            violations.Add(new ContractViolation("scratch.database.missing"));
        }
        else if (owner != contract.Roles.ScratchAdministrator)
        {
            violations.Add(new ContractViolation("scratch.owner.mismatch"));
        }
        if (violations.Count != 0)
        {
            return Failed(report, violations);
        }

        var connectionString = BuildScratchConnectionString(target, contract, receipt.Database);
        await using var scratch = new NpgsqlConnection(connectionString);
        await scratch.OpenAsync(cancellationToken);
        var expectedMarkers = ScratchMarkers(contract, receipt);
        foreach (var marker in expectedMarkers)
        {
            var actual = await ReadMarkerAsync(scratch, marker.Key, cancellationToken);
            if (actual != marker.Value)
            {
                violations.Add(new ContractViolation(marker.Key == contract.Markers.ScratchReceipt
                    ? "scratch.receipt.mismatch"
                    : marker.Key == contract.Markers.ScratchRunId
                        ? "scratch.run-id.mismatch"
                        : marker.Key == contract.Markers.RehearsalSubtype
                            ? "scratch.subtype.mismatch"
                            : "scratch.marker.mismatch"));
            }
        }

        var orderedViolations = OrderViolations(violations);
        return new ScratchLifecycleResult(
            orderedViolations.Count == 0,
            report with { Validated = orderedViolations.Count == 0 },
            orderedViolations,
            orderedViolations.Count == 0 ? connectionString : null);
    }

    private static List<ContractViolation> ValidateRequest(
        DatabaseContract contract,
        string runId,
        string command,
        string subtype)
    {
        var violations = new List<ContractViolation>();
        if (!AdvisoryLockProtocol.IsValidRunId(runId))
        {
            violations.Add(new ContractViolation("scratch.run-id.invalid"));
        }
        if (!AdvisoryLockProtocol.IsValidCommand(command))
        {
            violations.Add(new ContractViolation("scratch.command.invalid"));
        }
        if (!contract.RehearsalSubtypes.Contains(subtype, StringComparer.Ordinal)
            || !EmptyScratchSubtypes.Contains(subtype))
        {
            violations.Add(new ContractViolation("scratch.subtype.not-approved"));
        }
        return violations;
    }

    private static List<ContractViolation> ValidateReceipt(
        DatabaseContract contract,
        ScratchDatabaseReceipt receipt,
        string serverScope)
    {
        var violations = ValidateRequest(contract, receipt.RunId, receipt.Command, receipt.Subtype);
        if (receipt.Version != ReceiptVersion)
        {
            violations.Add(new ContractViolation("scratch.receipt.version"));
        }
        if (receipt.Database != ScratchDatabaseName(contract, receipt.RunId))
        {
            violations.Add(new ContractViolation("scratch.database-name.mismatch"));
        }
        if (receipt.Owner != contract.Roles.ScratchAdministrator)
        {
            violations.Add(new ContractViolation("scratch.owner.mismatch"));
        }
        if (receipt.Receipt.Length != 64 || !receipt.Receipt.All(Uri.IsHexDigit))
        {
            violations.Add(new ContractViolation("scratch.receipt.invalid"));
        }
        if (receipt.ServerScope != serverScope)
        {
            violations.Add(new ContractViolation("scratch.server.mismatch"));
        }
        return violations;
    }

    private static async Task<IReadOnlyList<ContractViolation>> ValidateServerAndOwnerAsync(
        NpgsqlConnection maintenance,
        DatabaseContract contract,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT current_setting('server_version_num')::integer / 10000,
                   pg_is_in_recovery(),
                   EXISTS (
                       SELECT 1
                       FROM pg_catalog.pg_roles
                       WHERE rolname = @owner
                         AND NOT rolcanlogin
                         AND rolcreatedb),
                   pg_has_role(session_user, @owner, 'MEMBER')
            """;
        await using var command = new NpgsqlCommand(sql, maintenance);
        command.Parameters.AddWithValue("owner", contract.Roles.ScratchAdministrator);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var violations = new List<ContractViolation>();
        if (reader.GetInt32(0) != contract.PostgresMajorVersion)
        {
            violations.Add(new ContractViolation("scratch.postgres-version.mismatch"));
        }
        if (reader.GetBoolean(1))
        {
            violations.Add(new ContractViolation("scratch.server.in-recovery"));
        }
        if (!reader.GetBoolean(2))
        {
            violations.Add(new ContractViolation("scratch.owner.invalid"));
        }
        if (!reader.GetBoolean(3))
        {
            violations.Add(new ContractViolation("scratch.owner.membership-missing"));
        }
        return violations;
    }

    private static async Task<NpgsqlConnection> OpenMaintenanceConnectionAsync(
        InspectionTargetValidation target,
        CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(target.Connection!.ConnectionString)
        {
            Database = "postgres",
            Pooling = false,
        };
        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string BuildScratchConnectionString(
        InspectionTargetValidation target,
        DatabaseContract contract,
        string database)
    {
        var builder = new NpgsqlConnectionStringBuilder(target.Connection!.ConnectionString)
        {
            Database = database,
            Pooling = false,
        };
        var roleOption = $"-c role={contract.Roles.ScratchAdministrator}";
        builder.Options = string.IsNullOrWhiteSpace(builder.Options)
            ? roleOption
            : $"{builder.Options} {roleOption}";
        return builder.ConnectionString;
    }

    private static async Task<bool> DatabaseExistsAsync(
        NpgsqlConnection connection,
        string database,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datname = @database)",
            connection);
        command.Parameters.AddWithValue("database", database);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<string?> ReadDatabaseOwnerAsync(
        NpgsqlConnection connection,
        string database,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.pg_get_userbyid(datdba) FROM pg_catalog.pg_database WHERE datname = @database",
            connection);
        command.Parameters.AddWithValue("database", database);
        return (string?)await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task ExecuteAsScratchOwnerAsync(
        NpgsqlConnection connection,
        string owner,
        string sql,
        CancellationToken cancellationToken)
    {
        await using (var setRole = new NpgsqlCommand(
                         $"SET ROLE {PostgreSqlIdentifier.Quote(owner)}",
                         connection))
        {
            await setRole.ExecuteNonQueryAsync(cancellationToken);
        }
        try
        {
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 120 };
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            await using var resetRole = new NpgsqlCommand("RESET ROLE", connection);
            await resetRole.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }

    private static async Task<string?> ReadMarkerAsync(
        NpgsqlConnection connection,
        string marker,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT substring(configured.setting FROM position('=' IN configured.setting) + 1)
            FROM pg_catalog.pg_db_role_setting AS settings
            INNER JOIN pg_catalog.pg_database AS database ON database.oid = settings.setdatabase
            CROSS JOIN LATERAL unnest(settings.setconfig) AS configured(setting)
            WHERE database.datname = current_database()
              AND settings.setrole = 0
              AND split_part(configured.setting, '=', 1) = @name
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("name", marker);
        return (string?)await command.ExecuteScalarAsync(cancellationToken);
    }

    private static IReadOnlyDictionary<string, string> ScratchMarkers(
        DatabaseContract contract,
        ScratchDatabaseReceipt receipt) => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [contract.Markers.RehearsalEnabled] = "true",
        [contract.Markers.RehearsalSubtype] = receipt.Subtype,
        [contract.Markers.ScratchRunId] = receipt.RunId,
        [contract.Markers.ScratchReceipt] = receipt.Receipt,
    };

    private static ScratchLifecycleReport CreateReport(
        string mode,
        DatabaseContract contract,
        string? runId,
        string? subtype) => new(
        mode,
        runId is null ? null : ScratchDatabaseName(contract, runId),
        contract.Roles.ScratchAdministrator,
        runId,
        subtype,
        TemplateDatabase,
        false,
        false,
        false,
        [],
        0);

    private static ScratchLifecycleResult Failed(
        ScratchLifecycleReport report,
        IEnumerable<ContractViolation> violations) => new(
        false,
        report,
        OrderViolations(violations));

    private static IReadOnlyList<ContractViolation> OrderViolations(
        IEnumerable<ContractViolation> violations) => violations
        .Distinct()
        .OrderBy(item => item.Code, StringComparer.Ordinal)
        .ThenBy(item => item.Subject, StringComparer.Ordinal)
        .ToArray();

    private static string ScratchDatabaseName(DatabaseContract contract, string runId) =>
        $"{contract.Targets.ScratchPrefix}{runId}";

    private static string ServerScope(InspectionTargetValidation target)
    {
        var connection = target.Connection!;
        var value = $"{connection.Host}\0{connection.Port}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string QuoteLiteral(string value) =>
        $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}
