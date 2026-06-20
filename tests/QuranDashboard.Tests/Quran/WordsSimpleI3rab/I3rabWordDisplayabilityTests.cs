using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.SimpleI3rabGeneration;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

[Collection(nameof(I3rabGenerationTestCollection))]
public sealed class I3rabWordDisplayabilityTests(I3rabGenerationTestFixture fixture)
{
    [Fact]
    public async Task Word_displayable_hard_check_fails_when_multi_segment_word_has_unlabeled_segment()
    {
        await fixture.ResetToPartialMorphologyAsync();

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var multiSegmentWordId = 5;
        var segments = await dbContext.WordMorphologySegments
            .Where(segment => segment.QuranWordId == multiSegmentWordId)
            .OrderBy(segment => segment.SegmentNumber)
            .ToListAsync();

        segments.Should().HaveCount(2);

        segments[0].I3rabStatus = I3rabStatusMapping.Approved;
        segments[0].I3rabArabic = "اسم مرفوع";
        segments[1].I3rabStatus = I3rabStatusMapping.Unsupported;
        segments[1].I3rabArabic = null;
        await dbContext.SaveChangesAsync();

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var npgsqlConnection = (NpgsqlConnection)connection;
        await using var transaction = await npgsqlConnection.BeginTransactionAsync();

        var before = await I3rabValidationRunner.CaptureSnapshotAsync(npgsqlConnection, transaction, CancellationToken.None);
        var checks = await I3rabValidationRunner.RunHardChecksAsync(
            npgsqlConnection,
            transaction,
            before,
            before,
            I3rabGenerationTestFixture.PartialMorphologyCounts,
            CancellationToken.None);

        var displayableCheck = checks.Single(check => check.Id == I3rabInvariants.CheckWordDisplayable);
        displayableCheck.Passed.Should().BeFalse(
            "a readable word is displayable only when every morphology segment has an approved non-null label");

        var undisplayableCount = await ExecuteScalarIntAsync(npgsqlConnection, transaction, I3rabSql.CountUndisplayableWords);
        undisplayableCount.Should().BeGreaterThan(0);
    }

    private static async Task<int> ExecuteScalarIntAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }
}
