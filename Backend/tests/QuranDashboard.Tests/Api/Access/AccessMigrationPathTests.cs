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
}
