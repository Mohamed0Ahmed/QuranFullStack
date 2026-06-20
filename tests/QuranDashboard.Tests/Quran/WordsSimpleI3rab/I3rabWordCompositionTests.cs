using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.SimpleI3rabGeneration;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

[Collection(nameof(I3rabGenerationTestCollection))]
public sealed class I3rabWordCompositionTests(I3rabGenerationTestFixture fixture)
{
    // Catalogue labels for PREFIX:P + STEM:N:GEN + SUFFIX:PRON:2MS (the بِحَمْدِكَ segment pattern).
    // v1 stores per-signature labels and composes by join; «محل …» role refinements are read-layer (data-model §8).
    private const string BihamdikaLocation = "2:30:20";
    private const string ExpectedJoinedLabels = "حرف جر، اسم مجرور، ضمير متصل للمخاطب";

    private const string WordSummarySql = """
        SELECT string_agg(s.i3rab_arabic, '، ' ORDER BY s.segment_number)
        FROM quran_word_morphology_segments AS s
        INNER JOIN quran_words AS w ON w.id = s.quran_word_id
        WHERE w.location = @location
        """;

    [Fact]
    public async Task Bihamdika_word_summary_composes_from_ordered_segment_labels()
    {
        await fixture.ResetToBihamdikaWordCompositionAsync();
        var result = await fixture.RunGenerationAsync(I3rabGenerationTestFixture.BihamdikaWordCompositionCounts);
        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = new NpgsqlCommand(WordSummarySql, (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("location", BihamdikaLocation);
        var composed = (string?)await command.ExecuteScalarAsync();
        composed.Should().Be(ExpectedJoinedLabels);
    }

    [Fact]
    public async Task Word_displayable_hard_check_passes_for_every_readable_word_in_fixture()
    {
        await fixture.ResetToCompleteMorphologyAsync();
        var reportDir = Path.Combine(Path.GetTempPath(), $"i3rab-word-display-{Guid.NewGuid():N}");
        var expectedCounts = I3rabGenerationTestFixture.CompleteMorphologyCounts;

        var result = await fixture.RunGenerationAsync(expectedCounts, reportOutDir: reportDir);
        result.Succeeded.Should().BeTrue(result.Message);

        var reportJson = await File.ReadAllTextAsync(Path.Combine(reportDir, "simple-i3rab-generation-report.json"));
        using var document = JsonDocument.Parse(reportJson);
        var displayableCheck = document.RootElement
            .GetProperty("checks")
            .EnumerateArray()
            .Single(check => check.GetProperty("id").GetString() == I3rabInvariants.CheckWordDisplayable);

        displayableCheck.GetProperty("passed").GetBoolean().Should().BeTrue();
        displayableCheck.GetProperty("expected").GetString().Should().Be(expectedCounts.ReadableWordCount.ToString());
        displayableCheck.GetProperty("observed").GetString().Should().Be(expectedCounts.ReadableWordCount.ToString());

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var displayableCount = await ExecuteScalarIntAsync((NpgsqlConnection)connection, I3rabSql.CountDisplayableWords);
        displayableCount.Should().Be(expectedCounts.ReadableWordCount);
    }

    private static async Task<int> ExecuteScalarIntAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }
}
