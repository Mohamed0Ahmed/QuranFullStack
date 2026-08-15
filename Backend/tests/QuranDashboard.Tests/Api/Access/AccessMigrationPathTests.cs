namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessProcessGlobalCollection))]
public sealed class AccessMigrationPathTests(AccessMigrationTestFixture fixture)
{
    [Fact]
    public async Task StagedMigrationCases_ReceiveAnEmptyDatabase_NotTheMigratedHeadTemplate()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        await using var db = database.CreateDbContext();

        (await RelationExistsAsync(database.ConnectionString, "__EFMigrationsHistory"))
            .Should().BeFalse();

        await db.Database.MigrateAsync();

        (await db.Database.GetAppliedMigrationsAsync())
            .Should().ContainSingle(migration => migration.EndsWith("_InitialBaseline"));
        (await db.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await RelationExistsAsync(database.ConnectionString, "users")).Should().BeTrue();
        (await RelationExistsAsync(database.ConnectionString, "user_device_sessions")).Should().BeTrue();
        (await RelationExistsAsync(database.ConnectionString, "linking_door_ayahs")).Should().BeTrue();
        (await RelationExistsAsync(database.ConnectionString, "linking_door_ayah_words")).Should().BeTrue();
        (await IndexIsUniqueAsync(
                database.ConnectionString,
                "IX_linking_door_ayahs_door_id_ayah_id"))
            .Should().BeTrue();
    }

    private static async Task<bool> RelationExistsAsync(
        string connectionString,
        string relationName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT to_regclass(@relation_name) IS NOT NULL;",
            connection);
        command.Parameters.AddWithValue("relation_name", relationName);

        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<bool> IndexIsUniqueAsync(
        string connectionString,
        string indexName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT indexdef LIKE 'CREATE UNIQUE INDEX%'
            FROM pg_indexes
            WHERE schemaname = current_schema()
              AND indexname = @index_name;
            """,
            connection);
        command.Parameters.AddWithValue("index_name", indexName);

        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
