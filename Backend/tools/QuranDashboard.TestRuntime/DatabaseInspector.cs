using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.TestRuntime;

internal static class DatabaseInspector
{
    internal static async Task<TestRuntimeReport> InspectAsync(
        DatabaseContract contract,
        ContractValidationResult contractValidation,
        InspectionTargetValidation targetValidation,
        CancellationToken cancellationToken)
    {
        var violations = new List<ContractViolation>();
        var connection = new NpgsqlConnection(targetValidation.Connection!.ConnectionString);
        await using (connection)
        {
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);
            await ExecuteAsync(connection, transaction, "SET TRANSACTION READ ONLY", cancellationToken);

            var target = await ReadTargetAsync(
                connection,
                transaction,
                targetValidation.EndpointKind!,
                cancellationToken);
            ValidateConnectedTarget(contract, target, violations);
            if (violations.Any(violation => violation.Code == "inspection.target.changed"))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new TestRuntimeReport(
                    "inspect",
                    false,
                    ToContractReport(contract, contractValidation),
                    target,
                    null,
                    null,
                    null,
                    null,
                    OrderViolations(violations));
            }

            var liveTables = await ReadLiveTablesAsync(connection, transaction, cancellationToken);
            ValidateLiveTables(contract, liveTables, violations);

            var migration = await ReadMigrationAsync(
                connection,
                transaction,
                contractValidation.ExpectedMigrations,
                liveTables.Contains("__EFMigrationsHistory"),
                cancellationToken);
            if (migration.State != "current")
            {
                violations.Add(new ContractViolation("inspection.migration.not-current", migration.State));
            }

            var catalogue = await ReadCatalogueAsync(
                connection,
                transaction,
                contract,
                liveTables,
                cancellationToken);
            violations.AddRange(catalogue.Violations);

            var markers = await ReadMarkersAsync(connection, transaction, contract, migration, cancellationToken);
            if (!markers.Healthy)
            {
                violations.Add(new ContractViolation("inspection.markers.invalid"));
            }

            var privileges = await ReadPrivilegesAsync(
                connection,
                transaction,
                contract,
                liveTables,
                cancellationToken);

