using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace QuranDashboard.TestRuntime;

internal enum CapabilityAdministrationMode
{
    Inspect,
    DryRun,
    Apply,
    Verify,
}

internal static class CapabilityAdministrator
{
    private const string InsufficientPrivilegeSqlState = "42501";

    private static readonly string[] PlannedOperations =
    [
        "reconcile-capability-role-attributes",
        "restrict-capability-role-membership-to-selected-login",
        "reconcile-test-database-table-and-sequence-grants",
        "install-database-scoped-safety-metadata",
        "verify-protected-and-out-of-scope-denials",
    ];

    private static readonly string[] ManagedMarkerKeys =
    [
        "capabilityEnabled",
        "resetEnabled",
        "contractVersion",
        "capabilityMetadataVersion",
        "migrationHead",
    ];

    internal static async Task<TestRuntimeReport> ExecuteAsync(
        DatabaseContract contract,
        ContractValidationResult contractValidation,
        InspectionTargetValidation targetValidation,
        CapabilityAdministrationMode mode,
        string selectedLogin,
        string? expectedRunId,
        string? expectedLockCommand,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(targetValidation.Connection!.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var target = await ReadTargetAsync(connection, targetValidation.EndpointKind!, cancellationToken);
        var preflightViolations = await ValidatePreflightAsync(
            connection,
            contract,
            target,
            mode,
            selectedLogin,
            cancellationToken);
        if (preflightViolations.Count != 0)
        {
            return CreateReport(
                contract,
                contractValidation,
                mode,
                selectedLogin,
                target,
                applied: false,
                roles: EmptyRoles(contract),
                markers: EmptyMarkers(),
                preflightViolations);
        }

        if (mode == CapabilityAdministrationMode.Apply
            && (string.IsNullOrWhiteSpace(expectedRunId)
                || string.IsNullOrWhiteSpace(expectedLockCommand)
                || !await AdvisoryLockProtocol.VerifyOwnershipAsync(
                    connection,
                    contract.AdvisoryLock.Key,
                    expectedRunId,
                    expectedLockCommand,
                    AdvisoryLockMode.Exclusive,
                    cancellationToken)))
        {
            return CreateReport(
                contract,
                contractValidation,
                mode,
                selectedLogin,
                target,
                applied: false,
                roles: EmptyRoles(contract),
                markers: EmptyMarkers(),
                [new ContractViolation(
                    "lock.exclusive-ownership.required",
                    "Use QuranDashboard.TestRuntime admin apply --run-id <run-id>.")]);
        }

        var expectedMarkers = ExpectedMarkers(contract, contractValidation);
        var initialState = await ReadStateAsync(
            connection,
            targetValidation.Connection,
            contract,
            selectedLogin,
            expectedMarkers,
            cancellationToken);

        if (mode == CapabilityAdministrationMode.DryRun)
        {
            return CreateReport(
                contract,
                contractValidation,
                mode,
                selectedLogin,
                target,
                applied: false,
                initialState.Roles,
                initialState.Markers,
                []);
        }

        if (mode == CapabilityAdministrationMode.Inspect)
        {
            return CreateReport(
                contract,
                contractValidation,
                mode,
                selectedLogin,
                target,
                applied: false,
                initialState.Roles,
                initialState.Markers,
                initialState.Violations);
        }

        var applied = false;
        if (mode == CapabilityAdministrationMode.Apply
            && initialState.Violations.Any(violation => violation.Code is
                "administration.role.database-owner"
                or "administration.role.development-create-privilege"
                or "administration.role.development-mutation-privilege"))
        {
            return CreateReport(
                contract,
                contractValidation,
                mode,
                selectedLogin,
                target,
                applied: false,
                initialState.Roles,
                initialState.Markers,
                initialState.Violations);
        }

        if (mode == CapabilityAdministrationMode.Apply && initialState.Violations.Count != 0)
        {
            await ApplyAsync(
                connection,
                contract,
                selectedLogin,
                expectedMarkers,
                cancellationToken);
            applied = true;
        }

        var verifiedState = await ReadStateAsync(
            connection,
            targetValidation.Connection,
            contract,
            selectedLogin,
            expectedMarkers,
            cancellationToken);
        var violations = verifiedState.Violations.ToList();
        if (violations.Count == 0)
        {
            violations.AddRange(await VerifyDenialsAsync(connection, contract, cancellationToken));
        }

        return CreateReport(
            contract,
            contractValidation,
            mode,
            selectedLogin,
            target,
            applied,
            verifiedState.Roles,
            verifiedState.Markers,
            OrderViolations(violations));
    }

    private static async Task<TargetReport> ReadTargetAsync(
        NpgsqlConnection connection,
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
        await using var command = new NpgsqlCommand(sql, connection);
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

    private static async Task<IReadOnlyList<ContractViolation>> ValidatePreflightAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        TargetReport target,
        CapabilityAdministrationMode mode,
        string selectedLogin,
        CancellationToken cancellationToken)
    {
        var violations = new List<ContractViolation>();
        if (target.Database != contract.Targets.TestDatabase)
        {
            violations.Add(new ContractViolation("administration.target.changed"));
        }

        if (target.PostgreSqlMajorVersion != contract.PostgresMajorVersion)
        {
            violations.Add(new ContractViolation("administration.postgres-version.unsupported"));
        }

        if (target.InRecovery is not false)
        {
            violations.Add(new ContractViolation("administration.target.in-recovery"));
        }

        if (contract.Roles.AsDictionary().Values.Contains(selectedLogin, StringComparer.Ordinal))
        {
            violations.Add(new ContractViolation("administration.login.capability-role"));
            return OrderViolations(violations);
        }

        const string principalSql = """
            SELECT role.rolcanlogin,
                   role.rolsuper,
                   role.rolcreaterole,
                   role.rolcreatedb,
                   pg_catalog.pg_has_role(@login, database.datdba, 'USAGE')
            FROM pg_catalog.pg_roles AS role
            CROSS JOIN pg_catalog.pg_database AS database
            WHERE role.rolname = @login
              AND database.datname = current_database()
            """;
        await using var command = new NpgsqlCommand(principalSql, connection);
        command.Parameters.AddWithValue("login", selectedLogin);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            violations.Add(new ContractViolation("administration.login.missing"));
            return OrderViolations(violations);
        }

        var canLogin = reader.GetBoolean(0);
        var isSuperuser = reader.GetBoolean(1);
        var canCreateRoles = reader.GetBoolean(2);
        var canCreateDatabases = reader.GetBoolean(3);
        var ownsTarget = reader.GetBoolean(4);
        await reader.DisposeAsync();

        if (!canLogin)
        {
            violations.Add(new ContractViolation("administration.login.not-login"));
        }

        if (target.SessionUser != selectedLogin || target.CurrentUser != selectedLogin)
        {
            violations.Add(new ContractViolation("administration.login.not-session-user"));
        }

        if (mode == CapabilityAdministrationMode.Apply
            && !isSuperuser
            && !(canCreateRoles && canCreateDatabases && ownsTarget))
        {
            violations.Add(new ContractViolation("administration.authority.insufficient"));
        }

        return OrderViolations(violations);
    }

