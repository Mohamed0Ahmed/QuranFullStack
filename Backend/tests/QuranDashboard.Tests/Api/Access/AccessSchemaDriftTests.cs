using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessProcessGlobalCollection))]
public sealed class AccessSchemaDriftTests
{
    public static IEnumerable<object[]> SchemaMutations =>
    [
        ["DROP TABLE roles CASCADE;", "missing_table=roles"],
        ["ALTER TABLE users DROP COLUMN logto_sub;", "missing_column=users.logto_sub"],
        ["ALTER TABLE users DROP COLUMN role_id;", "missing_column=users.role_id"],
        [
            """
            DROP INDEX "IX_users_logto_sub";
            CREATE INDEX "IX_users_logto_sub" ON users USING btree (logto_sub);
            """,
            "invalid_index=users.IX_users_logto_sub"
        ],
        [
            """
            ALTER TABLE users DROP CONSTRAINT "FK_users_roles_role_id";
            ALTER TABLE users ADD CONSTRAINT "FK_users_roles_role_id"
                FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE;
            """,
            "invalid_constraint=users.FK_users_roles_role_id"
        ],
        ["ALTER TABLE users ALTER COLUMN normalized_email DROP NOT NULL;", "invalid_nullability=users.normalized_email"],
        ["DROP INDEX \"IX_users_normalized_email\";", "missing_index=users.IX_users_normalized_email"],
        ["ALTER TABLE permissions DROP CONSTRAINT ck_permissions_code_format;", "missing_constraint=permissions.ck_permissions_code_format"],
        ["ALTER TABLE user_permissions DROP CONSTRAINT \"FK_user_permissions_users_user_id\";", "missing_constraint=user_permissions.FK_user_permissions_users_user_id"],
        ["DROP TABLE access_audit_events;", "missing_table=access_audit_events"],
        ["DROP TABLE user_device_sessions;", "missing_table=user_device_sessions"],
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

    [Theory]
    [MemberData(nameof(SchemaMutations))]
    public async Task AuthorizationPreflight_RejectsLiveSchemaDrift(
        string schemaMutation,
        string expectedViolation)
    {
        await using var database = await LeaseMigratedHeadDatabaseAsync();
        await using var db = CreateDbContext(database.ConnectionString);
        (await AccessAdminInProcess.RunAsync(database.ConnectionString, "catalogue", "sync"))
            .ExitCode.Should().Be(0);
        await db.Database.ExecuteSqlRawAsync(schemaMutation);

        var run = await AccessAdminInProcess.RunAsync(
            database.ConnectionString,
            "authorization",
            "preflight");

        run.ExitCode.Should().Be(3);
        run.Output.Should().Contain(expectedViolation);
    }

    [Fact]
    public async Task AuthorizationPreflight_RejectsAnOtherwiseCleanSchemaWithoutResolvedOwnerConfiguration()
    {
        await using var database = await LeaseMigratedHeadDatabaseAsync();
        (await AccessAdminInProcess.RunAsync(database.ConnectionString, "catalogue", "sync"))
            .ExitCode.Should().Be(0);

        var run = await AccessAdminInProcess.RunAsync(
            database.ConnectionString,
            "authorization",
            "preflight");

        run.ExitCode.Should().Be(3);
        run.Output.Should().Contain($"schema_violations={Environment.NewLine}");
        run.Output.Should().Contain("owner_awaiting_verified_sign_in=1");
    }

    [Fact]
    public async Task AuthorizationPreflight_RejectsARetiredCanonicalPermission()
    {
        await using var database = await LeaseMigratedHeadDatabaseAsync();
        await using var db = CreateDbContext(database.ConnectionString);
        (await AccessAdminInProcess.RunAsync(database.ConnectionString, "catalogue", "sync"))
            .ExitCode.Should().Be(0);
        var retiredCode = AbwabPermissionCatalogue.All[0].Code;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE permissions SET retired_at = now() WHERE code = {retiredCode};");

        var run = await AccessAdminInProcess.RunAsync(
            database.ConnectionString,
            "authorization",
            "preflight");

        run.ExitCode.Should().Be(3);
        run.Output.Should().Contain($"catalogue_retired={retiredCode}");
    }

    [Fact]
    public async Task CatalogueSync_ReportsARetiredCanonicalPermissionWithoutReactivatingIt()
    {
        await using var database = await LeaseMigratedHeadDatabaseAsync();
        await using var db = CreateDbContext(database.ConnectionString);
        (await AccessAdminInProcess.RunAsync(database.ConnectionString, "catalogue", "sync"))
            .ExitCode.Should().Be(0);
        var retiredCode = AbwabPermissionCatalogue.All[0].Code;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE permissions SET retired_at = now() WHERE code = {retiredCode};");

        var run = await AccessAdminInProcess.RunAsync(database.ConnectionString, "catalogue", "sync");

        run.ExitCode.Should().Be(3);
        run.Output.Should().Contain($"catalogue_retired={retiredCode}");
        (await ReadRetiredCodesAsync(db)).Should().Equal(retiredCode);
    }

    private static Task<PostgreSqlDatabaseLease> LeaseMigratedHeadDatabaseAsync()
    {
        return PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(nameof(AccessSchemaDriftTests));
    }

    private static QuranDashboardDbContext CreateDbContext(string connectionString)
    {
        return new QuranDashboardDbContext(
            new DbContextOptionsBuilder<QuranDashboardDbContext>()
                .UseNpgsql(connectionString)
                .Options);
    }

    private static async Task<IReadOnlyList<string>> ReadRetiredCodesAsync(QuranDashboardDbContext db)
    {
        return await db.AccessPermissions.AsNoTracking()
            .Where(permission => permission.RetiredAtUtc != null)
            .OrderBy(permission => permission.Code)
            .Select(permission => permission.Code)
            .ToListAsync();
    }
}
