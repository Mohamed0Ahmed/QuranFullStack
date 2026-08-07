using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using QuranDashboard.Application.Access.LegacyRoleConversion;
using QuranDashboard.Application.Access.OwnerReconciliation;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessProcessGlobalCollection))]
public sealed class AccessMigrationPathTests(AccessMigrationTestFixture fixture)
{
    [Fact]
    public async Task CurrentDatabase_CleanLegacyIdentities_MigrateThroughTheStagedAccessSchema()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToAsync(db, "RequireAbwabDoorSection");
        await InsertLegacyUserAsync(db, "legacy-one", " First@Example.Test ");
        await InsertLegacyUserAsync(db, "legacy-two", "Second@Example.Test");

        var preflight = new EmailIdentityPreflight(db, new EmailIdentityNormalizer());
        var legacyScan = await preflight.ScanAsync(CancellationToken.None);

        legacyScan.IsClean.Should().BeTrue();
        await fixture.MigrateToAsync(db, "AddAuthorizationAccessFoundation");
        (await HasNormalizedEmailUniqueIndexAsync(db)).Should().BeFalse();

        var stagedScan = await preflight.ScanAsync(CancellationToken.None);

        stagedScan.MissingNormalizedEmailUserIds.Should().HaveCount(2);
        (await preflight.BackfillAsync(CancellationToken.None)).Should().Be(2);
        (await preflight.ScanAsync(CancellationToken.None)).IsClean.Should().BeTrue();

        await fixture.MigrateToAsync(db, "RequireNormalizedEmail");

