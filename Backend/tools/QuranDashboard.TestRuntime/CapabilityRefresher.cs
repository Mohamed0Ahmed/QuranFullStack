using System.Data;
using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Infrastructure.Access;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.TestRuntime;

internal enum CapabilityRefreshMode
{
    Inspect,
    DryRun,
    Apply,
    Verify,
}

internal sealed record CapabilityRefreshRequest(
    CapabilityRefreshMode Mode,
    string SelectedLogin,
    string? RunId,
    string? Reason,
    bool Confirmed,
    TimeSpan? LockTimeout);

internal sealed record CanonicalPipelinePreparation(
    bool Succeeded,
    string? InputProvenance,
    IReadOnlyList<ContractViolation> Violations);

internal sealed record CapabilityRefreshValidation(
    bool Succeeded,
    string? CanonicalQuranFingerprint,
    string? SystemCatalogueFingerprint,
    string? SchemaStateFingerprint,
    string? ProtectedStateFingerprint,
    IReadOnlyList<ContractViolation> Violations);

internal interface ICanonicalRefreshPipeline
{
    Task<CanonicalPipelinePreparation> PrepareAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<CapabilityRefreshStageReport>> RunAsync(
        string connectionString,
        string runId,
        long advisoryLockKey,
        CancellationToken cancellationToken);
}

internal interface ICapabilityRefreshValidator
{
    Task<CapabilityRefreshValidation> ValidateAsync(
        DatabaseContract contract,
        ContractValidationResult contractValidation,
        string connectionString,
        string selectedLogin,
        IReadOnlyDictionary<string, string>? requiredMarkers,
        CancellationToken cancellationToken);
}

internal sealed record CapabilityRefreshDependencies(
    ICanonicalRefreshPipeline Pipeline,
    ICapabilityRefreshValidator Validator,
    Func<DateTimeOffset> UtcNow)
{
    internal static CapabilityRefreshDependencies CreateDefault(string contractPath)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(contractPath)
            ?? throw new InvalidOperationException("The database contract path has no parent directory."));
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "QuranDashboard.sln")))
        {
            directory = directory.Parent;
        }

        var backendDirectory = directory
            ?? throw new InvalidOperationException("Could not locate the Backend solution from the database contract.");
        var repositoryDirectory = backendDirectory.Parent
            ?? throw new InvalidOperationException("Backend must have a repository parent directory.");
        return new CapabilityRefreshDependencies(
            new CanonicalRefreshPipeline(backendDirectory.FullName),
            new CapabilityRefreshValidator(Path.Combine(
                repositoryDirectory.FullName,
                "test-oracles",
                "test-database-refresh.json")),
            () => DateTimeOffset.UtcNow);
    }
}

internal static class CapabilityRefresher
{
    internal const string PipelineIdentity = "quran-dashboard-canonical-curated-10-v1";

    internal static readonly string[] PlannedStages =
    [
        "create-staged-database-from-template0",
        "apply-committed-migrations",
        "import-foundation",
        "rebuild-words",
        "build-phrase-index",
        "import-morphology-enriched",
        "generate-i3rab",
        "import-mutashabihat",
        "import-navigation-metadata",
        "import-full-i3rab",
        "import-tafsirs-curated-10",
        "import-translations-curated-10",
        "reconcile-system-catalogue",
        "initialize-mutable-application-state",
        "validate-staged-capability",
        "install-capability-metadata-and-grants",
        "verify-target-idle",
        "swap-capability-name",
        "validate-installed-capability",
        "remove-replaced-database",
    ];

