using System.Data;
using Npgsql;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirSchemaShapeTests(TafsirImportTestFixture fixture)
{
    private const string TafsirTablesSql = """
        SELECT table_name
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name LIKE 'quran_tafsir%'
        ORDER BY table_name
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

    private static readonly IndexExpectation[] SourceIndexes =
    [
        new("IX_quran_tafsir_sources_source_key", ["source_key"], IsUnique: true),
        new("IX_quran_tafsir_sources_package_file", ["package_file"], IsUnique: true),
        new("IX_quran_tafsir_sources_language_code", ["language_code"], IsUnique: false),
        new("IX_quran_tafsir_sources_language_code_tafsir_kind", ["language_code", "tafsir_kind"], IsUnique: false)
    ];

    private static readonly IndexExpectation[] EntryIndexes =
    [
        new("IX_quran_tafsir_entries_source_id_source_entry_key", ["source_id", "source_entry_key"], IsUnique: true),
        new("IX_quran_tafsir_entries_leader_ayah_id", ["leader_ayah_id"], IsUnique: false),
        new("IX_quran_tafsir_entries_source_id_leader_ayah_id", ["source_id", "leader_ayah_id"], IsUnique: false)
    ];

    private static readonly IndexExpectation[] AyahEntryIndexes =
    [
        new("IX_quran_tafsir_ayah_entries_source_id_ayah_id", ["source_id", "ayah_id"], IsUnique: true),
        new("IX_quran_tafsir_ayah_entries_source_id_verse_key", ["source_id", "verse_key"], IsUnique: true),
        new("IX_quran_tafsir_ayah_entries_ayah_id_source_id", ["ayah_id", "source_id"], IsUnique: false),
        new("IX_quran_tafsir_ayah_entries_tafsir_entry_id", ["tafsir_entry_id"], IsUnique: false)
    ];

    [Fact]
    public async Task Schema_has_exactly_three_tafsir_tables()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        var tables = await QueryTableNamesAsync(connection);
        tables.Should().BeEquivalentTo(
        [
            "quran_tafsir_ayah_entries",
            "quran_tafsir_entries",
            "quran_tafsir_sources"
        ]);
    }

    [Fact]
    public async Task Schema_has_required_source_indexes_and_checks()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        const string table = "quran_tafsir_sources";

        await AssertIndexesMatchAsync(connection, table, SourceIndexes);

        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_tafsir_sources_resource_kind")).Should().BeTrue();
        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_tafsir_sources_content_coverage_count")).Should().BeTrue();
        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_tafsir_sources_direction")).Should().BeTrue();
    }

    [Fact]
    public async Task Schema_has_required_entry_indexes_and_checks()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        const string table = "quran_tafsir_entries";

        await AssertIndexesMatchAsync(connection, table, EntryIndexes);

        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_tafsir_entries_covered_ayah_count")).Should().BeTrue();
        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_tafsir_entries_tafsir_text")).Should().BeTrue();
        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_tafsir_entries_source_shape")).Should().BeTrue();
    }

    [Fact]
    public async Task Schema_has_required_ayah_entry_indexes_and_checks()
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        const string table = "quran_tafsir_ayah_entries";

        await AssertIndexesMatchAsync(connection, table, AyahEntryIndexes);

        (await NamedCheckConstraintExistsAsync(
            connection,
            table,
            "CK_quran_tafsir_ayah_entries_source_value_kind")).Should().BeTrue();
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
        await using var command = new NpgsqlCommand(TafsirTablesSql, connection);
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
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

    private sealed record IndexExpectation(string Name, string[] ColumnNames, bool IsUnique);

    private sealed record IndexShape(string Name, bool IsUnique, string[] ColumnNames);
}

[CollectionDefinition(nameof(TafsirImportTestCollection))]
public sealed class TafsirImportTestCollection : ICollectionFixture<TafsirImportTestFixture>;
