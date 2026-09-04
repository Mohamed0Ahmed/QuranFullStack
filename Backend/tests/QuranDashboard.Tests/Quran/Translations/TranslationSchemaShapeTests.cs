using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;
using QuranDashboard.Domain.Quran.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

[Collection(nameof(TranslationImportTestCollection))]
public sealed class TranslationSchemaShapeTests(TranslationImportTestFixture fixture)
{
    private const string TranslationTablesSql = """
        SELECT table_name
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name LIKE 'quran_translation%'
        ORDER BY table_name
        """;

    private const string ColumnShapeSql = """
        SELECT column_name, data_type, is_nullable
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = @tableName
        ORDER BY ordinal_position
        """;

    private const string ForeignKeyShapeSql = """
        SELECT
            kcu.column_name,
            ccu.table_name AS foreign_table_name,
            ccu.column_name AS foreign_column_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu
            ON tc.constraint_name = kcu.constraint_name
            AND tc.table_schema = kcu.table_schema
        JOIN information_schema.constraint_column_usage ccu
            ON ccu.constraint_name = tc.constraint_name
            AND ccu.table_schema = tc.table_schema
        WHERE tc.constraint_type = 'FOREIGN KEY'
          AND tc.table_schema = 'public'
          AND tc.table_name = @tableName
        ORDER BY kcu.column_name
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

    private static readonly ColumnExpectation[] SourceColumns =
    [
        new("id", "integer", IsNullable: false),
        new("source_key", "text", IsNullable: false),
        new("language_code", "text", IsNullable: false),
        new("language_name_en", "text", IsNullable: false),
        new("language_name_ar", "text", IsNullable: false),
        new("native_name", "text", IsNullable: true),
        new("direction", "text", IsNullable: false),
        new("translation_type", "text", IsNullable: false),
        new("display_name_en", "text", IsNullable: false),
        new("display_name_ar", "text", IsNullable: false),
        new("translator_key", "text", IsNullable: true),
        new("translator_name_en", "text", IsNullable: true),
        new("translator_name_ar", "text", IsNullable: true),
        new("contains_inline_footnotes", "boolean", IsNullable: false),
        new("contains_html_markup", "boolean", IsNullable: false),
        new("content_coverage_count", "smallint", IsNullable: false)
    ];

    private static readonly ColumnExpectation[] AyahEntryColumns =
    [
        new("id", "bigint", IsNullable: false),
        new("source_id", "integer", IsNullable: false),
        new("ayah_id", "integer", IsNullable: false),
        new("verse_key", "text", IsNullable: true),
        new("text", "text", IsNullable: false)
    ];

    private static readonly ForeignKeyExpectation[] AyahEntryForeignKeys =
    [
        new("source_id", "quran_translation_sources", "id"),
        new("ayah_id", "quran_ayahs", "id")
    ];

    private static readonly IndexExpectation[] SourceIndexes =
    [
        new("IX_quran_translation_sources_source_key", ["source_key"], IsUnique: true),
        new("IX_quran_translation_sources_language_code", ["language_code"], IsUnique: false),
        new("IX_quran_translation_sources_language_code_translation_type", ["language_code", "translation_type"], IsUnique: false)
    ];

    private static readonly IndexExpectation[] AyahEntryIndexes =
    [
        new("IX_quran_translation_ayah_entries_source_id_ayah_id", ["source_id", "ayah_id"], IsUnique: true),
        new("IX_quran_translation_ayah_entries_ayah_id_source_id", ["ayah_id", "source_id"], IsUnique: false)
    ];

    [Fact]
    public async Task Schema_has_exactly_two_translation_tables()
    {
        await using var scope = fixture.CreateScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        var tables = await QueryTableNamesAsync(connection);
        tables.Should().BeEquivalentTo(
        [
            "quran_translation_ayah_entries",
            "quran_translation_sources"
        ]);
    }

    [Fact]
    public async Task Schema_has_required_source_columns()
    {
        await using var scope = fixture.CreateScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        await AssertColumnsMatchAsync(connection, "quran_translation_sources", SourceColumns);
    }

    [Fact]
    public async Task Schema_has_required_ayah_entry_columns()
    {
        await using var scope = fixture.CreateScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        await AssertColumnsMatchAsync(connection, "quran_translation_ayah_entries", AyahEntryColumns);
    }

    [Fact]
    public async Task Schema_has_required_source_indexes_and_checks()
    {
        await using var scope = fixture.CreateScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        const string table = "quran_translation_sources";

        await AssertIndexesMatchAsync(connection, table, SourceIndexes);

        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_translation_sources_direction")).Should().BeTrue();
        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_translation_sources_translation_type")).Should().BeTrue();
        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_translation_sources_content_coverage_count")).Should().BeTrue();
        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_translation_sources_required_fields")).Should().BeTrue();
    }

    [Fact]
    public async Task Schema_has_required_ayah_entry_indexes_checks_and_foreign_keys()
    {
        await using var scope = fixture.CreateScope();
        var connection = await OpenConnectionAsync(scope.ServiceProvider);

        const string table = "quran_translation_ayah_entries";

        await AssertColumnsMatchAsync(connection, table, AyahEntryColumns);
        await AssertIndexesMatchAsync(connection, table, AyahEntryIndexes);
        await AssertForeignKeysMatchAsync(connection, table, AyahEntryForeignKeys);

        (await NamedCheckConstraintExistsAsync(connection, table, "CK_quran_translation_ayah_entries_text")).Should().BeTrue();
    }

    [Fact]
    public async Task Schema_accepts_synthetic_source_with_required_coverage_count()
    {
        await fixture.TruncateTranslationTablesAsync();

        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        dbContext.TranslationSources.Add(new TranslationSource
        {
            SourceKey = "en-test-simple",
            LanguageCode = "en",
            LanguageNameEn = "English",
            LanguageNameAr = "الإنجليزية",
            NativeName = "English",
            Direction = "ltr",
            TranslationType = "simple",
            DisplayNameEn = "Synthetic English Test",
            DisplayNameAr = "ترجمة اختبارية إنجليزية",
            ContainsInlineFootnotes = false,
            ContainsHtmlMarkup = false,
            ContentCoverageCount = (short)TranslationInvariants.ExpectedAyahsPerSource
        });

        var act = async () => await dbContext.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    private static async Task AssertColumnsMatchAsync(
        NpgsqlConnection connection,
        string tableName,
        IReadOnlyList<ColumnExpectation> expectedColumns)
    {
        var actualColumns = await QueryColumnShapesAsync(connection, tableName);
        actualColumns.Should().BeEquivalentTo(
            expectedColumns,
            options => options.WithStrictOrdering(),
            $"columns on {tableName} should match the v1 contract");
    }

    private static async Task AssertForeignKeysMatchAsync(
        NpgsqlConnection connection,
        string tableName,
        IReadOnlyList<ForeignKeyExpectation> expectedForeignKeys)
    {
        var actualForeignKeys = await QueryForeignKeyShapesAsync(connection, tableName);
        actualForeignKeys.Should().BeEquivalentTo(
            expectedForeignKeys,
            $"foreign keys on {tableName} should match the v1 contract");
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
        await using var command = new NpgsqlCommand(TranslationTablesSql, connection);
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task<IReadOnlyList<ColumnExpectation>> QueryColumnShapesAsync(
        NpgsqlConnection connection,
        string tableName)
    {
        await using var command = new NpgsqlCommand(ColumnShapeSql, connection);
        command.Parameters.AddWithValue("tableName", tableName);

        var columns = new List<ColumnExpectation>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new ColumnExpectation(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2) == "YES"));
        }

        return columns;
    }

    private static async Task<IReadOnlyList<ForeignKeyExpectation>> QueryForeignKeyShapesAsync(
        NpgsqlConnection connection,
        string tableName)
    {
        await using var command = new NpgsqlCommand(ForeignKeyShapeSql, connection);
        command.Parameters.AddWithValue("tableName", tableName);

        var foreignKeys = new List<ForeignKeyExpectation>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            foreignKeys.Add(new ForeignKeyExpectation(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2)));
        }

        return foreignKeys;
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

    private sealed record ColumnExpectation(string Name, string DataType, bool IsNullable);

    private sealed record ForeignKeyExpectation(
        string ColumnName,
        string ForeignTableName,
        string ForeignColumnName);

    private sealed record IndexExpectation(string Name, string[] ColumnNames, bool IsUnique);

    private sealed record IndexShape(string Name, bool IsUnique, string[] ColumnNames);
}

[CollectionDefinition(nameof(TranslationImportTestCollection), DisableParallelization = true)]
public sealed class TranslationImportTestCollection : ICollectionFixture<TranslationImportTestFixture>;

[CollectionDefinition(nameof(TranslationRebuildTestCollection), DisableParallelization = true)]
public sealed class TranslationRebuildTestCollection : ICollectionFixture<TranslationRebuildTestFixture>;