        await AssertNormalizedEmailConstraintAsync(db);
        var normalizedEmails = await ReadNormalizedEmailsAsync(db);
        normalizedEmails.Should().Equal("FIRST@EXAMPLE.TEST", "SECOND@EXAMPLE.TEST");
    }

    [Fact]
    public async Task CurrentDatabase_CollidingLegacyIdentities_StopBeforeTheAdditiveMigrationWithoutMutation()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToAsync(db, "RequireAbwabDoorSection");
        await InsertLegacyUserAsync(db, "legacy-one", "Teacher@Example.Test");
        await InsertLegacyUserAsync(db, "legacy-two", " teacher@example.test ");

        var run = await AccessAdminInProcess.RunAsync(database.ConnectionString, "identity", "scan");

        run.ExitCode.Should().Be(3);
        (await ReadLegacyEmailsAsync(db)).Should().Equal(
            "Teacher@Example.Test",
            " teacher@example.test ");
        (await NormalizedEmailColumnExistsAsync(db)).Should().BeFalse();
    }

    [Fact]
    public async Task CurrentDatabase_FinalMigration_RejectsUnbackfilledLegacyRows()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToAsync(db, "RequireAbwabDoorSection");
        await InsertLegacyUserAsync(db, "legacy-one", "First@Example.Test");
        await fixture.MigrateToAsync(db, "AddAuthorizationAccessFoundation");

        var applyFinalMigration = () => fixture.MigrateToAsync(db, "RequireNormalizedEmail");

        await applyFinalMigration.Should().ThrowAsync<PostgresException>();
    }

    [Fact]
    public async Task LegacyRoleConversion_LeavesFormerAdminAndEditorRoleLessWithoutDirectGrants_ThenPermitsCleanup()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToAsync(db, "RequireNormalizedEmail");
        var ownerId = await InsertRoleBoundUserAsync(db, "legacy-owner", "legacy-owner@example.test", 1, UserStatus.Active);
        var adminId = await InsertRoleBoundUserAsync(db, "legacy-admin", "legacy-admin@example.test", 2, UserStatus.Active);
        var editorId = await InsertRoleBoundUserAsync(db, "legacy-editor", "legacy-editor@example.test", 3, UserStatus.Active);
        var conversion = CreateLegacyRoleConversionService(db, "legacy-owner@example.test");

        var inventory = await conversion.InventoryAsync(CancellationToken.None);

        inventory.CanConvert.Should().BeTrue();
        inventory.OwnerUsers.Select(user => user.Id).Should().Equal(ownerId);
        inventory.AdminUsers.Select(user => user.Id).Should().Equal(adminId);
        inventory.EditorUsers.Select(user => user.Id).Should().Equal(editorId);
        inventory.AdminOrEditorUsers.Should().OnlyContain(user => user.DirectGrantCount == 0);

        var conversionResult = await conversion.ConvertAsync(CancellationToken.None);

        conversionResult.Applied.Should().BeTrue();
        conversionResult.CanConvert.Should().BeTrue();
        conversionResult.HasAdminOrEditorReferences.Should().BeFalse();
        db.ChangeTracker.Clear();
        var formerRoleIds = await db.AccessUsers
            .Where(user => user.Id == adminId || user.Id == editorId)
            .OrderBy(user => user.Id)
            .Select(user => user.RoleId)
            .ToListAsync();
        formerRoleIds.Should().HaveCount(2);
        formerRoleIds.Should().OnlyContain(roleId => roleId == null);
        (await db.AccessUserPermissions
            .CountAsync(grant => grant.UserId == adminId || grant.UserId == editorId))
            .Should().Be(0);
        (await db.AccessAuditEvents
            .Where(eventItem => eventItem.TargetUserId == adminId || eventItem.TargetUserId == editorId)
            .Select(eventItem => eventItem.ActionType)
            .ToListAsync())
            .Should().BeEquivalentTo(
                [AccessAuditActionType.LegacyRoleRemoved, AccessAuditActionType.LegacyRoleRemoved]);

        await fixture.MigrateToAsync(db, "RemoveLegacyAdminEditorRoles");

        (await db.AccessRoles.AsNoTracking().Select(role => role.Name).ToListAsync())
            .Should().Equal(RoleNames.Owner);
    }

    [Fact]
    public async Task LegacyRoleConvertCommand_WithAReadyOwner_ConvertsFormerAdminAndEditor()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToAsync(db, "RequireNormalizedEmail");
        await InsertRoleBoundUserAsync(db, "legacy-owner", "legacy-owner@example.test", 1, UserStatus.Active);
        var adminId = await InsertRoleBoundUserAsync(db, "legacy-admin", "legacy-admin@example.test", 2, UserStatus.Active);
        var editorId = await InsertRoleBoundUserAsync(db, "legacy-editor", "legacy-editor@example.test", 3, UserStatus.Active);
        await using var managementApi = await LogtoManagementApiStub.StartAsync("legacy-owner@example.test");

        var run = await AccessAdminInProcess.RunAsync(
            database.ConnectionString,
            new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["OwnerBootstrap__Emails__0"] = "legacy-owner@example.test",
                ["Auth__ManagementApi__Endpoint"] = managementApi.Endpoint.AbsoluteUri,
                ["Auth__ManagementApi__Resource"] = "https://tenant.example/api",
                ["Auth__ManagementApi__AppId"] = "test-app-id",
                ["Auth__ManagementApi__AppSecret"] = "test-app-secret",
            },
            "legacy-roles",
            "convert",
            "--apply");

        run.ExitCode.Should().Be(0);
        run.Output.Should().Contain("legacy_role_applied=true");
        db.ChangeTracker.Clear();
        var formerRoleIds = await db.AccessUsers
            .Where(user => user.Id == adminId || user.Id == editorId)
            .OrderBy(user => user.Id)
            .Select(user => user.RoleId)
            .ToListAsync();
        formerRoleIds.Should().HaveCount(2);
        formerRoleIds.Should().OnlyContain(roleId => roleId == null);
        (await db.AccessUserPermissions
            .CountAsync(grant => grant.UserId == adminId || grant.UserId == editorId))
            .Should().Be(0);
        (await db.AccessAuditEvents
            .Where(eventItem => eventItem.TargetUserId == adminId || eventItem.TargetUserId == editorId)
            .Select(eventItem => eventItem.ActionType)
            .ToListAsync())
            .Should().BeEquivalentTo(
                [AccessAuditActionType.LegacyRoleRemoved, AccessAuditActionType.LegacyRoleRemoved]);
    }

    [Fact]
    public async Task LegacyRoleConversion_RefusesToConvertAFormerRoleUserWithDirectGrants()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToAsync(db, "RequireNormalizedEmail");
        var ownerId = await InsertRoleBoundUserAsync(db, "legacy-owner", "legacy-owner@example.test", 1, UserStatus.Active);
        var adminId = await InsertRoleBoundUserAsync(db, "legacy-admin", "legacy-admin@example.test", 2, UserStatus.Active);
        var permission = new Permission("legacy.roles.audit", "فحص الدور", "Legacy role conversion test", 1);
        db.AccessPermissions.Add(permission);
        await db.SaveChangesAsync();
        db.AccessUserPermissions.Add(new UserPermission
        {
            UserId = adminId,
            PermissionId = permission.Id,
            GrantedByUserId = ownerId,
            GrantedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        var conversion = CreateLegacyRoleConversionService(db, "legacy-owner@example.test");

        var result = await conversion.ConvertAsync(CancellationToken.None);

        result.Applied.Should().BeFalse();
        result.CanConvert.Should().BeFalse();
        result.PreflightViolations.Should().Contain($"legacy_role_grant_user_ids={adminId}");
        db.ChangeTracker.Clear();
        (await db.AccessUsers.Where(user => user.Id == adminId).Select(user => user.RoleId).SingleAsync())
            .Should().Be(2);
        (await db.AccessUserPermissions.CountAsync(grant => grant.UserId == adminId)).Should().Be(1);
    }

    [Fact]
    public async Task CleanupMigration_RejectsAnyRemainingAdminOrEditorReference()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToAsync(db, "RequireNormalizedEmail");
        var adminId = await InsertRoleBoundUserAsync(db, "legacy-admin", "legacy-admin@example.test", 2, UserStatus.Active);

        var preflight = await AccessAdminInProcess.RunAsync(database.ConnectionString, "authorization", "preflight");
        var convert = await AccessAdminInProcess.RunAsync(
            database.ConnectionString,
            "legacy-roles",
            "convert",
            "--apply",
            "--confirm-production");

        preflight.ExitCode.Should().Be(3);
        preflight.Output.Should().Contain("legacy_role_admin_users=1");
        preflight.Output.Should().Contain("legacy_role_can_convert=false");
        convert.ExitCode.Should().Be(3);
        convert.Output.Should().Contain("legacy_role_admin_users=1");
        convert.Output.Should().Contain("legacy_role_applied=false");

        var applyCleanup = () => fixture.MigrateToAsync(db, "RemoveLegacyAdminEditorRoles");

        var exception = await applyCleanup.Should().ThrowAsync<PostgresException>();
        exception.Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        db.ChangeTracker.Clear();
        (await db.AccessUsers.Where(user => user.Id == adminId).Select(user => user.RoleId).SingleAsync())
            .Should().Be(2);
        (await db.AccessRoles.Where(role => role.Id == 2).Select(role => role.Name).SingleAsync())
            .Should().Be("Admin");
    }

    [Fact]
    public async Task StagedMigrationSchemas_KeepOneCasesHistoryAndTablesOutOfAnother()
    {
        await using var migratedCase = await fixture.CreateDatabaseAsync();
        await using var untouchedCase = await fixture.CreateDatabaseAsync();
        await using var db = migratedCase.CreateDbContext();
        await fixture.MigrateToAsync(db, "RequireAbwabDoorSection");

        (await ScalarAsync(migratedCase.ConnectionString, "SELECT current_schema();"))
            .Should().NotBe(await ScalarAsync(untouchedCase.ConnectionString, "SELECT current_schema();"));
        (await ScalarAsync(migratedCase.ConnectionString, "SELECT count(*) FROM \"__EFMigrationsHistory\";"))
            .Should().BeOfType<long>().Which.Should().BeGreaterThan(0);
        (await ScalarAsync(migratedCase.ConnectionString, "SELECT to_regclass('users') IS NOT NULL;"))
            .Should().Be(true);
        (await ScalarAsync(untouchedCase.ConnectionString, "SELECT to_regclass('\"__EFMigrationsHistory\"') IS NULL;"))
            .Should().Be(true);
        (await ScalarAsync(untouchedCase.ConnectionString, "SELECT to_regclass('users') IS NULL;"))
            .Should().Be(true);
        (await ScalarAsync(untouchedCase.ConnectionString, "SELECT to_regclass('public.users') IS NULL;"))
            .Should().Be(true);
    }

    [Fact]
    public async Task StagedMigrationCases_ReceiveAnEmptyDatabase_NotTheMigratedHeadTemplate()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var caseSchema = (string)(await ScalarAsync(database.ConnectionString, "SELECT current_schema();"))!;

        (await ScalarAsync(
                fixture.ConnectionString,
                "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NULL;"))
            .Should().Be(true);
        (await CountRelationsAsync(fixture.ConnectionString, "public")).Should().Be(0);
        (await CountRelationsAsync(fixture.ConnectionString, caseSchema)).Should().Be(0);
    }

    private static async Task InsertLegacyUserAsync(
        QuranDashboardDbContext db,
        string logtoSub,
        string email)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO users (logto_sub, email, status, created_at, updated_at)
            VALUES ({logtoSub}, {email}, {(int)UserStatus.Pending}, {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow});
            """);
    }

    private static async Task<int> InsertRoleBoundUserAsync(
        QuranDashboardDbContext db,
        string logtoSub,
        string email,
        int roleId,
        UserStatus status)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO users (logto_sub, email, normalized_email, role_id, status, created_at, updated_at)
            VALUES ({logtoSub}, {email}, {email.ToUpperInvariant()}, {roleId}, {(int)status}, {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow});
            """);
        return await db.AccessUsers
            .Where(user => user.LogtoSub == logtoSub)
            .Select(user => user.Id)
            .SingleAsync();
    }

    private static ILegacyRoleConversionService CreateLegacyRoleConversionService(
        QuranDashboardDbContext db,
        string ownerEmail)
    {
        var ownerReconciliation = new OwnerReconciliationService(
            new OwnerReconciliationStore(db),
            new StaticOwnerBootstrapConfigurationSource(new OwnerBootstrapConfiguration(
                new HashSet<string>(StringComparer.Ordinal) { ownerEmail.ToUpperInvariant() },
                "legacy-role-conversion-test")),
            new FakeExternalUserProfileSource(),
            new EmailIdentityNormalizer());
        return new LegacyRoleConversionService(
            new LegacyRoleConversionStore(db),
            ownerReconciliation);
    }

    private sealed class StaticOwnerBootstrapConfigurationSource(
        OwnerBootstrapConfiguration configuration) : IOwnerBootstrapConfigurationSource
    {
        public OwnerBootstrapConfiguration GetCurrent() => configuration;
    }

    private static async Task AssertNormalizedEmailConstraintAsync(QuranDashboardDbContext db)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync();
        }

        await using var nullableCommand = new NpgsqlCommand("""
            SELECT is_nullable
            FROM information_schema.columns
            WHERE table_schema = current_schema()
              AND table_name = 'users'
              AND column_name = 'normalized_email';
            """, connection);

        (await nullableCommand.ExecuteScalarAsync()).Should().Be("NO");
        if (shouldCloseConnection)
        {
            await db.Database.CloseConnectionAsync();
        }

        (await HasNormalizedEmailUniqueIndexAsync(db)).Should().BeTrue();
    }

    private static async Task<IReadOnlyList<string>> ReadNormalizedEmailsAsync(QuranDashboardDbContext db)
    {
        return await db.AccessUsers.AsNoTracking()
            .OrderBy(user => user.Id)
            .Select(user => user.NormalizedEmail)
            .ToListAsync();
    }

    private static async Task<bool> HasNormalizedEmailUniqueIndexAsync(QuranDashboardDbContext db)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync();
        }

        await using var command = new NpgsqlCommand("""
            SELECT index.indisunique
            FROM pg_index index
            JOIN pg_class index_relation ON index_relation.oid = index.indexrelid
            WHERE index_relation.relname = 'IX_users_normalized_email';
            """, connection);
        var isUnique = await command.ExecuteScalarAsync() is true;
        if (shouldCloseConnection)
        {
            await db.Database.CloseConnectionAsync();
        }

        return isUnique;
    }

    private static async Task<IReadOnlyList<string>> ReadLegacyEmailsAsync(QuranDashboardDbContext db)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync();
        }

        var emails = new List<string>();

        await using (var command = new NpgsqlCommand("SELECT email FROM users ORDER BY id;", connection))
        {
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                emails.Add(reader.GetString(0));
            }
        }

        if (shouldCloseConnection)
        {
            await db.Database.CloseConnectionAsync();
        }

        return emails;
    }

    private static async Task<long> CountRelationsAsync(string connectionString, string schemaName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT count(*)
            FROM pg_class relation
            JOIN pg_namespace relation_namespace ON relation_namespace.oid = relation.relnamespace
            WHERE relation_namespace.nspname = @schema;
            """, connection);
        command.Parameters.AddWithValue("schema", schemaName);

        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<object?> ScalarAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        return await command.ExecuteScalarAsync();
    }

    private static async Task<bool> NormalizedEmailColumnExistsAsync(QuranDashboardDbContext db)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync();
        }

        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = current_schema()
                  AND table_name = 'users'
                  AND column_name = 'normalized_email');
            """, connection);
        var exists = (bool)(await command.ExecuteScalarAsync())!;
        if (shouldCloseConnection)
        {
            await db.Database.CloseConnectionAsync();
        }

        return exists;
    }

    private sealed class LogtoManagementApiStub(WebApplication application, Uri endpoint) : IAsyncDisposable
    {
        public Uri Endpoint { get; } = endpoint;

        public static async Task<LogtoManagementApiStub> StartAsync(string ownerEmail)
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
            var application = builder.Build();
            application.MapPost("/oidc/token", () => new { access_token = "test-token", expires_in = 300 });
            application.MapGet("/api/users/{logtoSub}", () => new
            {
                primaryEmail = ownerEmail,
                username = "owner",
                name = "Owner",
            });
            await application.StartAsync();

            return new LogtoManagementApiStub(application, new Uri(application.Urls.Single()));
        }

        public async ValueTask DisposeAsync()
        {
            await application.StopAsync();
            await application.DisposeAsync();
        }
    }
}