    private static async Task ApplyAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        string selectedLogin,
        IReadOnlyDictionary<string, string> expectedMarkers,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var roles = contract.Roles.AsDictionary();
        foreach (var role in roles)
        {
            if (!await RoleExistsAsync(connection, transaction, role.Value, cancellationToken))
            {
                var createDatabaseAttribute = role.Key == "scratchAdministrator" ? "CREATEDB" : "NOCREATEDB";
                await ExecuteAsync(
                    connection,
                    transaction,
                    $"CREATE ROLE {QuoteIdentifier(role.Value)} NOLOGIN NOSUPERUSER NOCREATEROLE {createDatabaseAttribute} NOREPLICATION NOBYPASSRLS",
                    cancellationToken);
            }

            var databaseAttribute = role.Key == "scratchAdministrator" ? "CREATEDB" : "NOCREATEDB";
            await ExecuteAsync(
                connection,
                transaction,
                $"ALTER ROLE {QuoteIdentifier(role.Value)} NOLOGIN NOSUPERUSER NOCREATEROLE {databaseAttribute} NOREPLICATION NOBYPASSRLS",
                cancellationToken);
            await ReconcileMembershipAsync(
                connection,
                transaction,
                role.Value,
                selectedLogin,
                cancellationToken);
            await RevokePrivilegesAsync(connection, transaction, contract, role.Value, cancellationToken);
        }

        var readableRoles = new[] { contract.Roles.Reader, contract.Roles.Application };
        foreach (var role in readableRoles)
        {
            await ExecuteAsync(
                connection,
                transaction,
                $"GRANT CONNECT ON DATABASE {QuoteIdentifier(contract.Targets.TestDatabase)} TO {QuoteIdentifier(role)}",
                cancellationToken);
            await ExecuteAsync(
                connection,
                transaction,
                $"GRANT USAGE ON SCHEMA public TO {QuoteIdentifier(role)}",
                cancellationToken);
            await GrantTablesAsync(
                connection,
                transaction,
                role,
                "SELECT",
                contract.AllTables().Select(entry => entry.Table),
                cancellationToken);
        }

        await ExecuteAsync(
            connection,
            transaction,
            $"GRANT CONNECT ON DATABASE {QuoteIdentifier(contract.Targets.TestDatabase)} TO {QuoteIdentifier(contract.Roles.Resetter)}",
            cancellationToken);
        await ExecuteAsync(
            connection,
            transaction,
            $"GRANT USAGE ON SCHEMA public TO {QuoteIdentifier(contract.Roles.Resetter)}",
            cancellationToken);
        await GrantTablesAsync(
            connection,
            transaction,
            contract.Roles.Resetter,
            "SELECT",
            contract.DataClasses.MutableApplicationState,
            cancellationToken);

        await GrantTablesAsync(
            connection,
            transaction,
            contract.Roles.Application,
            "INSERT, UPDATE, DELETE",
            contract.DataClasses.MutableApplicationState,
            cancellationToken);
        await GrantTablesAsync(
            connection,
            transaction,
            contract.Roles.Resetter,
            "TRUNCATE",
            contract.DataClasses.MutableApplicationState.Where(table => table != contract.LinkingDataBaseline.Table),
            cancellationToken);
        await GrantTablesAsync(
            connection,
            transaction,
            contract.Roles.Resetter,
            "UPDATE",
            [contract.LinkingDataBaseline.Table],
            cancellationToken);

        var mutableSequences = await ReadMutableSequencesAsync(
            connection,
            transaction,
            contract,
            cancellationToken);
        if (mutableSequences.Count != 0)
        {
            await ExecuteAsync(
                connection,
                transaction,
                $"GRANT USAGE, SELECT ON SEQUENCE {JoinPublicObjects(mutableSequences)} TO {QuoteIdentifier(contract.Roles.Application)}",
                cancellationToken);
        }