    internal static async Task<TestRuntimeReport> ExecuteAsync(
        DatabaseContract contract,
        ContractValidationResult contractValidation,
        InspectionTargetValidation targetValidation,
        CapabilityRefreshRequest request,
        CapabilityRefreshDependencies dependencies,
        CancellationToken cancellationToken)
    {
        var stages = new List<CapabilityRefreshStageReport>();
        var sessions = new List<CapabilityRefreshSessionReport>();
        var violations = new List<ContractViolation>();
        var targetConnection = targetValidation.Connection!;
        var capabilityConnectionString = new NpgsqlConnectionStringBuilder(targetConnection.ConnectionString)
        {
            Pooling = false,
        }.ConnectionString;
        var maintenanceConnectionString = new NpgsqlConnectionStringBuilder(targetConnection.ConnectionString)
        {
            Database = "postgres",
            Pooling = false,
        }.ConnectionString;
        var runId = request.RunId;
        var stagedDatabase = runId is null ? null : contract.Targets.RefreshPrefix + runId;
        var replacedDatabase = runId is null ? null : contract.Targets.TestDatabase + "_replaced_" + runId;
        AdvisoryLockAcquisition? applyAcquisition = null;
        if (request.Mode == CapabilityRefreshMode.Apply)
        {
            applyAcquisition = await AdvisoryLockProtocol.AcquireAsync(
                maintenanceConnectionString,
                contract.AdvisoryLock.Key,
                AdvisoryLockMode.Exclusive,
                runId!,
                "capability-refresh",
                request.LockTimeout,
                cancellationToken);
            if (applyAcquisition.Lease is null)
            {
                violations.Add(new ContractViolation("lock.acquisition.timeout"));
                return CreateReport(
                    contract,
                    contractValidation,
                    new TargetReport(
                        "postgres", targetValidation.EndpointKind, null, null, null, null, null, null, null),
                    request,
                    stages,
                    sessions,
                    violations,
                    stagedDatabase,
                    replacedDatabase,
                    advisoryLock: applyAcquisition.Report);
            }
        }

        await using var applyLease = applyAcquisition?.Lease;
        var target = await ReadMaintenanceTargetAsync(
            maintenanceConnectionString,
            targetValidation.EndpointKind!,
            cancellationToken);
        ValidateMaintenancePreflight(contract, request, target, violations);

        var targetExists = false;
        await using (var maintenance = new NpgsqlConnection(maintenanceConnectionString))
        {
            await maintenance.OpenAsync(cancellationToken);
            targetExists = await DatabaseExistsAsync(maintenance, contract.Targets.TestDatabase, cancellationToken);
            if (!await HasMaintenanceAuthorityAsync(
                    maintenance,
                    request.SelectedLogin,
                    contract.Targets.TestDatabase,
                    targetExists,
                    cancellationToken))
            {
                violations.Add(new ContractViolation("refresh.authority.insufficient"));
            }

            if (request.Mode == CapabilityRefreshMode.Apply)
            {
                var missingParameters = await CapabilityAdministrator.ReadMissingMarkerParameterPrivilegesAsync(
                    maintenance,
                    contract,
                    request.SelectedLogin,
                    cancellationToken);
                violations.AddRange(missingParameters.Select(parameter => new ContractViolation(
                    "refresh.authority.parameter-set-missing",
                    parameter)));
            }

            sessions.AddRange(await ReadSessionsAsync(maintenance, contract.Targets.TestDatabase, cancellationToken));
        }

        if (request.Mode == CapabilityRefreshMode.Verify)
        {
            if (!targetExists)
            {
                violations.Add(new ContractViolation("refresh.target.missing"));
            }
            else
            {
                var expectedMarkers = CapabilityAdministrator.ExpectedMarkers(contract, contractValidation);
                var validation = await dependencies.Validator.ValidateAsync(
                    contract,
                    contractValidation,
                    capabilityConnectionString,
                    request.SelectedLogin,
                    expectedMarkers,
                    cancellationToken);
                violations.AddRange(validation.Violations);
                return CreateReport(
                    contract,
                    contractValidation,
                    target,
                    request,
                    stages,
                    sessions,
                    violations,
                    canonicalQuranFingerprint: validation.CanonicalQuranFingerprint,
                    systemCatalogueFingerprint: validation.SystemCatalogueFingerprint,
                    schemaStateFingerprint: validation.SchemaStateFingerprint,
                    protectedStateFingerprint: validation.ProtectedStateFingerprint);
            }
        }

        if (request.Mode is CapabilityRefreshMode.Inspect or CapabilityRefreshMode.DryRun
            || violations.Count != 0)
        {
            return CreateReport(
                contract,
                contractValidation,
                target,
                request,
                stages,
                sessions,
                violations);
        }

        var preparation = await dependencies.Pipeline.PrepareAsync(cancellationToken);
        violations.AddRange(preparation.Violations);
        if (!preparation.Succeeded)
        {
            return CreateReport(
                contract,
                contractValidation,
                target,
                request,
                stages,
                sessions,
                violations,
                canonicalInputProvenance: preparation.InputProvenance);
        }

        if (runId is null || stagedDatabase is null || replacedDatabase is null)
        {
            throw new InvalidOperationException("A confirmed refresh run requires complete database identity.");
        }
        if (!IsValidDatabaseName(stagedDatabase) || !IsValidDatabaseName(replacedDatabase))
        {
            violations.Add(new ContractViolation("refresh.database-name.invalid"));
            return CreateReport(
                contract,
                contractValidation,
                target,
                request,
                stages,
                sessions,
                violations,
                stagedDatabase,
                replacedDatabase,
                preparation.InputProvenance);
        }

        var acquisition = applyAcquisition!;
        {
            await using var maintenance = new NpgsqlConnection(maintenanceConnectionString);
            await maintenance.OpenAsync(cancellationToken);
            if (await DatabaseExistsAsync(maintenance, stagedDatabase!, cancellationToken)
                || await DatabaseExistsAsync(maintenance, replacedDatabase!, cancellationToken))
            {
                violations.Add(new ContractViolation("refresh.staging-name.in-use"));
                return CreateReport(
                    contract,
                    contractValidation,
                    target,
                    request,
                    stages,
                    sessions,
                    violations,
                    stagedDatabase,
                    replacedDatabase,
                    preparation.InputProvenance,
                    advisoryLock: acquisition.Report);
            }

            sessions.Clear();
            sessions.AddRange(await ReadSessionsAsync(maintenance, contract.Targets.TestDatabase, cancellationToken));
            if (sessions.Count != 0)
            {
                violations.Add(new ContractViolation("refresh.target.sessions-active", sessions.Count.ToString(CultureInfo.InvariantCulture)));
                return CreateReport(
                    contract,
                    contractValidation,
                    target,
                    request,
                    stages,
                    sessions,
                    violations,
                    stagedDatabase,
                    replacedDatabase,
                    preparation.InputProvenance,
                    advisoryLock: acquisition.Report);
            }

            await RunStageAsync(
                stages,
                "create-staged-database-from-template0",
                async () =>
                {
                    await RequireExclusiveOwnershipAsync(maintenance, contract, runId, cancellationToken);
                    await ExecuteAsync(
                        maintenance,
                        $"CREATE DATABASE {QuoteIdentifier(stagedDatabase)} WITH TEMPLATE template0 OWNER {QuoteIdentifier(request.SelectedLogin)}",
                        cancellationToken);
                });

            var stagedConnection = new NpgsqlConnectionStringBuilder(targetConnection.ConnectionString)
            {
                Database = stagedDatabase,
                Pooling = false,
            }.ConnectionString;
            await RunStageAsync(
                stages,
                "apply-committed-migrations",
                async () =>
                {
                    await RequireExclusiveOwnershipAsync(maintenance, contract, runId, cancellationToken);
                    await ApplyMigrationsAsync(stagedConnection, cancellationToken);
                });
            stages.AddRange(await dependencies.Pipeline.RunAsync(
                stagedConnection,
                runId,
                contract.AdvisoryLock.Key,
                cancellationToken));
            if (stages.Any(stage => stage.Status != "passed"))
            {
                violations.Add(new ContractViolation("refresh.canonical-pipeline.failed"));
                return CreateReport(
                    contract,
                    contractValidation,
                    target,
                    request,
                    stages,
                    sessions,
                    violations,
                    stagedDatabase,
                    replacedDatabase,
                    preparation.InputProvenance,
                    advisoryLock: acquisition.Report);
            }

            await RunStageAsync(
                stages,
                "reconcile-system-catalogue",
                async () =>
                {
                    await RequireExclusiveOwnershipAsync(maintenance, contract, runId, cancellationToken);
                    await ReconcileCatalogueAsync(stagedConnection, cancellationToken);
                });
            await RunStageAsync(
                stages,
                "initialize-mutable-application-state",
                async () =>
                {
                    await RequireExclusiveOwnershipAsync(maintenance, contract, runId, cancellationToken);
                    await InitializeMutableStateAsync(stagedConnection, contract, cancellationToken);
                });

            CapabilityRefreshValidation stagedValidation = null!;
            var stagedValidationTimer = Stopwatch.StartNew();
            stagedValidation = await dependencies.Validator.ValidateAsync(
                contract,
                contractValidation,
                stagedConnection,
                request.SelectedLogin,
                requiredMarkers: null,
                cancellationToken);
            stages.Add(new CapabilityRefreshStageReport(
                "validate-staged-capability",
                stagedValidation.Succeeded ? "passed" : "failed",
                stagedValidationTimer.ElapsedMilliseconds,
                stagedValidation.Succeeded ? null : nameof(CapabilityRefreshValidationException)));
            if (!stagedValidation.Succeeded)
            {
                violations.AddRange(stagedValidation.Violations);
                return CreateReport(
                    contract,
                    contractValidation,
                    target,
                    request,
                    stages,
                    sessions,
                    violations,
                    stagedDatabase,
                    replacedDatabase,
                    preparation.InputProvenance,
                    stagedValidation.CanonicalQuranFingerprint,
                    stagedValidation.SystemCatalogueFingerprint,
                    advisoryLock: acquisition.Report);
            }

            var refreshedAt = dependencies.UtcNow().ToUniversalTime();
            var markers = new Dictionary<string, string>(
                CapabilityAdministrator.ExpectedMarkers(contract, contractValidation),
                StringComparer.Ordinal)
            {
                ["canonicalPipeline"] = PipelineIdentity,
                ["canonicalInputProvenance"] = preparation.InputProvenance!,
                ["canonicalQuranFingerprint"] = stagedValidation.CanonicalQuranFingerprint!,
                ["systemCatalogueFingerprint"] = stagedValidation.SystemCatalogueFingerprint!,
                ["protectedStateFingerprint"] = stagedValidation.ProtectedStateFingerprint!,
                ["refreshedAtUtc"] = refreshedAt.ToString("O", CultureInfo.InvariantCulture),
            };
            try
            {
                await RunStageAsync(
                    stages,
                    "install-capability-metadata-and-grants",
                    async () =>
                    {
                        await RequireExclusiveOwnershipAsync(maintenance, contract, runId, cancellationToken);
                        await using var connection = new NpgsqlConnection(stagedConnection);
                        await connection.OpenAsync(cancellationToken);
                        await CapabilityAdministrator.ApplyDatabaseStateAsync(
                            connection,
                            contract,
                            request.SelectedLogin,
                            markers,
                            stagedDatabase,
                            cancellationToken);
                        var validation = await dependencies.Validator.ValidateAsync(
                            contract,
                            contractValidation,
                            stagedConnection,
                            request.SelectedLogin,
                            markers,
                            cancellationToken);
                        if (!validation.Succeeded)
                        {
                            throw new CapabilityRefreshValidationException(validation.Violations);
                        }
                    });
            }
            catch (CapabilityRefreshValidationException exception)
            {
                violations.AddRange(exception.Violations);
                return CreateReport(
                    contract,
                    contractValidation,
                    target,
                    request,
                    stages,
                    sessions,
                    violations,
                    stagedDatabase,
                    replacedDatabase,
                    preparation.InputProvenance,
                    stagedValidation.CanonicalQuranFingerprint,
                    stagedValidation.SystemCatalogueFingerprint,
                    refreshedAtUtc: refreshedAt,
                    advisoryLock: acquisition.Report);
            }

            sessions.Clear();
            var idleTimer = Stopwatch.StartNew();
            sessions.AddRange(await ReadSessionsAsync(
                maintenance,
                contract.Targets.TestDatabase,
                cancellationToken));
            stages.Add(new CapabilityRefreshStageReport(
                "verify-target-idle",
                sessions.Count == 0 ? "passed" : "failed",
                idleTimer.ElapsedMilliseconds,
                sessions.Count == 0 ? null : nameof(CapabilityRefreshValidationException)));
            if (sessions.Count != 0)
            {
                violations.Add(new ContractViolation("refresh.target.sessions-active", sessions.Count.ToString(CultureInfo.InvariantCulture)));
                return CreateReport(
                    contract,
                    contractValidation,
                    target,
                    request,
                    stages,
                    sessions,
                    violations,
                    stagedDatabase,
                    replacedDatabase,
                    preparation.InputProvenance,
                    stagedValidation.CanonicalQuranFingerprint,
                    stagedValidation.SystemCatalogueFingerprint,
                    refreshedAtUtc: refreshedAt,
                    advisoryLock: acquisition.Report);
            }

            var oldCapabilityExists = await DatabaseExistsAsync(
                maintenance,
                contract.Targets.TestDatabase,
                cancellationToken);
            var swapRolledBack = false;
            try
            {
                await RunStageAsync(
                    stages,
                    "swap-capability-name",
                    async () =>
                    {
                        await RequireExclusiveOwnershipAsync(maintenance, contract, runId, cancellationToken);
                        if (oldCapabilityExists)
                        {
                            await ExecuteAsync(
                                maintenance,
                                $"ALTER DATABASE {QuoteIdentifier(contract.Targets.TestDatabase)} RENAME TO {QuoteIdentifier(replacedDatabase)}",
                                cancellationToken);
                        }

                        try
                        {
                            await ExecuteAsync(
                                maintenance,
                                $"ALTER DATABASE {QuoteIdentifier(stagedDatabase)} RENAME TO {QuoteIdentifier(contract.Targets.TestDatabase)}",
                                cancellationToken);
                        }
                        catch
                        {
                            if (oldCapabilityExists)
                            {
                                await ExecuteAsync(
                                    maintenance,
                                    $"ALTER DATABASE {QuoteIdentifier(replacedDatabase)} RENAME TO {QuoteIdentifier(contract.Targets.TestDatabase)}",
                                    cancellationToken);
                            }

                            throw;
                        }
                    });

                CapabilityRefreshValidation installedValidation = null!;
                await RunStageAsync(
                    stages,
                    "validate-installed-capability",
                    async () =>
                    {
                        installedValidation = await dependencies.Validator.ValidateAsync(
                            contract,
                            contractValidation,
                            capabilityConnectionString,
                            request.SelectedLogin,
                            markers,
                            cancellationToken);
                        var fingerprintsMatch = installedValidation.CanonicalQuranFingerprint
                                                == stagedValidation.CanonicalQuranFingerprint
                                                && installedValidation.SystemCatalogueFingerprint
                                                == stagedValidation.SystemCatalogueFingerprint
                                                && installedValidation.SchemaStateFingerprint
                                                == stagedValidation.SchemaStateFingerprint
                                                && installedValidation.ProtectedStateFingerprint
                                                == stagedValidation.ProtectedStateFingerprint;
                        if (!installedValidation.Succeeded || !fingerprintsMatch)
                        {
                            var validationViolations = installedValidation.Violations.ToList();
                            if (!fingerprintsMatch)
                            {
                                validationViolations.Add(new ContractViolation("refresh.post-swap.fingerprint-mismatch"));
                            }

                            throw new CapabilityRefreshValidationException(validationViolations);
                        }
                    });
            }
            catch (Exception exception) when (exception is NpgsqlException
                                              or IOException
                                              or InvalidOperationException
                                              or OperationCanceledException)
            {
                if (exception is CapabilityRefreshValidationException validationException)
                {
                    violations.AddRange(validationException.Violations);
                }
                else
                {
                    violations.Add(new ContractViolation(
                        "refresh.swap-or-post-validation.failed",
                        exception is PostgresException postgresException
                            ? postgresException.SqlState
                            : exception.GetType().Name));
                }

                swapRolledBack = await RollBackSwapAsync(
                    maintenance,
                    contract.Targets.TestDatabase,
                    stagedDatabase,
                    replacedDatabase,
                    oldCapabilityExists,
                    CancellationToken.None);
                if (!swapRolledBack)
                {
                    violations.Add(new ContractViolation("refresh.swap.rollback-failed"));
                }

                return CreateReport(
                    contract,
                    contractValidation,
                    target,
                    request,
                    stages,
                    sessions,
                    violations,
                    stagedDatabase,
                    replacedDatabase,
                    preparation.InputProvenance,
                    stagedValidation.CanonicalQuranFingerprint,
                    stagedValidation.SystemCatalogueFingerprint,
                    swapRolledBack,
                    refreshedAtUtc: refreshedAt,
                    advisoryLock: acquisition.Report);
            }

            var removed = false;
            if (oldCapabilityExists)
            {
                await RunStageAsync(
                    stages,
                    "remove-replaced-database",
                    async () =>
                    {
                        await RequireExclusiveOwnershipAsync(maintenance, contract, runId, cancellationToken);
                        await ExecuteAsync(
                            maintenance,
                            $"DROP DATABASE {QuoteIdentifier(replacedDatabase)}",
                            cancellationToken);
                    });
                removed = true;
            }

            return CreateReport(
                contract,
                contractValidation,
                target,
                request,
                stages,
                sessions,
                violations,
                stagedDatabase,
                oldCapabilityExists ? replacedDatabase : null,
                preparation.InputProvenance,
                stagedValidation.CanonicalQuranFingerprint,
                stagedValidation.SystemCatalogueFingerprint,
                swapRolledBack,
                removed,
                refreshedAt,
                acquisition.Report,
                applied: true,
                schemaStateFingerprint: stagedValidation.SchemaStateFingerprint,
                protectedStateFingerprint: stagedValidation.ProtectedStateFingerprint);
        }
    }

