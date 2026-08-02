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

    private const string ColumnNullabilitySql = """
        SELECT is_nullable
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = @tableName
          AND column_name = @columnName
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

    private const string IndexFilterSql = """
        SELECT pg_get_expr(ix.indpred, ix.indrelid)
        FROM pg_class tc
        JOIN pg_namespace tn ON tn.oid = tc.relnamespace
        JOIN pg_index ix ON ix.indrelid = tc.oid
        JOIN pg_class ic ON ic.oid = ix.indexrelid
        WHERE tn.nspname = 'public'
          AND tc.relname = @tableName
          AND ic.relname = @indexName
        """;

    private const string IndexesOnColumnSql = """
        SELECT DISTINCT ix.indisunique
        FROM pg_class tc
        JOIN pg_namespace tn ON tn.oid = tc.relnamespace
        JOIN pg_index ix ON ix.indrelid = tc.oid
        JOIN pg_attribute a ON a.attrelid = tc.oid AND a.attnum = ANY(ix.indkey)
        WHERE tn.nspname = 'public'
          AND tc.relname = @tableName
          AND a.attname = @columnName
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
        "order_value", "global_order_value", .. ExpectedAuditSeedColumns
    ];

    private static readonly string[] ExpectedDoorAliasColumns =
    [
        "id", "door_id", "value", .. ExpectedAuditSeedColumns
    ];

    [Fact]
    public async Task Sections_table_has_exactly_the_expected_columns()
    {
        // Exact-set, not a subset check: T105's proof is the ABSENCE of a concurrency-token
        // column (Version/xmin is never a real column — see AbwabSectionConfiguration), and a
        // subset assertion would not fail if one leaked in.
        await using var scope = fixture.Services.CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        var columns = await QueryColumnsAsync(connection, "abwab_sections");
        columns.Should().BeEquivalentTo(ExpectedSectionColumns);
    }

    [Fact]
    public async Task Doors_table_has_exactly_the_expected_columns()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        var columns = await QueryColumnsAsync(connection, "abwab_doors");
        columns.Should().BeEquivalentTo(ExpectedDoorColumns);
    }

    [Fact]
    public async Task Door_aliases_table_has_exactly_the_expected_columns()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        var columns = await QueryColumnsAsync(connection, "abwab_door_aliases");
        columns.Should().BeEquivalentTo(ExpectedDoorAliasColumns);
    }

    [Fact]
    public async Task Doors_table_has_the_five_required_indexes()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        await AssertIndexAsync(connection, "abwab_doors", "IX_abwab_doors_section_id_parent_id_order_value",
            isUnique: false, ["section_id", "parent_id", "order_value"]);
        await AssertIndexAsync(connection, "abwab_doors", "IX_abwab_doors_parent_id",
            isUnique: false, ["parent_id"]);
        await AssertIndexAsync(connection, "abwab_doors", "IX_abwab_doors_deleted_at",
            isUnique: false, ["deleted_at"]);
        await AssertIndexAsync(connection, "abwab_doors", "IX_abwab_doors_section_id_parent_id_name",
            isUnique: true, ["section_id", "parent_id", "name"]);
        await AssertIndexAsync(connection, "abwab_doors", "IX_abwab_doors_global_order_value",
            isUnique: false, ["global_order_value"]);
    }

    [Fact]
    public async Task Global_order_value_index_is_partial_to_live_root_doors()
    {
        // Without this, deleting .HasFilter(...) from AbwabDoorConfiguration would still pass
        // Doors_table_has_the_five_required_indexes — that test checks name/uniqueness/columns,
        // never the predicate the index exists to enforce.
        await using var scope = fixture.Services.CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        await using var command = new NpgsqlCommand(IndexFilterSql, connection);
        command.Parameters.AddWithValue("tableName", "abwab_doors");
        command.Parameters.AddWithValue("indexName", "IX_abwab_doors_global_order_value");
        var filter = (string?)await command.ExecuteScalarAsync();

        filter.Should().Be("((parent_id IS NULL) AND (deleted_at IS NULL))");
    }

    [Fact]
    public async Task Global_order_value_has_no_unique_index()
    {
        // Positive assertion (plan §6): a Postgres unique index is checked per statement, so
        // 1..N global resequencing (one UPDATE per row) would transiently violate one and die
        // mid-transaction. This trips if a later "hardening" PR adds one back.
        await using var scope = fixture.Services.CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        var indexes = await QueryIndexesOnColumnAsync(connection, "abwab_doors", "global_order_value");
        indexes.Should().NotBeEmpty();
        indexes.Should().OnlyContain(isUnique => isUnique == false);
    }

    [Fact]
    public async Task Root_doors_with_the_same_name_are_rejected_by_the_unique_index()
    {
        // The mandatory NULLS NOT DISTINCT proof (plan §T102): a naive UNIQUE(parent_id, name)
        // would let two NULL-parent (root) doors share a name, since Postgres NULLs never
        // collide in a unique index by default.
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var sectionId = await NewSectionIdAsync(dbContext, "root-collision-section", now);
        dbContext.AbwabDoors.Add(new AbwabDoor
        {
            SectionId = sectionId,
            Name = "root-collision-door",
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await dbContext.SaveChangesAsync();

        dbContext.AbwabDoors.Add(new AbwabDoor
        {
            SectionId = sectionId,
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
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var sectionId = await NewSectionIdAsync(dbContext, "soft-delete-filtered-door-section", now);
        var archived = new AbwabDoor
        {
            SectionId = sectionId,
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
            SectionId = sectionId,
            Name = "soft-delete-filtered-door",
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        var act = async () => await dbContext.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Live_sections_with_the_same_name_are_rejected_by_the_unique_index()
    {
        // Not in T102's index list, but required by phase 2b's "409 duplicate name" outcome for
        // sections — added here since sections have no parent to scope siblings by.
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        dbContext.AbwabSections.Add(new AbwabSection
        {
            Name = "duplicate-section",
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await dbContext.SaveChangesAsync();

        dbContext.AbwabSections.Add(new AbwabSection
        {
            Name = "duplicate-section",
            OrderValue = 2,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        var act = async () => await dbContext.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Archived_section_does_not_block_a_new_section_with_the_same_name()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var archived = new AbwabSection
        {
            Name = "soft-delete-filtered-section",
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        dbContext.AbwabSections.Add(archived);
        await dbContext.SaveChangesAsync();

        archived.DeletedAtUtc = now;
        archived.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync();

        dbContext.AbwabSections.Add(new AbwabSection
        {
            Name = "soft-delete-filtered-section",
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
        await using var writerScope = fixture.Services.CreateAsyncScope();
        var writerContext = writerScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var door = new AbwabDoor
        {
            SectionId = await NewSectionIdAsync(writerContext, "concurrency-section", now),
            Name = "concurrency-door",
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        writerContext.AbwabDoors.Add(door);
        await writerContext.SaveChangesAsync();

        await using var staleScope = fixture.Services.CreateAsyncScope();
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
        await using var writerScope = fixture.Services.CreateAsyncScope();
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

        await using var staleScope = fixture.Services.CreateAsyncScope();
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

    // The DB tier of "every door belongs to a section". The write side refuses a section-less door
    // already; this is the constraint that holds when something bypasses the writer entirely.
    [Fact]
    public async Task Doors_section_id_is_not_null()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        await using var command = new NpgsqlCommand(ColumnNullabilitySql, connection);
        command.Parameters.AddWithValue("tableName", "abwab_doors");
        command.Parameters.AddWithValue("columnName", "section_id");

        (await command.ExecuteScalarAsync()).Should().Be("NO");
    }

    // Asserted through raw SQL, not the DbContext: an EF insert cannot even express a null section now,
    // so only a statement that bypasses the model proves the column itself is what refuses it.
    [Fact]
    public async Task Doors_insert_with_null_section_is_rejected_by_postgres()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO abwab_doors (section_id, parent_id, name, order_value, created_at, updated_at)
            VALUES (NULL, NULL, 'raw-null-section-door', 1, now(), now())
            """,
            connection);

        var act = async () => await command.ExecuteNonQueryAsync();

        var exception = await act.Should().ThrowAsync<PostgresException>();
        exception.Which.SqlState.Should().Be("23502", "a not-null violation, not an FK or a check");
        // Pinned to the column, not just the SQLSTATE: every other NOT NULL column on this row would
        // raise the same code, so without this the test could pass while section_id stayed nullable.
        exception.Which.ColumnName.Should().Be("section_id");
    }

    // Every door row needs a section now, so a test that inserts one directly brings its own rather than
    // depending on whatever else this shared, non-truncated fixture happens to hold.
    private static async Task<int> NewSectionIdAsync(QuranDashboardDbContext dbContext, string name, DateTimeOffset now)
    {
        var section = new AbwabSection { Name = name, OrderValue = 1, CreatedAtUtc = now, UpdatedAtUtc = now };
        dbContext.AbwabSections.Add(section);
        await dbContext.SaveChangesAsync();
        return section.Id;
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

    private static async Task<IReadOnlyList<bool>> QueryIndexesOnColumnAsync(
        NpgsqlConnection connection, string tableName, string columnName)
    {
        await using var command = new NpgsqlCommand(IndexesOnColumnSql, connection);
        command.Parameters.AddWithValue("tableName", tableName);
        command.Parameters.AddWithValue("columnName", columnName);

        var indexes = new List<bool>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetBoolean(0));
        }

        return indexes;
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
