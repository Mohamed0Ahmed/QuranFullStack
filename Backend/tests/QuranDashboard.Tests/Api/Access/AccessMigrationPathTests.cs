using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;
using AccessAdminProgram = QuranDashboard.AccessAdmin.Program;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessMigrationCollection))]
public sealed class AccessMigrationPathTests(AccessMigrationTestFixture fixture)
{
    public static IEnumerable<object[]> SchemaMutations =>
    [
        ["ALTER TABLE users ALTER COLUMN normalized_email DROP NOT NULL;", "invalid_nullability=users.normalized_email"],
        ["DROP INDEX \"IX_users_normalized_email\";", "missing_index=users.IX_users_normalized_email"],
        ["ALTER TABLE permissions DROP CONSTRAINT ck_permissions_code_format;", "missing_constraint=permissions.ck_permissions_code_format"],
        ["ALTER TABLE user_permissions DROP CONSTRAINT \"FK_user_permissions_users_user_id\";", "missing_constraint=user_permissions.FK_user_permissions_users_user_id"],
        ["DROP TABLE access_audit_events;", "missing_table=access_audit_events"],
        [
            """
            ALTER TABLE permissions DROP CONSTRAINT ck_permissions_code_format;
            ALTER TABLE permissions ADD CONSTRAINT ck_permissions_code_format CHECK (true);
            """,
            "invalid_constraint=permissions.ck_permissions_code_format"
        ],
        [
            """
            ALTER TABLE access_audit_events DROP CONSTRAINT ck_access_audit_events_metadata_schema_version;
            ALTER TABLE access_audit_events ADD CONSTRAINT ck_access_audit_events_metadata_schema_version CHECK (true);
            """,
            "invalid_constraint=access_audit_events.ck_access_audit_events_metadata_schema_version"
        ],
        [
            """
            ALTER TABLE user_permissions DROP CONSTRAINT "FK_user_permissions_users_user_id";
            ALTER TABLE user_permissions ADD CONSTRAINT "FK_user_permissions_users_user_id"
                FOREIGN KEY (granted_by_user_id) REFERENCES users(id) ON DELETE RESTRICT;
            """,
            "invalid_constraint=user_permissions.FK_user_permissions_users_user_id"
        ],
        [
            """
            DROP INDEX "IX_permissions_code";
            CREATE INDEX "IX_permissions_code" ON permissions USING btree (code);
            """,
            "invalid_index=permissions.IX_permissions_code"
        ],
        [
            """
            DROP INDEX "IX_access_audit_events_occurred_at_id";
            CREATE INDEX "IX_access_audit_events_occurred_at_id"
                ON access_audit_events USING btree (occurred_at, id);
            """,
            "invalid_index=access_audit_events.IX_access_audit_events_occurred_at_id"
        ],
        [
            "ALTER TABLE permissions ALTER COLUMN display_order TYPE bigint;",
            "invalid_column_type=permissions.display_order"
        ],
        [
            "ALTER TABLE permissions ALTER COLUMN code TYPE character varying(256);",
            "invalid_column_type=permissions.code"
        ],
        [
            "ALTER TABLE access_audit_events ALTER COLUMN reason TYPE text;",
            "invalid_column_type=access_audit_events.reason"
        ],
        [
            "ALTER TABLE access_audit_events ALTER COLUMN occurred_at TYPE timestamp without time zone;",
            "invalid_column_type=access_audit_events.occurred_at"
        ],
        [
            "ALTER TABLE permissions ALTER COLUMN id DROP IDENTITY;",
            "invalid_column_identity=permissions.id"
        ],
    ];

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

        var run = await RunAccessAdminAsync(database.ConnectionString, "identity", "scan");

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

    [Theory]
    [MemberData(nameof(SchemaMutations))]
    public async Task AuthorizationPreflight_RejectsLiveSchemaDrift(
        string schemaMutation,
        string expectedViolation)
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToHeadAsync(db);
        (await RunAccessAdminAsync(database.ConnectionString, "catalogue", "sync")).ExitCode.Should().Be(0);
        await db.Database.ExecuteSqlRawAsync(schemaMutation);

        var run = await RunAccessAdminAsync(database.ConnectionString, "authorization", "preflight");

        run.ExitCode.Should().Be(3);
        run.Output.Should().Contain(expectedViolation);
    }

    [Fact]
    public async Task AuthorizationPreflight_AcceptsAFreshlyMigratedAndSynchronizedSchema()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToHeadAsync(db);
        (await RunAccessAdminAsync(database.ConnectionString, "catalogue", "sync")).ExitCode.Should().Be(0);

        var run = await RunAccessAdminAsync(database.ConnectionString, "authorization", "preflight");

        run.ExitCode.Should().Be(0);
        run.Output.Should().Contain($"schema_violations={Environment.NewLine}");
    }

    [Fact]
    public async Task AuthorizationPreflight_RejectsARetiredCanonicalPermission()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToHeadAsync(db);
        (await RunAccessAdminAsync(database.ConnectionString, "catalogue", "sync")).ExitCode.Should().Be(0);
        var retiredCode = AbwabPermissionCatalogue.All[0].Code;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE permissions SET retired_at = now() WHERE code = {retiredCode};");

        var run = await RunAccessAdminAsync(database.ConnectionString, "authorization", "preflight");

        run.ExitCode.Should().Be(3);
        run.Output.Should().Contain($"catalogue_retired={retiredCode}");
    }

    [Fact]
    public async Task CatalogueSync_ReportsARetiredCanonicalPermissionWithoutReactivatingIt()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();
        await fixture.MigrateToHeadAsync(db);
        (await RunAccessAdminAsync(database.ConnectionString, "catalogue", "sync")).ExitCode.Should().Be(0);
        var retiredCode = AbwabPermissionCatalogue.All[0].Code;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE permissions SET retired_at = now() WHERE code = {retiredCode};");

        var run = await RunAccessAdminAsync(database.ConnectionString, "catalogue", "sync");

        run.ExitCode.Should().Be(3);
        run.Output.Should().Contain($"catalogue_retired={retiredCode}");
        (await ReadRetiredCodesAsync(db)).Should().Equal(retiredCode);
    }

    [Fact]
    public async Task AuthorizationPreflight_UnreachableDatabase_ReportsAControlledOperationalFailure()
    {
        var run = await RunAccessAdminAsync(
            "Host=127.0.0.1;Port=1;Database=absent;Username=absent;Password=absent;Timeout=2",
            "authorization",
            "preflight");

        run.ExitCode.Should().Be(4);
        run.Output.Should().Contain("access_admin_failure=");
        run.Output.Should().NotContain("   at ");
    }

    internal static async Task<AccessAdminRun> RunAccessAdminAsync(
        string connectionString,
        params string[] args)
    {
        const string connectionStringKey = "ConnectionStrings__QuranDashboardDb";
        var originalConnectionString = Environment.GetEnvironmentVariable(connectionStringKey);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var captured = new StringWriter();
        Environment.SetEnvironmentVariable(connectionStringKey, connectionString);
        Console.SetOut(captured);
        Console.SetError(captured);

        try
        {
            return new AccessAdminRun(await AccessAdminProgram.Main(args), captured.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Environment.SetEnvironmentVariable(connectionStringKey, originalConnectionString);
        }
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

    private static async Task<IReadOnlyList<string>> ReadRetiredCodesAsync(QuranDashboardDbContext db)
    {
        return await db.AccessPermissions.AsNoTracking()
            .Where(permission => permission.RetiredAtUtc != null)
            .OrderBy(permission => permission.Code)
            .Select(permission => permission.Code)
            .ToListAsync();
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
}
