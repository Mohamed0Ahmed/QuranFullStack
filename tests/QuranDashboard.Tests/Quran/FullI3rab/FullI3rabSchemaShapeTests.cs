using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Tests.Quran.FullI3rab;

[Collection(nameof(FullI3rabSchemaTestCollection))]
public sealed class FullI3rabSchemaShapeTests(FullI3rabSchemaFixture fixture)
{
    private const string FullI3rabTablesSql = """
        SELECT table_name
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name LIKE 'quran_full_i3rab%'
        ORDER BY table_name
        """;

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

    private const string NamedCheckConstraintExistsSql = """
        SELECT EXISTS (
            SELECT 1
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'public'
              AND t.relname = @tableName
              AND c.contype = 'c'
              AND c.conname = @constraintName
        )
        """;

    private const string ForeignKeyExistsSql = """
        SELECT EXISTS (
            SELECT 1
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = 'public'
              AND t.relname = @tableName
              AND c.contype = 'f'
              AND c.conname = @constraintName
        )
        """;

    private static readonly IndexExpectation[] SourceIndexes =
    [
        new("IX_quran_full_i3rab_sources_source_key", ["source_key"], IsUnique: true),
        new("IX_quran_full_i3rab_sources_package_file", ["package_file"], IsUnique: true)
    ];

    private static readonly IndexExpectation[] EntryIndexes =
    [
        new("IX_quran_full_i3rab_entries_source_id_source_entry_key", ["source_id", "source_entry_key"], IsUnique: true),
        new("IX_quran_full_i3rab_entries_leader_ayah_id", ["leader_ayah_id"], IsUnique: false),
        new("IX_quran_full_i3rab_entries_source_id_leader_ayah_id", ["source_id", "leader_ayah_id"], IsUnique: false)
    ];

    private static readonly IndexExpectation[] AyahEntryIndexes =
    [
        new("IX_quran_full_i3rab_ayah_entries_source_id_ayah_id", ["source_id", "ayah_id"], IsUnique: true),
        new("IX_quran_full_i3rab_ayah_entries_source_id_verse_key", ["source_id", "verse_key"], IsUnique: true),
        new("IX_quran_full_i3rab_ayah_entries_ayah_id_source_id", ["ayah_id", "source_id"], IsUnique: false),
        new("IX_quran_full_i3rab_ayah_entries_entry_id", ["entry_id"], IsUnique: false)
    ];

    private static readonly string[] ExpectedSourceColumns =
    [
        "id",
        "source_key",
        "display_name_ar",
        "short_name_ar",
        "display_name_en",
        "short_name_en",
        "language_code",
        "direction",
        "contributor_name_ar",
        "contributor_name_en",
        "resource_kind",
        "markup_format",
        "has_quran_quotation_markup",
        "content_coverage_count",
        "package_file",
        "source_file_original",
        "sha256",
        "file_size_bytes",
        "license_status",
        "provenance_status",
        "usage_scope",
        "manifest_metadata",
        "imported_at_utc"
    ];

    private static readonly string[] ExpectedEntryColumns =
    [
        "id",
        "source_id",
        "source_entry_key",
        "leader_ayah_id",
        "i3rab_html",
        "covered_ayah_count",
        "covered_ayah_keys",
        "source_shape",
        "text_hash"
    ];

    private static readonly string[] ExpectedAyahEntryColumns =
    [
        "id",
        "source_id",
        "ayah_id",
        "entry_id",
        "verse_key",
        "source_value_kind",
        "source_leader_verse_key",
        "is_group_leader",
        "sort_order"
    ];

    private static readonly string[] ExpectedSourceChecks =
    [
        "CK_quran_full_i3rab_sources_resource_kind",
        "CK_quran_full_i3rab_sources_content_coverage_count",
        "CK_quran_full_i3rab_sources_direction",
        "CK_quran_full_i3rab_sources_language_code",
        "CK_quran_full_i3rab_sources_markup_format",
        "CK_quran_full_i3rab_sources_license_status",
        "CK_quran_full_i3rab_sources_provenance_status",
        "CK_quran_full_i3rab_sources_usage_scope"
    ];

