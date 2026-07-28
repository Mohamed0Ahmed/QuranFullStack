using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Tests.Abwab;

[Collection(nameof(AbwabSchemaTestCollection))]
public sealed class AbwabSchemaTests(AbwabSchemaFixture fixture)
{
    private const string ColumnsOfTableSql = """
        SELECT column_name
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = @tableName
        ORDER BY column_name
        """;

    private const string IndexShapeSql = """
        SELECT
            ic.relname AS index_name,
            ix.indisunique,
            ARRAY(
                SELECT a.attname
                FROM unnest(ix.indkey) WITH ORDINALITY AS u(attnum, ord)
                JOIN pg_attribute a ON a.attrelid = ix.indrelid AND a.attnum = u.attnum
                ORDER BY u.ord
            ) AS column_names
        FROM pg_class tc
        JOIN pg_namespace tn ON tn.oid = tc.relnamespace
        JOIN pg_index ix ON ix.indrelid = tc.oid
        JOIN pg_class ic ON ic.oid = ix.indexrelid
        WHERE tn.nspname = 'public'
          AND tc.relname = @tableName
          AND ic.relname = @indexName
        """;

    private static readonly string[] ExpectedAuditSeedColumns =
    [
        "created_at", "created_by",
        "updated_at", "updated_by",
        "approved_at", "approved_by",
        "deleted_at", "deleted_by"
    ];

    private static readonly string[] ExpectedSectionColumns =
    [
        "id", "name", "order_value", .. ExpectedAuditSeedColumns
    ];

    private static readonly string[] ExpectedDoorColumns =
    [
        "id", "section_id", "parent_id", "name", "description", "representative_ayah_text",
        "order_value", .. ExpectedAuditSeedColumns
    ];

    private static readonly string[] ExpectedDoorAliasColumns =
    [
        "id", "door_id", "value", .. ExpectedAuditSeedColumns
    ];

    [Fact]
    public async Task Sections_table_has_the_expected_columns()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        var columns = await QueryColumnsAsync(connection, "abwab_sections");
        columns.Should().Contain(ExpectedSectionColumns);

        // xmin is Postgres's own hidden system column, never a migration-created one — proven
        // separately by the concurrency test below, not by its presence in information_schema.
        columns.Should().NotContain("version");
    }

    [Fact]
    public async Task Doors_table_has_the_expected_columns()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        var columns = await QueryColumnsAsync(connection, "abwab_doors");
        columns.Should().Contain(ExpectedDoorColumns);
        columns.Should().NotContain("version");
    }

    [Fact]
    public async Task Door_aliases_table_has_the_expected_columns()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        var columns = await QueryColumnsAsync(connection, "abwab_door_aliases");
        columns.Should().Contain(ExpectedDoorAliasColumns);
    }

    [Fact]
    public async Task Doors_table_has_the_four_required_indexes()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        await AssertIndexAsync(connection, "abwab_doors", "IX_abwab_doors_section_id_parent_id_order_value",
            isUnique: false, ["section_id", "parent_id", "order_value"]);
        await AssertIndexAsync(connection, "abwab_doors", "IX_abwab_doors_parent_id",
            isUnique: false, ["parent_id"]);
        await AssertIndexAsync(connection, "abwab_doors", "IX_abwab_doors_deleted_at",
            isUnique: false, ["deleted_at"]);
        await AssertIndexAsync(connection, "abwab_doors", "IX_abwab_doors_section_id_parent_id_name",
            isUnique: true, ["section_id", "parent_id", "name"]);
    }

    [Fact]
    public async Task Root_doors_with_the_same_name_are_rejected_by_the_unique_index()
    {
        // The mandatory NULLS NOT DISTINCT proof (plan §T102): a naive UNIQUE(parent_id, name)
        // would let two NULL-parent (root) doors share a name, since Postgres NULLs never
        // collide in a unique index by default.
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        dbContext.AbwabDoors.Add(new AbwabDoor
        {
            Name = "root-collision-door",
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await dbContext.SaveChangesAsync();

        dbContext.AbwabDoors.Add(new AbwabDoor
        {
            Name = "root-collision-door",
            OrderValue = 2,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        var act = async () => await dbContext.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Soft_deleted_door_does_not_block_a_new_door_with_the_same_name()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var archived = new AbwabDoor
        {
            Name = "soft-delete-filtered-door",
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        dbContext.AbwabDoors.Add(archived);
        await dbContext.SaveChangesAsync();

        archived.DeletedAtUtc = now;
        archived.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync();

        dbContext.AbwabDoors.Add(new AbwabDoor
        {
            Name = "soft-delete-filtered-door",
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        var act = async () => await dbContext.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Stale_door_version_token_raises_concurrency_exception_on_update()
    {
        await using var writerScope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writerContext = writerScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var door = new AbwabDoor
        {
            Name = "concurrency-door",
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        writerContext.AbwabDoors.Add(door);
        await writerContext.SaveChangesAsync();

        await using var staleScope = fixture.CreateServiceProvider().CreateAsyncScope();
        var staleContext = staleScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var staleDoor = await staleContext.AbwabDoors.SingleAsync(d => d.Id == door.Id);

        door.Name = "concurrency-door-renamed";
        door.UpdatedAtUtc = now;
        await writerContext.SaveChangesAsync();

        staleDoor.Name = "concurrency-door-conflicting-rename";
        staleDoor.UpdatedAtUtc = now;

        var act = async () => await staleContext.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task Stale_section_version_token_raises_concurrency_exception_on_update()
    {
        await using var writerScope = fixture.CreateServiceProvider().CreateAsyncScope();
        var writerContext = writerScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var section = new AbwabSection
        {
            Name = "concurrency-section",
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        writerContext.AbwabSections.Add(section);
        await writerContext.SaveChangesAsync();

        await using var staleScope = fixture.CreateServiceProvider().CreateAsyncScope();
        var staleContext = staleScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var staleSection = await staleContext.AbwabSections.SingleAsync(s => s.Id == section.Id);

        section.Name = "concurrency-section-renamed";
        section.UpdatedAtUtc = now;
        await writerContext.SaveChangesAsync();

        staleSection.Name = "concurrency-section-conflicting-rename";
        staleSection.UpdatedAtUtc = now;

        var act = async () => await staleContext.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<QuranDashboardDbContext>();
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return (NpgsqlConnection)connection;
    }

    private static async Task<IReadOnlyList<string>> QueryColumnsAsync(NpgsqlConnection connection, string tableName)
    {
        await using var command = new NpgsqlCommand(ColumnsOfTableSql, connection);
        command.Parameters.AddWithValue("tableName", tableName);
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private static async Task AssertIndexAsync(
        NpgsqlConnection connection,
        string tableName,
        string indexName,
        bool isUnique,
        string[] columnNames)
    {
        await using var command = new NpgsqlCommand(IndexShapeSql, connection);
        command.Parameters.AddWithValue("tableName", tableName);
        command.Parameters.AddWithValue("indexName", indexName);

        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue($"index {indexName} on {tableName} should exist");
        reader.GetBoolean(1).Should().Be(isUnique, $"index {indexName} uniqueness");
        reader.GetFieldValue<string[]>(2).Should().Equal(columnNames, $"index {indexName} columns");
    }
}