        foreach (var marker in expectedMarkers)
        {
            var markerName = contract.Markers.AsDictionary()[marker.Key];
            await ExecuteAsync(
                connection,
                transaction,
                $"ALTER DATABASE {QuoteIdentifier(contract.Targets.TestDatabase)} SET {markerName} TO {QuoteLiteral(marker.Value)}",
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ReconcileMembershipAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string capabilityRole,
        string selectedLogin,
        CancellationToken cancellationToken)
    {
        const string membersSql = """
            SELECT member.rolname
            FROM pg_catalog.pg_auth_members AS membership
            INNER JOIN pg_catalog.pg_roles AS granted_role ON granted_role.oid = membership.roleid
            INNER JOIN pg_catalog.pg_roles AS member ON member.oid = membership.member
            WHERE granted_role.rolname = @role
            """;
        var members = await ReadNamesAsync(
            connection,
            transaction,
            membersSql,
            capabilityRole,
            cancellationToken);
        foreach (var member in members)
        {
            await ExecuteAsync(
                connection,
                transaction,
                $"REVOKE {QuoteIdentifier(capabilityRole)} FROM {QuoteIdentifier(member)}",
                cancellationToken);
        }

        const string parentsSql = """
            SELECT granted_role.rolname
            FROM pg_catalog.pg_auth_members AS membership
            INNER JOIN pg_catalog.pg_roles AS granted_role ON granted_role.oid = membership.roleid
            INNER JOIN pg_catalog.pg_roles AS member ON member.oid = membership.member
            WHERE member.rolname = @role
            """;
        var parents = await ReadNamesAsync(
            connection,
            transaction,
            parentsSql,
            capabilityRole,
            cancellationToken);
        foreach (var parent in parents)
        {
            await ExecuteAsync(
                connection,
                transaction,
                $"REVOKE {QuoteIdentifier(parent)} FROM {QuoteIdentifier(capabilityRole)}",
                cancellationToken);
        }

        await ExecuteAsync(
            connection,
            transaction,
            $"GRANT {QuoteIdentifier(capabilityRole)} TO {QuoteIdentifier(selectedLogin)} WITH ADMIN FALSE, INHERIT TRUE, SET TRUE",
            cancellationToken);
    }

    private static async Task RevokePrivilegesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        string role,
        CancellationToken cancellationToken)
    {
        var quotedRole = QuoteIdentifier(role);
        await ExecuteAsync(
            connection,
            transaction,
            $"REVOKE ALL PRIVILEGES ON DATABASE {QuoteIdentifier(contract.Targets.TestDatabase)} FROM {quotedRole}",
            cancellationToken);
        await ExecuteAsync(connection, transaction, $"REVOKE ALL PRIVILEGES ON SCHEMA public FROM {quotedRole}", cancellationToken);
        await ExecuteAsync(connection, transaction, $"REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM {quotedRole}", cancellationToken);
        await ExecuteAsync(connection, transaction, $"REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public FROM {quotedRole}", cancellationToken);
        await ExecuteAsync(connection, transaction, $"REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public FROM {quotedRole}", cancellationToken);
    }