    private static TestRuntimeReport CreateReport(
        DatabaseContract contract,
        ContractValidationResult contractValidation,
        TargetReport target,
        CapabilityRefreshRequest request,
        IReadOnlyList<CapabilityRefreshStageReport> stages,
        IReadOnlyList<CapabilityRefreshSessionReport> sessions,
        IEnumerable<ContractViolation> violations,
        string? stagedDatabase = null,
        string? replacedDatabase = null,
        string? canonicalInputProvenance = null,
        string? canonicalQuranFingerprint = null,
        string? systemCatalogueFingerprint = null,
        bool swapRolledBack = false,
        bool replacedDatabaseRemoved = false,
        DateTimeOffset? refreshedAtUtc = null,
        AdvisoryLockReport? advisoryLock = null,
        bool applied = false,
        string? schemaStateFingerprint = null,
        string? protectedStateFingerprint = null)
    {
        var ordered = violations.Distinct()
            .OrderBy(violation => violation.Code, StringComparer.Ordinal)
            .ThenBy(violation => violation.Subject, StringComparer.Ordinal)
            .ToArray();
        var mode = ModeName(request.Mode);
        return new TestRuntimeReport(
            $"refresh-{mode}",
            ordered.Length == 0,
            DatabaseInspector.ToContractReport(contract, contractValidation),
            target,
            null,
            null,
            null,
            null,
            ordered,
            AdvisoryLock: advisoryLock,
            Refresh: new CapabilityRefreshReport(
                mode,
                applied,
                request.Confirmed,
                contract.Targets.TestDatabase,
                stagedDatabase,
                replacedDatabase,
                PipelineIdentity,
                PlannedStages,
                stages,
                sessions,
                canonicalInputProvenance,
                canonicalQuranFingerprint,
                systemCatalogueFingerprint,
                schemaStateFingerprint,
                protectedStateFingerprint,
                swapRolledBack,
                replacedDatabaseRemoved,
                refreshedAtUtc?.ToString("O", CultureInfo.InvariantCulture),
                request.Reason));
    }