    [Fact]
    public async Task Schema_has_exactly_three_full_i3rab_tables()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        var tables = await QueryTableNamesAsync(connection);
        tables.Should().BeEquivalentTo(
        [
            "quran_full_i3rab_ayah_entries",
            "quran_full_i3rab_entries",
            "quran_full_i3rab_sources"
        ]);
    }

    [Fact]
    public async Task Schema_is_distinct_from_simple_i3rab_rules_table()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        var fullI3rabTables = await QueryTableNamesAsync(connection);
        fullI3rabTables.Should().NotContain("quran_i3rab_rules",
            "Feature 010 full i'rab must remain separate from Feature 005 simple i'rab");

        foreach (var table in fullI3rabTables)
        {
            table.Should().StartWith("quran_full_i3rab_",
                "all Feature 010 tables must use the quran_full_i3rab_ prefix");
        }
    }

    [Fact]
    public async Task Source_table_has_required_columns_indexes_and_checks()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        const string table = "quran_full_i3rab_sources";

        var columns = await QueryColumnsAsync(connection, table);
        columns.Should().Contain(ExpectedSourceColumns);

        await AssertIndexesMatchAsync(connection, table, SourceIndexes);

        foreach (var check in ExpectedSourceChecks)
        {
            (await NamedCheckConstraintExistsAsync(connection, table, check)).Should().BeTrue(
                $"source table must enforce check {check}");
        }
    }

    [Fact]
    public async Task Entry_table_has_required_columns_indexes_checks_and_foreign_keys()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        const string table = "quran_full_i3rab_entries";

        var columns = await QueryColumnsAsync(connection, table);
        columns.Should().Contain(ExpectedEntryColumns);

        await AssertIndexesMatchAsync(connection, table, EntryIndexes);

        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_full_i3rab_entries_covered_ayah_count")).Should().BeTrue();
        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_full_i3rab_entries_i3rab_html")).Should().BeTrue();
        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_full_i3rab_entries_source_shape")).Should().BeTrue();

        (await ForeignKeyExistsAsync(connection, table, "FK_quran_full_i3rab_entries_quran_full_i3rab_sources_source_id")).Should().BeTrue();
        (await ForeignKeyExistsAsync(connection, table, "FK_quran_full_i3rab_entries_quran_ayahs_leader_ayah_id")).Should().BeTrue();
    }

    [Fact]
    public async Task Ayah_entry_table_has_required_columns_indexes_checks_and_foreign_keys()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        const string table = "quran_full_i3rab_ayah_entries";

        var columns = await QueryColumnsAsync(connection, table);
        columns.Should().Contain(ExpectedAyahEntryColumns);

        await AssertIndexesMatchAsync(connection, table, AyahEntryIndexes);

        (await NamedCheckConstraintExistsAsync(
            connection,
            table,
            "CK_quran_full_i3rab_ayah_entries_source_value_kind")).Should().BeTrue();

        (await ForeignKeyExistsAsync(connection, table, "FK_quran_full_i3rab_ayah_entries_quran_full_i3rab_sources_sour~")).Should().BeTrue();
        (await ForeignKeyExistsAsync(connection, table, "FK_quran_full_i3rab_ayah_entries_quran_ayahs_ayah_id")).Should().BeTrue();
        (await ForeignKeyExistsAsync(connection, table, "FK_quran_full_i3rab_ayah_entries_quran_full_i3rab_entries_entr~")).Should().BeTrue();
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

    private static async Task<IReadOnlyList<string>> QueryTableNamesAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(FullI3rabTablesSql, connection);
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
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

    private static async Task AssertIndexesMatchAsync(
        NpgsqlConnection connection,
        string tableName,
        IReadOnlyList<IndexExpectation> expectedIndexes)
    {
        foreach (var expected in expectedIndexes)
        {
            var actual = await QueryIndexShapeAsync(connection, tableName, expected.Name);
            actual.Should().NotBeNull($"index {expected.Name} on {tableName} should exist");
            actual!.IsUnique.Should().Be(expected.IsUnique, $"index {expected.Name} uniqueness");
            actual.ColumnNames.Should().Equal(expected.ColumnNames, $"index {expected.Name} columns");
        }
    }

    private static async Task<IndexShape?> QueryIndexShapeAsync(
        NpgsqlConnection connection,
        string tableName,
        string indexName)
    {
        await using var command = new NpgsqlCommand(IndexShapeSql, connection);
        command.Parameters.AddWithValue("tableName", tableName);
        command.Parameters.AddWithValue("indexName", indexName);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new IndexShape(
            reader.GetString(0),
            reader.GetBoolean(1),
            reader.GetFieldValue<string[]>(2));
    }

    private static async Task<bool> NamedCheckConstraintExistsAsync(
        NpgsqlConnection connection,
        string tableName,
        string constraintName)
    {
        await using var command = new NpgsqlCommand(NamedCheckConstraintExistsSql, connection);
        command.Parameters.AddWithValue("tableName", tableName);
        command.Parameters.AddWithValue("constraintName", constraintName);
        var result = await command.ExecuteScalarAsync();
        return result is true;
    }

    private static async Task<bool> ForeignKeyExistsAsync(
        NpgsqlConnection connection,
        string tableName,
        string constraintName)
    {
        await using var command = new NpgsqlCommand(ForeignKeyExistsSql, connection);
        command.Parameters.AddWithValue("tableName", tableName);
        command.Parameters.AddWithValue("constraintName", constraintName);
        var result = await command.ExecuteScalarAsync();
        return result is true;
    }

    private sealed record IndexExpectation(string Name, string[] ColumnNames, bool IsUnique);

    private sealed record IndexShape(string Name, bool IsUnique, string[] ColumnNames);
}

[CollectionDefinition(nameof(FullI3rabSchemaTestCollection))]
public sealed class FullI3rabSchemaTestCollection : ICollectionFixture<FullI3rabSchemaFixture>;