    private static async Task GrantTablesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string role,
        string privileges,
        IEnumerable<string> tables,
        CancellationToken cancellationToken)
    {
        var names = tables.Distinct(StringComparer.Ordinal).ToArray();
        if (names.Length == 0)
        {
            return;
        }

        await ExecuteAsync(
            connection,
            transaction,
            $"GRANT {privileges} ON TABLE {JoinPublicObjects(names)} TO {QuoteIdentifier(role)}",
            cancellationToken);
    }

    private static async Task<AdministrationState> ReadStateAsync(
        NpgsqlConnection connection,
        NpgsqlConnectionStringBuilder targetConnection,
        DatabaseContract contract,
        string selectedLogin,
        IReadOnlyDictionary<string, string> expectedMarkers,
        CancellationToken cancellationToken)
    {
        var violations = new List<ContractViolation>();
        var mutableSequences = await ReadMutableSequencesAsync(connection, null, contract, cancellationToken);
        var allSequences = await ReadPublicSequencesAsync(connection, cancellationToken);
        var developmentMutationPrivileges = await ReadDevelopmentMutationPrivilegesAsync(
            connection,
            targetConnection,
            contract,
            cancellationToken);
        var roleReports = new Dictionary<string, CapabilityRoleReport>(StringComparer.Ordinal);
        foreach (var role in contract.Roles.AsDictionary())
        {
            var report = await ReadRoleAsync(
                connection,
                contract,
                role.Key,
                role.Value,
                selectedLogin,
                mutableSequences,
                allSequences,
                developmentMutationPrivileges[role.Value],
                cancellationToken);
            roleReports[role.Key] = report;
            if (!report.Exists)
            {
                violations.Add(new ContractViolation("administration.role.missing", role.Key));
                continue;
            }

            if (!report.NoLogin || !report.ExpectedAttributes)
            {
                violations.Add(new ContractViolation("administration.role.attributes-invalid", role.Key));
            }

            if (!report.SelectedLoginIsOnlyMember || !report.HasNoInheritedRoles)
            {
                violations.Add(new ContractViolation("administration.role.membership-invalid", role.Key));
            }

            if (report.OwnsDevelopmentDatabase || report.OwnsTestDatabase)
            {
                violations.Add(new ContractViolation("administration.role.database-owner", role.Key));
            }

            if (report.CanCreateInDevelopmentDatabase)
            {
                violations.Add(new ContractViolation("administration.role.development-create-privilege", role.Key));
            }

            if (report.CanMutateDevelopmentDatabase)
            {
                violations.Add(new ContractViolation("administration.role.development-mutation-privilege", role.Key));
            }

            if (!report.PrivilegesMatch)
            {
                violations.Add(new ContractViolation("administration.role.privileges-invalid", role.Key));
            }
        }

        var markerStates = await ReadMarkersAsync(
            connection,
            contract,
            expectedMarkers,
            cancellationToken);
        foreach (var marker in markerStates.Where(marker => marker.Value.MatchesExpected is not true))
        {
            violations.Add(new ContractViolation("administration.marker.invalid", marker.Key));
        }

        return new AdministrationState(
            roleReports,
            markerStates,
            OrderViolations(violations));
    }

    private static async Task<CapabilityRoleReport> ReadRoleAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        string roleKey,
        string roleName,
        string selectedLogin,
        IReadOnlyCollection<string> mutableSequences,
        IReadOnlyCollection<string> allSequences,
        bool canMutateDevelopmentDatabase,
        CancellationToken cancellationToken)
    {
        const string roleSql = """
            SELECT role.rolcanlogin,
                   role.rolsuper,
                   role.rolcreaterole,
                   role.rolcreatedb,
                   role.rolreplication,
                   role.rolbypassrls,
                   EXISTS (
                       SELECT 1 FROM pg_catalog.pg_database AS database
                       WHERE database.datname = @development AND database.datdba = role.oid),
                   EXISTS (
                       SELECT 1 FROM pg_catalog.pg_database AS database
                       WHERE database.datname = @test AND database.datdba = role.oid),
                   CASE WHEN EXISTS (
                       SELECT 1 FROM pg_catalog.pg_database AS database
                       WHERE database.datname = @development)
                   THEN pg_catalog.has_database_privilege(role.rolname, @development, 'CREATE')
                   ELSE false
                   END
            FROM pg_catalog.pg_roles AS role
            WHERE role.rolname = @role
            """;
        await using var command = new NpgsqlCommand(roleSql, connection);
        command.Parameters.AddWithValue("role", roleName);
        command.Parameters.AddWithValue("development", contract.Targets.DevelopmentDatabase);
        command.Parameters.AddWithValue("test", contract.Targets.TestDatabase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new CapabilityRoleReport(roleName, false, false, false, false, false, false, false, false, false, false);
        }

        var noLogin = !reader.GetBoolean(0);
        var expectedAttributes = !reader.GetBoolean(1)
                                 && !reader.GetBoolean(2)
                                 && reader.GetBoolean(3) == (roleKey == "scratchAdministrator")
                                 && !reader.GetBoolean(4)
                                 && !reader.GetBoolean(5);
        var ownsDevelopment = reader.GetBoolean(6);
        var ownsTest = reader.GetBoolean(7);
        var canCreateInDevelopment = reader.GetBoolean(8);
        await reader.DisposeAsync();

        var membershipMatches = await SelectedLoginIsOnlyMemberAsync(
            connection,
            roleName,
            selectedLogin,
            cancellationToken);
        var parents = await ReadRoleRelationNamesAsync(connection, roleName, grantedRole: false, cancellationToken);
        var privilegesMatch = await PrivilegesMatchAsync(
            connection,
            contract,
            roleKey,
            roleName,
            mutableSequences,
            allSequences,
            cancellationToken);
        return new CapabilityRoleReport(
            roleName,
            true,
            noLogin,
            expectedAttributes,
            membershipMatches,
            parents.Count == 0,
            ownsDevelopment,
            ownsTest,
            canCreateInDevelopment,
            canMutateDevelopmentDatabase,
            privilegesMatch);
    }

    private static async Task<bool> PrivilegesMatchAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        string roleKey,
        string roleName,
        IReadOnlyCollection<string> mutableSequences,
        IReadOnlyCollection<string> allSequences,
        CancellationToken cancellationToken)
    {
        var allTables = contract.AllTables().Select(entry => entry.Table).ToArray();
        var protectedTables = contract.DataClasses.CanonicalQuranData
            .Concat(contract.DataClasses.SystemCatalogue)
            .Concat(contract.DataClasses.SchemaState)
            .ToArray();
        var mutableTables = contract.DataClasses.MutableApplicationState;
        var resetTables = mutableTables.Where(table => table != contract.LinkingDataBaseline.Table).ToArray();
        var protectedSequences = allSequences.Except(mutableSequences, StringComparer.Ordinal).ToArray();

        var canSelectAll = await HasPrivilegeOnAllTablesAsync(
            connection,
            roleName,
            allTables,
            "SELECT",
            cancellationToken);
        var canMutateProtected = await HasPrivilegeOnAnyTablesAsync(
            connection,
            roleName,
            protectedTables,
            ["INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "TRIGGER", "MAINTAIN"],
            cancellationToken);
        var canCreateInSchema = await ScalarBoolAsync(
            connection,
            "SELECT pg_catalog.has_schema_privilege(@role, 'public', 'CREATE')",
            roleName,
            cancellationToken);
        var canCreateInTestDatabase = await ScalarBoolAsync(
            connection,
            "SELECT pg_catalog.has_database_privilege(@role, current_database(), 'CREATE')",
            roleName,
            cancellationToken);

        if (roleKey == "reader")
        {
            return canSelectAll
                   && !canMutateProtected
                   && !await HasPrivilegeOnAnyTablesAsync(
                       connection,
                       roleName,
                       mutableTables,
                       ["INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "TRIGGER", "MAINTAIN"],
                       cancellationToken)
                   && !await HasPrivilegeOnAnySequencesAsync(
                       connection, roleName, allSequences, ["SELECT", "USAGE", "UPDATE"], cancellationToken)
                   && !canCreateInSchema
                   && !canCreateInTestDatabase;
        }

        if (roleKey == "application")
        {
            return canSelectAll
                   && !canMutateProtected
                   && await HasPrivilegeOnAllTablesAsync(connection, roleName, mutableTables, "INSERT", cancellationToken)
                   && await HasPrivilegeOnAllTablesAsync(connection, roleName, mutableTables, "UPDATE", cancellationToken)
                   && await HasPrivilegeOnAllTablesAsync(connection, roleName, mutableTables, "DELETE", cancellationToken)
                   && !await HasPrivilegeOnAnyTablesAsync(
                       connection,
                       roleName,
                       mutableTables,
                       ["TRUNCATE", "REFERENCES", "TRIGGER", "MAINTAIN"],
                       cancellationToken)
                   && await HasPrivilegeOnAllSequencesAsync(connection, roleName, mutableSequences, cancellationToken)
                   && !await HasPrivilegeOnAnySequencesAsync(
                       connection, roleName, protectedSequences, ["SELECT", "USAGE", "UPDATE"], cancellationToken)
                   && !await HasPrivilegeOnAnySequencesAsync(
                       connection, roleName, mutableSequences, ["UPDATE"], cancellationToken)
                   && !canCreateInSchema
                   && !canCreateInTestDatabase;
        }

        if (roleKey == "resetter")
        {
            return !canSelectAll
                   && await HasPrivilegeOnAllTablesAsync(connection, roleName, mutableTables, "SELECT", cancellationToken)
                   && !await HasPrivilegeOnAnyTablesAsync(connection, roleName, protectedTables, ["SELECT"], cancellationToken)
                   && !canMutateProtected
                   && await HasPrivilegeOnAllTablesAsync(connection, roleName, resetTables, "TRUNCATE", cancellationToken)
                   && !await HasPrivilegeOnAnyTablesAsync(
                       connection, roleName, [contract.LinkingDataBaseline.Table], ["TRUNCATE"], cancellationToken)
                   && await HasPrivilegeOnAllTablesAsync(
                       connection, roleName, [contract.LinkingDataBaseline.Table], "UPDATE", cancellationToken)
                   && !await HasPrivilegeOnAnyTablesAsync(
                       connection,
                       roleName,
                       mutableTables,
                       ["INSERT", "DELETE", "REFERENCES", "TRIGGER", "MAINTAIN"],
                       cancellationToken)
                   && !await HasPrivilegeOnAnyTablesAsync(connection, roleName, resetTables, ["UPDATE"], cancellationToken)
                   && !await HasPrivilegeOnAnySequencesAsync(
                       connection, roleName, allSequences, ["SELECT", "USAGE", "UPDATE"], cancellationToken)
                   && !canCreateInSchema
                   && !canCreateInTestDatabase;
        }

        return !canSelectAll
               && !canMutateProtected
               && !await HasPrivilegeOnAnyTablesAsync(
                   connection,
                   roleName,
                   mutableTables,
                   ["INSERT", "UPDATE", "DELETE", "TRUNCATE", "REFERENCES", "TRIGGER", "MAINTAIN"],
                   cancellationToken)
               && !await HasPrivilegeOnAnySequencesAsync(
                   connection, roleName, allSequences, ["SELECT", "USAGE", "UPDATE"], cancellationToken)
               && !canCreateInSchema
               && !canCreateInTestDatabase;
    }

    private static async Task<IReadOnlyDictionary<string, MarkerState>> ReadMarkersAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        IReadOnlyDictionary<string, string> expectedMarkers,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, MarkerState>(StringComparer.Ordinal);
        foreach (var marker in expectedMarkers)
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
            command.Parameters.AddWithValue("name", contract.Markers.AsDictionary()[marker.Key]);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            result[marker.Key] = new MarkerState(
                value is not null and not DBNull,
                value is not null and not DBNull && (string)value == marker.Value);
        }

        return result;
    }

    private static async Task<IReadOnlyList<ContractViolation>> VerifyDenialsAsync(
        NpgsqlConnection connection,
        DatabaseContract contract,
        CancellationToken cancellationToken)
    {
        var violations = new List<ContractViolation>();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var protectedTable = contract.DataClasses.CanonicalQuranData.First();
        foreach (var role in contract.Roles.AsDictionary())
        {
            await ExecuteAsync(
                connection,
                transaction,
                $"SET LOCAL ROLE {QuoteIdentifier(role.Value)}",
                cancellationToken);
            if (!await IsDeniedAsync(
                    connection,
                    transaction,
                    $"DELETE FROM public.{QuoteIdentifier(protectedTable)} WHERE false",
                    cancellationToken))
            {
                violations.Add(new ContractViolation("administration.verify.protected-write-not-rejected", role.Key));
            }

            if (!await IsDeniedAsync(
                    connection,
                    transaction,
                    $"ALTER DATABASE {QuoteIdentifier(contract.Targets.TestDatabase)} SET "
                    + $"{contract.Markers.CapabilityEnabled} TO 'false'",
                    cancellationToken))
            {
                violations.Add(new ContractViolation("administration.verify.metadata-write-not-rejected", role.Key));
            }

            if (!await CanReadManagedMarkersAsync(
                    connection,
                    transaction,
                    contract,
                    cancellationToken))
            {
                violations.Add(new ContractViolation("administration.verify.metadata-not-readable", role.Key));
            }

            await ExecuteAsync(connection, transaction, "RESET ROLE", cancellationToken);
        }

        await ExecuteAsync(
            connection,
            transaction,
            $"SET LOCAL ROLE {QuoteIdentifier(contract.Roles.ScratchAdministrator)}",
            cancellationToken);
        if (!await IsDeniedAsync(
                connection,
                transaction,
                "CREATE SCHEMA quran_dashboard_test_out_of_scope_verification",
                cancellationToken))
        {
            violations.Add(new ContractViolation("administration.verify.out-of-scope-test-write-not-rejected"));
        }

        if (await DatabaseExistsAsync(connection, transaction, contract.Targets.DevelopmentDatabase, cancellationToken)
            && !await IsDeniedAsync(
                connection,
                transaction,
                $"ALTER DATABASE {QuoteIdentifier(contract.Targets.DevelopmentDatabase)} CONNECTION LIMIT -1",
                cancellationToken))
        {
            violations.Add(new ContractViolation("administration.verify.development-write-not-rejected"));
        }

        await transaction.RollbackAsync(cancellationToken);
        return OrderViolations(violations);
    }

    private static async Task<bool> IsDeniedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(connection, transaction, "SAVEPOINT privilege_verification", cancellationToken);
        try
        {
            await ExecuteAsync(connection, transaction, sql, cancellationToken);
            await ExecuteAsync(connection, transaction, "ROLLBACK TO SAVEPOINT privilege_verification", cancellationToken);
            return false;
        }
        catch (PostgresException exception) when (exception.SqlState == InsufficientPrivilegeSqlState)
        {
            await ExecuteAsync(connection, transaction, "ROLLBACK TO SAVEPOINT privilege_verification", cancellationToken);
            return true;
        }
    }

    private static async Task<bool> CanReadManagedMarkersAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DatabaseContract contract,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT count(*)
            FROM pg_catalog.pg_db_role_setting AS settings
            INNER JOIN pg_catalog.pg_database AS database ON database.oid = settings.setdatabase
            CROSS JOIN LATERAL unnest(settings.setconfig) AS configured(setting)
            WHERE database.datname = current_database()
              AND settings.setrole = 0
              AND split_part(configured.setting, '=', 1) = ANY(@names)
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(
            "names",
            NpgsqlDbType.Array | NpgsqlDbType.Text,
            ManagedMarkerKeys.Select(key => contract.Markers.AsDictionary()[key]).ToArray());
        return (long)(await command.ExecuteScalarAsync(cancellationToken))! == ManagedMarkerKeys.Length;
    }

    private static async Task<IReadOnlyList<string>> ReadMutableSequencesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        DatabaseContract contract,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT sequence.relname
            FROM pg_catalog.pg_class AS sequence
            INNER JOIN pg_catalog.pg_depend AS dependency
                ON dependency.classid = 'pg_class'::regclass
               AND dependency.objid = sequence.oid
               AND dependency.deptype IN ('a', 'i')
            INNER JOIN pg_catalog.pg_class AS table_relation ON table_relation.oid = dependency.refobjid
            INNER JOIN pg_catalog.pg_namespace AS schema ON schema.oid = table_relation.relnamespace
            WHERE sequence.relkind = 'S'
              AND schema.nspname = 'public'
              AND table_relation.relname = ANY(@tables)
            ORDER BY sequence.relname
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(
            "tables",
            NpgsqlDbType.Array | NpgsqlDbType.Text,
            contract.DataClasses.MutableApplicationState);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var sequences = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            sequences.Add(reader.GetString(0));
        }

        return sequences;
    }

    private static async Task<IReadOnlyList<string>> ReadPublicSequencesAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT sequence.relname
            FROM pg_catalog.pg_class AS sequence
            INNER JOIN pg_catalog.pg_namespace AS schema ON schema.oid = sequence.relnamespace
            WHERE sequence.relkind = 'S'
              AND schema.nspname = 'public'
            ORDER BY sequence.relname
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var sequences = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            sequences.Add(reader.GetString(0));
        }

        return sequences;
    }

    private static async Task<IReadOnlyDictionary<string, bool>> ReadDevelopmentMutationPrivilegesAsync(
        NpgsqlConnection targetConnection,
        NpgsqlConnectionStringBuilder target,
        DatabaseContract contract,
        CancellationToken cancellationToken)
    {
        var result = contract.Roles.AsDictionary().Values.ToDictionary(role => role, _ => false, StringComparer.Ordinal);
        await using (var existsCommand = new NpgsqlCommand(
                         "SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datname = @database)",
                         targetConnection))
        {
            existsCommand.Parameters.AddWithValue("database", contract.Targets.DevelopmentDatabase);
            if ((bool)(await existsCommand.ExecuteScalarAsync(cancellationToken))! is false)
            {
                return result;
            }
        }

        var developmentTarget = new NpgsqlConnectionStringBuilder(target.ConnectionString)
        {
            Database = contract.Targets.DevelopmentDatabase,
            Pooling = false,
            ApplicationName = "quran-dashboard-test-runtime-development-acl-audit",
        };
        developmentTarget.Options = string.Join(
            ' ',
            new[] { developmentTarget.Options, "-c", "default_transaction_read_only=on" }
                .Where(option => !string.IsNullOrWhiteSpace(option)));

        await using var connection = new NpgsqlConnection(developmentTarget.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        await ExecuteAsync(connection, transaction, "SET TRANSACTION READ ONLY", cancellationToken);
        const string sql = """
            SELECT role.rolname,
                   bool_or(
                       pg_catalog.has_schema_privilege(role.rolname, schema.oid, 'CREATE')
                       OR EXISTS (
                           SELECT 1
                           FROM pg_catalog.pg_class AS relation
                           WHERE relation.relnamespace = schema.oid
                             AND relation.relkind IN ('r', 'p', 'v', 'm', 'f')
                             AND pg_catalog.has_table_privilege(
                                 role.rolname,
                                 relation.oid,
                                 'INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER, MAINTAIN'))
                       OR EXISTS (
                           SELECT 1
                           FROM pg_catalog.pg_class AS sequence
                           WHERE sequence.relnamespace = schema.oid
                             AND sequence.relkind = 'S'
                             AND CASE WHEN sequence.relkind = 'S'
                                 THEN pg_catalog.has_sequence_privilege(role.rolname, sequence.oid, 'USAGE, UPDATE')
                                 ELSE false
                             END)
                       OR EXISTS (
                           SELECT 1
                           FROM pg_catalog.pg_proc AS function
                           WHERE function.pronamespace = schema.oid
                             AND function.prosecdef
                             AND pg_catalog.has_function_privilege(role.rolname, function.oid, 'EXECUTE')))
            FROM pg_catalog.pg_roles AS role
            CROSS JOIN pg_catalog.pg_namespace AS schema
            WHERE role.rolname = ANY(@roles)
              AND schema.nspname <> 'information_schema'
              AND schema.nspname !~ '^pg_'
            GROUP BY role.rolname
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(
            "roles",
            NpgsqlDbType.Array | NpgsqlDbType.Text,
            result.Keys.ToArray());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result[reader.GetString(0)] = reader.GetBoolean(1);
        }

        await reader.DisposeAsync();
        await transaction.RollbackAsync(cancellationToken);
        return result;
    }

    private static async Task<bool> HasPrivilegeOnAllSequencesAsync(
        NpgsqlConnection connection,
        string role,
        IReadOnlyCollection<string> sequences,
        CancellationToken cancellationToken)
    {
        if (sequences.Count == 0)
        {
            return true;
        }

        const string sql = """
            SELECT bool_and(pg_catalog.has_sequence_privilege(
                @role,
                format('%I.%I', 'public', sequence_name),
                'USAGE')
                AND pg_catalog.has_sequence_privilege(
                    @role,
                    format('%I.%I', 'public', sequence_name),
                    'SELECT'))
            FROM unnest(@sequences) AS sequence_names(sequence_name)
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("sequences", NpgsqlDbType.Array | NpgsqlDbType.Text, sequences.ToArray());
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<bool> HasPrivilegeOnAnySequencesAsync(
        NpgsqlConnection connection,
        string role,
        IReadOnlyCollection<string> sequences,
        IReadOnlyCollection<string> privileges,
        CancellationToken cancellationToken)
    {
        if (sequences.Count == 0 || privileges.Count == 0)
        {
            return false;
        }

        const string sql = """
            SELECT bool_or(pg_catalog.has_sequence_privilege(
                @role,
                format('%I.%I', 'public', sequence_name),
                privilege))
            FROM unnest(@sequences) AS sequence_names(sequence_name)
            CROSS JOIN unnest(@privileges) AS privilege_names(privilege)
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("sequences", NpgsqlDbType.Array | NpgsqlDbType.Text, sequences.ToArray());
        command.Parameters.AddWithValue("privileges", NpgsqlDbType.Array | NpgsqlDbType.Text, privileges.ToArray());
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<bool> HasPrivilegeOnAllTablesAsync(
        NpgsqlConnection connection,
        string role,
        IReadOnlyCollection<string> tables,
        string privilege,
        CancellationToken cancellationToken)
    {
        if (tables.Count == 0)
        {
            return true;
        }

        const string sql = """
            SELECT bool_and(pg_catalog.has_table_privilege(
                @role,
                format('%I.%I', 'public', table_name),
                @privilege))
            FROM unnest(@tables) AS table_names(table_name)
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("privilege", privilege);
        command.Parameters.AddWithValue("tables", NpgsqlDbType.Array | NpgsqlDbType.Text, tables.ToArray());
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<bool> HasPrivilegeOnAnyTablesAsync(
        NpgsqlConnection connection,
        string role,
        IReadOnlyCollection<string> tables,
        IReadOnlyCollection<string> privileges,
        CancellationToken cancellationToken)
    {
        if (tables.Count == 0 || privileges.Count == 0)
        {
            return false;
        }

        const string sql = """
            SELECT bool_or(pg_catalog.has_table_privilege(
                @role,
                format('%I.%I', 'public', table_name),
                privilege))
            FROM unnest(@tables) AS table_names(table_name)
            CROSS JOIN unnest(@privileges) AS privilege_names(privilege)
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("tables", NpgsqlDbType.Array | NpgsqlDbType.Text, tables.ToArray());
        command.Parameters.AddWithValue("privileges", NpgsqlDbType.Array | NpgsqlDbType.Text, privileges.ToArray());
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<bool> ScalarBoolAsync(
        NpgsqlConnection connection,
        string sql,
        string role,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("role", role);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<IReadOnlyList<string>> ReadRoleRelationNamesAsync(
        NpgsqlConnection connection,
        string role,
        bool grantedRole,
        CancellationToken cancellationToken)
    {
        var sql = grantedRole
            ? """
                SELECT member.rolname
                FROM pg_catalog.pg_auth_members AS membership
                INNER JOIN pg_catalog.pg_roles AS granted ON granted.oid = membership.roleid
                INNER JOIN pg_catalog.pg_roles AS member ON member.oid = membership.member
                WHERE granted.rolname = @role
                ORDER BY member.rolname
                """
            : """
                SELECT granted.rolname
                FROM pg_catalog.pg_auth_members AS membership
                INNER JOIN pg_catalog.pg_roles AS granted ON granted.oid = membership.roleid
                INNER JOIN pg_catalog.pg_roles AS member ON member.oid = membership.member
                WHERE member.rolname = @role
                ORDER BY granted.rolname
                """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("role", role);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    private static async Task<bool> SelectedLoginIsOnlyMemberAsync(
        NpgsqlConnection connection,
        string role,
        string selectedLogin,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT count(*) = 1
               AND bool_and(member.rolname = @login)
               AND bool_and(NOT membership.admin_option)
               AND bool_and(membership.inherit_option)
               AND bool_and(membership.set_option)
            FROM pg_catalog.pg_auth_members AS membership
            INNER JOIN pg_catalog.pg_roles AS granted ON granted.oid = membership.roleid
            INNER JOIN pg_catalog.pg_roles AS member ON member.oid = membership.member
            WHERE granted.rolname = @role
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("login", selectedLogin);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<IReadOnlyList<string>> ReadNamesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        string role,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("role", role);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var names = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static async Task<bool> RoleExistsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string role,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = @role)",
            connection,
            transaction);
        command.Parameters.AddWithValue("role", role);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<bool> DatabaseExistsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string database,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datname = @database)",
            connection,
            transaction);
        command.Parameters.AddWithValue("database", database);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
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

    private static IReadOnlyDictionary<string, string> ExpectedMarkers(
        DatabaseContract contract,
        ContractValidationResult validation) => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["capabilityEnabled"] = "true",
            ["resetEnabled"] = "true",
            ["contractVersion"] = contract.ContractVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["capabilityMetadataVersion"] = contract.CapabilityMetadataVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["mutableStateDirty"] = "false",
            ["migrationHead"] = validation.ExpectedMigrations.Last(),
        };

    private static TestRuntimeReport CreateReport(
        DatabaseContract contract,
        ContractValidationResult validation,
        CapabilityAdministrationMode mode,
        string selectedLogin,
        TargetReport target,
        bool applied,
        IReadOnlyDictionary<string, CapabilityRoleReport> roles,
        IReadOnlyDictionary<string, MarkerState> markers,
        IReadOnlyList<ContractViolation> violations)
    {
        var modeName = ModeName(mode);
        return new TestRuntimeReport(
            $"admin-{modeName}",
            violations.Count == 0,
            DatabaseInspector.ToContractReport(contract, validation),
            target,
            null,
            null,
            null,
            null,
            violations,
            Administration: new CapabilityAdministrationReport(
                modeName,
                selectedLogin,
                applied,
                roles.Count != 0 && roles.Values.All(RoleCompliant) && markers.Values.All(marker => marker.MatchesExpected is true),
                PlannedOperations,
                roles,
                markers));
    }

    private static bool RoleCompliant(CapabilityRoleReport role) =>
        role.Exists
        && role.NoLogin
        && role.ExpectedAttributes
        && role.SelectedLoginIsOnlyMember
        && role.HasNoInheritedRoles
        && !role.OwnsDevelopmentDatabase
        && !role.OwnsTestDatabase
        && !role.CanCreateInDevelopmentDatabase
        && !role.CanMutateDevelopmentDatabase
        && role.PrivilegesMatch;

    private static IReadOnlyDictionary<string, CapabilityRoleReport> EmptyRoles(DatabaseContract contract) =>
        contract.Roles.AsDictionary().ToDictionary(
            role => role.Key,
            role => new CapabilityRoleReport(role.Value, false, false, false, false, false, false, false, false, false, false),
            StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, MarkerState> EmptyMarkers() =>
        ManagedMarkerKeys.ToDictionary(key => key, _ => new MarkerState(false, false), StringComparer.Ordinal);

    private static IReadOnlyList<ContractViolation> OrderViolations(IEnumerable<ContractViolation> violations) =>
        violations.Distinct()
            .OrderBy(violation => violation.Code, StringComparer.Ordinal)
            .ThenBy(violation => violation.Subject, StringComparer.Ordinal)
            .ToArray();

    private static string ModeName(CapabilityAdministrationMode mode) => mode switch
    {
        CapabilityAdministrationMode.Inspect => "inspect",
        CapabilityAdministrationMode.DryRun => "dry-run",
        CapabilityAdministrationMode.Apply => "apply",
        CapabilityAdministrationMode.Verify => "verify",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    private static string JoinPublicObjects(IEnumerable<string> names) =>
        string.Join(", ", names.Select(name => $"public.{QuoteIdentifier(name)}"));

    private static string QuoteIdentifier(string value) => new NpgsqlCommandBuilder().QuoteIdentifier(value);

    private static string QuoteLiteral(string value) => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

    private sealed record AdministrationState(
        IReadOnlyDictionary<string, CapabilityRoleReport> Roles,
        IReadOnlyDictionary<string, MarkerState> Markers,
        IReadOnlyList<ContractViolation> Violations);
}