    private static void ValidateMaintenancePreflight(
        DatabaseContract contract,
        CapabilityRefreshRequest request,
        TargetReport target,
        ICollection<ContractViolation> violations)
    {
        if (target.Database != "postgres")
        {
            violations.Add(new ContractViolation("refresh.maintenance-target.changed"));
        }

        if (target.PostgreSqlMajorVersion != contract.PostgresMajorVersion)
        {
            violations.Add(new ContractViolation("refresh.postgres-version.unsupported"));
        }

        if (target.InRecovery is not false)
        {
            violations.Add(new ContractViolation("refresh.target.in-recovery"));
        }

        if (target.SessionUser != request.SelectedLogin || target.CurrentUser != request.SelectedLogin)
        {
            violations.Add(new ContractViolation("refresh.login.not-session-user"));
        }
    }

    private static async Task<TargetReport> ReadMaintenanceTargetAsync(
        string connectionString,
        string endpointKind,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT current_database(), inet_server_addr()::text, inet_server_port(), session_user,
                   current_user, current_setting('server_version'),
                   current_setting('server_version_num')::integer / 10000, pg_is_in_recovery()
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new TargetReport(
            reader.GetString(0), endpointKind,
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetInt32(2),
            reader.GetString(3), reader.GetString(4), reader.GetString(5),
            reader.GetInt32(6), reader.GetBoolean(7));
    }