            await transaction.RollbackAsync(cancellationToken);
            var orderedViolations = OrderViolations(violations);
            return new TestRuntimeReport(
                "inspect",
                orderedViolations.Count == 0,
                ToContractReport(contract, contractValidation),
                target,
                migration,
                catalogue,
                markers,
                privileges,
                orderedViolations);
        }
    }

    private static async Task<TargetReport> ReadTargetAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string endpointKind,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT current_database(),
                   inet_server_addr()::text,
                   inet_server_port(),
                   session_user,
                   current_user,
                   current_setting('server_version'),
                   current_setting('server_version_num')::integer / 10000,
                   pg_is_in_recovery()
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new TargetReport(
            reader.GetString(0),
            endpointKind,
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetInt32(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetInt32(6),
            reader.GetBoolean(7));
    }

    private static void ValidateConnectedTarget(
        DatabaseContract contract,
        TargetReport target,
        ICollection<ContractViolation> violations)
    {
        if (target.Database != contract.Targets.TestDatabase)
        {
            violations.Add(new ContractViolation("inspection.target.changed"));
        }

        if (target.PostgreSqlMajorVersion != contract.PostgresMajorVersion)
        {
            violations.Add(new ContractViolation("inspection.postgres-version.unsupported"));
        }

        if (target.InRecovery is not false)
        {
            violations.Add(new ContractViolation("inspection.target.in-recovery"));
        }
    }

    private static async Task<HashSet<string>> ReadLiveTablesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT tablename
            FROM pg_catalog.pg_tables
            WHERE schemaname = 'public'
            ORDER BY tablename
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tables = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static void ValidateLiveTables(
        DatabaseContract contract,
        IReadOnlySet<string> liveTables,
        ICollection<ContractViolation> violations)
    {
        var classified = contract.AllTables().Select(entry => entry.Table).ToHashSet(StringComparer.Ordinal);
        foreach (var table in classified.Except(liveTables, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            violations.Add(new ContractViolation("inspection.catalogue.missing-table", table));
        }

        foreach (var table in liveTables.Except(classified, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            violations.Add(new ContractViolation("inspection.catalogue.unclassified-table", table));
        }
    }

    private static async Task<MigrationReport> ReadMigrationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<string> expectedMigrations,
        bool historyTablePresent,
        CancellationToken cancellationToken)
    {
        var expectedHead = expectedMigrations.LastOrDefault();
        if (!historyTablePresent)
        {
            return new MigrationReport(false, expectedHead, null, expectedMigrations.Count, 0, "history-missing");
        }

        const string sql = """
            SELECT "MigrationId"
            FROM public."__EFMigrationsHistory"
            ORDER BY "MigrationId"
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var appliedMigrations = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            appliedMigrations.Add(reader.GetString(0));
        }

        var head = appliedMigrations.LastOrDefault();
        return new MigrationReport(
            true,
            expectedHead,
            head,
            expectedMigrations.Count,
            appliedMigrations.Count,
            ClassifyMigrationState(expectedMigrations, appliedMigrations));
    }

    private static async Task<CatalogueReport> ReadCatalogueAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        IReadOnlySet<string> liveTables,
        CancellationToken cancellationToken)
    {
        if (!liveTables.Contains("roles") || !liveTables.Contains("permissions"))
        {
            return new CatalogueReport(
                false,
                false,
                0,
                0,
                [new ContractViolation("inspection.system-catalogue.unavailable")]);
        }

        var violations = new List<ContractViolation>();
        var roles = await ReadRolesAsync(connection, transaction, cancellationToken);
        var owner = contract.SystemCatalogue.OwnerRole;
        if (roles.Count(role => role.Id == owner.Id
                               && role.Name == owner.Name
                               && role.DisplayName == owner.DisplayName) != 1
            || roles.Any(role => (role.Id == owner.Id || role.Name == owner.Name)
                                 && (role.Id != owner.Id
                                     || role.Name != owner.Name
                                     || role.DisplayName != owner.DisplayName)))
        {
            violations.Add(new ContractViolation("inspection.system-catalogue.owner-invalid"));
        }

        var permissions = await ReadPermissionsAsync(connection, transaction, cancellationToken);
        var canonical = AbwabPermissionCatalogue.All.ToDictionary(permission => permission.Code, StringComparer.Ordinal);
        foreach (var definition in canonical.Values)
        {
            var matches = permissions.Where(permission => permission.Code == definition.Code).ToArray();
            if (matches.Length == 0)
            {
                violations.Add(new ContractViolation("inspection.system-catalogue.permission-missing", definition.Code));
            }
            else if (matches.Length > 1)
            {
                violations.Add(new ContractViolation("inspection.system-catalogue.permission-duplicate", definition.Code));
            }
            else if (matches[0].RetiredAtUtc is not null)
            {
                violations.Add(new ContractViolation("inspection.system-catalogue.permission-retired", definition.Code));
            }
            else if (matches[0].ArabicLabel != definition.ArabicLabel
                     || matches[0].EnglishDescription != definition.EnglishDescription
                     || matches[0].DisplayOrder != definition.DisplayOrder)
            {
                violations.Add(new ContractViolation("inspection.system-catalogue.permission-metadata-mismatch", definition.Code));
            }
        }

        foreach (var permission in permissions.Where(permission => !canonical.ContainsKey(permission.Code)
                                                                   && permission.RetiredAtUtc is null))
        {
            violations.Add(new ContractViolation("inspection.system-catalogue.permission-active-unknown", permission.Code));
        }

        var orderedViolations = OrderViolations(violations);
        return new CatalogueReport(
            true,
            orderedViolations.Count == 0,
            roles.Count,
            permissions.Count,
            orderedViolations);
    }

    private static async Task<IReadOnlyList<RoleRow>> ReadRolesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, name, display_name FROM public.roles ORDER BY id";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var roles = new List<RoleRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            roles.Add(new RoleRow(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        }

        return roles;
    }

    private static async Task<IReadOnlyList<PermissionRow>> ReadPermissionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT code, arabic_label, english_description, display_order, retired_at
            FROM public.permissions
            ORDER BY code
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var permissions = new List<PermissionRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            permissions.Add(new PermissionRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4)));
        }

        return permissions;
    }

    private static async Task<MarkerReport> ReadMarkersAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        MigrationReport migration,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var marker in contract.Markers.AsDictionary())
        {
            const string sql = """
                SELECT substring(configured.setting FROM position('=' IN configured.setting) + 1)
                FROM pg_catalog.pg_db_role_setting AS settings
                INNER JOIN pg_catalog.pg_database AS database
                    ON database.oid = settings.setdatabase
                CROSS JOIN LATERAL unnest(settings.setconfig) AS configured(setting)
                WHERE database.datname = current_database()
                  AND settings.setrole = 0
                  AND split_part(configured.setting, '=', 1) = @name
                """;
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("name", marker.Value);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            values[marker.Key] = value is null or DBNull ? null : (string)value;
        }

        var expectations = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["capabilityEnabled"] = "true",
            ["resetEnabled"] = "true",
            ["contractVersion"] = contract.ContractVersion.ToString(),
            ["capabilityMetadataVersion"] = contract.CapabilityMetadataVersion.ToString(),
            ["migrationHead"] = migration.ExpectedHead,
        };
        var healthy = expectations.All(expectation => values[expectation.Key] == expectation.Value)
                      && HasValue(values, "canonicalPipeline")
                      && HasValue(values, "canonicalInputProvenance")
                      && HasValue(values, "canonicalQuranFingerprint")
                      && HasValue(values, "systemCatalogueFingerprint")
                      && DateTimeOffset.TryParse(values["refreshedAtUtc"], out _);
        var states = values.ToDictionary(
            marker => marker.Key,
            marker => new MarkerState(
                !string.IsNullOrWhiteSpace(marker.Value),
                expectations.TryGetValue(marker.Key, out var expected)
                    ? marker.Value == expected
                    : null),
            StringComparer.Ordinal);
        return new MarkerReport(healthy, states);
    }

    private static async Task<PrivilegeReport> ReadPrivilegesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        IReadOnlySet<string> liveTables,
        CancellationToken cancellationToken)
    {
        const string databaseSql = """
            SELECT has_database_privilege(current_user, current_database(), 'CONNECT'),
                   has_database_privilege(current_user, current_database(), 'CREATE'),
                   has_database_privilege(current_user, current_database(), 'TEMP'),
                   has_schema_privilege(current_user, 'public', 'USAGE'),
                   has_schema_privilege(current_user, 'public', 'CREATE')
            """;
        await using var databaseCommand = new NpgsqlCommand(databaseSql, connection, transaction);
        await using var databaseReader = await databaseCommand.ExecuteReaderAsync(cancellationToken);
        await databaseReader.ReadAsync(cancellationToken);
        var canConnect = databaseReader.GetBoolean(0);
        var canCreateDatabaseObjects = databaseReader.GetBoolean(1);
        var canCreateTemporaryTables = databaseReader.GetBoolean(2);
        var canUsePublicSchema = databaseReader.GetBoolean(3);
        var canCreateInPublicSchema = databaseReader.GetBoolean(4);
        await databaseReader.DisposeAsync();

        var roleReports = new Dictionary<string, ExpectedRoleReport>(StringComparer.Ordinal);
        foreach (var role in contract.Roles.AsDictionary())
        {
            const string roleSql = """
                SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = @role),
                       CASE
                           WHEN EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = @role)
                           THEN pg_has_role(session_user, @role, 'MEMBER')
                           ELSE false
                       END
                """;
            await using var roleCommand = new NpgsqlCommand(roleSql, connection, transaction);
            roleCommand.Parameters.AddWithValue("role", role.Value);
            await using var roleReader = await roleCommand.ExecuteReaderAsync(cancellationToken);
            await roleReader.ReadAsync(cancellationToken);
            roleReports[role.Key] = new ExpectedRoleReport(roleReader.GetBoolean(0), roleReader.GetBoolean(1));
        }

        var classReports = new Dictionary<string, DataClassPrivilegeReport>(StringComparer.Ordinal);
        classReports["canonicalQuranData"] = await ReadTablePrivilegesAsync(
            connection, transaction, contract.DataClasses.CanonicalQuranData, liveTables, cancellationToken);
        classReports["systemCatalogue"] = await ReadTablePrivilegesAsync(
            connection, transaction, contract.DataClasses.SystemCatalogue, liveTables, cancellationToken);
        classReports["mutableApplicationState"] = await ReadTablePrivilegesAsync(
            connection, transaction, contract.DataClasses.MutableApplicationState, liveTables, cancellationToken);
        classReports["schemaState"] = await ReadTablePrivilegesAsync(
            connection, transaction, contract.DataClasses.SchemaState, liveTables, cancellationToken);

        return new PrivilegeReport(
            canConnect,
            canCreateDatabaseObjects,
            canCreateTemporaryTables,
            canUsePublicSchema,
            canCreateInPublicSchema,
            roleReports,
            classReports);
    }

    private static async Task<DataClassPrivilegeReport> ReadTablePrivilegesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyCollection<string> classifiedTables,
        IReadOnlySet<string> liveTables,
        CancellationToken cancellationToken)
    {
        var tables = classifiedTables.Where(liveTables.Contains).ToArray();
        if (tables.Length == 0)
        {
            return new DataClassPrivilegeReport(0, false, false, false, false, false);
        }

        const string sql = """
            SELECT bool_and(has_table_privilege(current_user, format('%I.%I', 'public', table_name), 'SELECT')),
                   bool_or(has_table_privilege(current_user, format('%I.%I', 'public', table_name), 'INSERT')),
                   bool_or(has_table_privilege(current_user, format('%I.%I', 'public', table_name), 'UPDATE')),
                   bool_or(has_table_privilege(current_user, format('%I.%I', 'public', table_name), 'DELETE')),
                   bool_or(has_table_privilege(current_user, format('%I.%I', 'public', table_name), 'TRUNCATE'))
            FROM unnest(@tables) AS tables(table_name)
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("tables", NpgsqlDbType.Array | NpgsqlDbType.Text, tables);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new DataClassPrivilegeReport(
            tables.Length,
            reader.GetBoolean(0),
            reader.GetBoolean(1),
            reader.GetBoolean(2),
            reader.GetBoolean(3),
            reader.GetBoolean(4));
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool HasValue(IReadOnlyDictionary<string, string?> values, string key) =>
        !string.IsNullOrWhiteSpace(values[key]);

    private static IReadOnlyList<ContractViolation> OrderViolations(IEnumerable<ContractViolation> violations) =>
        violations.Distinct()
            .OrderBy(violation => violation.Code, StringComparer.Ordinal)
            .ThenBy(violation => violation.Subject, StringComparer.Ordinal)
            .ToArray();

    internal static ContractReport ToContractReport(
        DatabaseContract contract,
        ContractValidationResult validation) => new(
        contract.ContractVersion,
        contract.CapabilityMetadataVersion,
        validation.MappedTableCount,
        validation.SchemaTableCount,
        validation.ExpectedMigrations.LastOrDefault());

    internal static string ClassifyMigrationState(
        IReadOnlyList<string> expectedMigrations,
        IReadOnlyList<string> appliedMigrations)
    {
        if (appliedMigrations.SequenceEqual(expectedMigrations, StringComparer.Ordinal))
        {
            return "current";
        }

        if (appliedMigrations.Count == 0)
        {
            return "empty";
        }

        var expected = expectedMigrations.ToHashSet(StringComparer.Ordinal);
        return appliedMigrations.Any(migration => !expected.Contains(migration))
            ? "unknown"
            : "pending";
    }

    private sealed record RoleRow(int Id, string Name, string DisplayName);

    private sealed record PermissionRow(
        string Code,
        string ArabicLabel,
        string EnglishDescription,
        int DisplayOrder,
        DateTimeOffset? RetiredAtUtc);
}
