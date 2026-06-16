namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

[Collection(nameof(I3rabGenerationTestCollection))]
public sealed class I3rabSchemaShapeTests(I3rabGenerationTestFixture fixture)
{
    private const string TableExistsSql = """
        SELECT EXISTS (
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name = @tableName
        )
        """;

    private const string ColumnExistsSql = """
        SELECT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'quran_word_morphology_segments'
              AND column_name = @columnName
        )
        """;

    [Fact]
    public async Task Schema_has_inline_i3rab_columns_only_and_no_stored_word_or_segment_i3rab_tables()
    {
        await fixture.ResetToCompleteMorphologyAsync();
        await fixture.RunGenerationAsync(I3rabGenerationTestFixture.CompleteMorphologyCounts);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var npgsqlConnection = (NpgsqlConnection)connection;

        (await TableExistsAsync(npgsqlConnection, "quran_word_i3rab")).Should().BeFalse(
            "word-level summaries are composed at read time — no quran_word_i3rab table (FR-018, SC-009)");
        (await TableExistsAsync(npgsqlConnection, "quran_word_segment_i3rab")).Should().BeFalse(
            "segment labels live inline on quran_word_morphology_segments — no quran_word_segment_i3rab table (FR-018, SC-009)");
        (await TableExistsAsync(npgsqlConnection, "quran_i3rab_rules")).Should().BeTrue(
            "the 142-row signature catalogue is persisted in quran_i3rab_rules");

        foreach (var columnName in new[] { "i3rab_arabic", "i3rab_rule_id", "i3rab_status", "i3rab_review_reason" })
        {
            (await SegmentColumnExistsAsync(npgsqlConnection, columnName)).Should().BeTrue(
                $"segment i'rab is stored inline on quran_word_morphology_segments via {columnName}");
        }
    }

    private static async Task<bool> TableExistsAsync(NpgsqlConnection connection, string tableName)
    {
        await using var command = new NpgsqlCommand(TableExistsSql, connection);
        command.Parameters.AddWithValue("tableName", tableName);
        var result = await command.ExecuteScalarAsync();
        return result is true;
    }

    private static async Task<bool> SegmentColumnExistsAsync(NpgsqlConnection connection, string columnName)
    {
        await using var command = new NpgsqlCommand(ColumnExistsSql, connection);
        command.Parameters.AddWithValue("columnName", columnName);
        var result = await command.ExecuteScalarAsync();
        return result is true;
    }
}