    private static async Task ApplyMigrationsAsync(string connectionString, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new QuranDashboardDbContext(options);
        await context.Database.MigrateAsync(cancellationToken);
    }

    private static async Task ReconcileCatalogueAsync(string connectionString, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new QuranDashboardDbContext(options);
        var result = await new PermissionCatalogueSynchronizer(context).SynchronizeAsync(cancellationToken);
        if (result.UnknownDatabaseCodes.Count != 0 || result.RetiredCanonicalCodes.Count != 0)
        {
            throw new CapabilityRefreshValidationException(
                [new ContractViolation("refresh.system-catalogue.reconciliation-failed")]);
        }
    }

    private static async Task InitializeMutableStateAsync(
        string connectionString,
        DatabaseContract contract,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var resetTables = contract.DataClasses.MutableApplicationState
            .Where(table => table != contract.LinkingDataBaseline.Table)
            .Select(QuoteIdentifier);
        await ExecuteAsync(
            connection,
            transaction,
            $"TRUNCATE TABLE {string.Join(", ", resetTables)} CONTINUE IDENTITY RESTRICT",
            cancellationToken);
        var baseline = contract.LinkingDataBaseline;
        await ExecuteAsync(
            connection,
            transaction,
            $"DELETE FROM {QuoteIdentifier(baseline.Table)}; "
            + $"INSERT INTO {QuoteIdentifier(baseline.Table)} (id, generation, updated_at_utc) "
            + "VALUES (@id, @generation, @updatedAtUtc)",
            cancellationToken,
            new NpgsqlParameter("id", baseline.Id),
            new NpgsqlParameter("generation", baseline.Generation),
            new NpgsqlParameter("updatedAtUtc", baseline.UpdatedAtUtc));
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<CapabilityRefreshSessionReport>> ReadSessionsAsync(
        NpgsqlConnection connection,
        string database,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT pid, COALESCE(application_name, ''), COALESCE(state, 'unknown')
            FROM pg_catalog.pg_stat_activity
            WHERE datname = @database
              AND pid <> pg_backend_pid()
            ORDER BY pid
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("database", database);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<CapabilityRefreshSessionReport>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new CapabilityRefreshSessionReport(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2)));
        }

        return result;
    }

    private static async Task<bool> RollBackSwapAsync(
        NpgsqlConnection maintenance,
        string targetDatabase,
        string stagedDatabase,
        string replacedDatabase,
        bool oldCapabilityExists,
        CancellationToken cancellationToken)
    {
        try
        {
            var targetExists = await DatabaseExistsAsync(maintenance, targetDatabase, cancellationToken);
            var stagedExists = await DatabaseExistsAsync(maintenance, stagedDatabase, cancellationToken);
            var replacedExists = await DatabaseExistsAsync(maintenance, replacedDatabase, cancellationToken);
            if (targetExists && stagedExists && !replacedExists)
            {
                return true;
            }

            if (targetExists && !stagedExists)
            {
                await ExecuteAsync(
                    maintenance,
                    $"ALTER DATABASE {QuoteIdentifier(targetDatabase)} RENAME TO {QuoteIdentifier(stagedDatabase)}",
                    cancellationToken);
            }

            if (oldCapabilityExists && replacedExists)
            {
                await ExecuteAsync(
                    maintenance,
                    $"ALTER DATABASE {QuoteIdentifier(replacedDatabase)} RENAME TO {QuoteIdentifier(targetDatabase)}",
                    cancellationToken);
            }

            return oldCapabilityExists
                ? await DatabaseExistsAsync(maintenance, targetDatabase, cancellationToken)
                  && !await DatabaseExistsAsync(maintenance, replacedDatabase, cancellationToken)
                : !await DatabaseExistsAsync(maintenance, targetDatabase, cancellationToken)
                  && await DatabaseExistsAsync(maintenance, stagedDatabase, cancellationToken);
        }
        catch (PostgresException)
        {
            return false;
        }
    }

    private static async Task RunStageAsync(
        ICollection<CapabilityRefreshStageReport> stages,
        string stage,
        Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await action();
            stages.Add(new CapabilityRefreshStageReport(stage, "passed", stopwatch.ElapsedMilliseconds));
        }
        catch (CapabilityRefreshValidationException)
        {
            stages.Add(new CapabilityRefreshStageReport(
                stage, "failed", stopwatch.ElapsedMilliseconds, nameof(CapabilityRefreshValidationException)));
            throw;
        }
        catch (Exception exception)
        {
            stages.Add(new CapabilityRefreshStageReport(
                stage, "failed", stopwatch.ElapsedMilliseconds, exception.GetType().Name));
            throw;
        }
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

    private static async Task RequireExclusiveOwnershipAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        string runId,
        CancellationToken cancellationToken)
    {
        if (!await AdvisoryLockProtocol.VerifyOwnershipAsync(
                connection,
                contract.AdvisoryLock.Key,
                runId,
                "capability-refresh",
                AdvisoryLockMode.Exclusive,
                cancellationToken))
        {
            throw new CapabilityRefreshValidationException(
                [new ContractViolation("lock.exclusive-ownership.required")]);
        }
    }

    private static async Task<bool> HasMaintenanceAuthorityAsync(
        NpgsqlConnection connection,
        string selectedLogin,
        string targetDatabase,
        bool targetExists,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT role.rolsuper
                   OR (role.rolcreatedb
                       AND role.rolcreaterole
                       AND (NOT @targetExists OR pg_catalog.pg_get_userbyid(database.datdba) = @login))
            FROM pg_catalog.pg_roles AS role
            LEFT JOIN pg_catalog.pg_database AS database ON database.datname = @targetDatabase
            WHERE role.rolname = @login
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("targetExists", targetExists);
        command.Parameters.AddWithValue("targetDatabase", targetDatabase);
        command.Parameters.AddWithValue("login", selectedLogin);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool IsValidDatabaseName(string value) =>
        value.Length <= 63 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    private static string ModeName(CapabilityRefreshMode mode) => mode switch
    {
        CapabilityRefreshMode.Inspect => "inspect",
        CapabilityRefreshMode.DryRun => "dry-run",
        CapabilityRefreshMode.Apply => "apply",
        CapabilityRefreshMode.Verify => "verify",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    internal static string QuoteIdentifier(string value) => new NpgsqlCommandBuilder().QuoteIdentifier(value);
}

internal sealed class CapabilityRefreshValidationException(IReadOnlyList<ContractViolation> violations)
    : InvalidOperationException("The staged Test Database Capability did not pass validation.")
{
    internal IReadOnlyList<ContractViolation> Violations { get; } = violations;
}
