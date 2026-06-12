using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;

public sealed class EfI3rabGenerationWriter(
    QuranDashboardDbContext dbContext,
    I3rabExpectedCounts expectedCounts,
    II3rabGenerationWriteProbe writeProbe)
    : II3rabGenerationWriter
{
    public async Task<I3rabGenerationResult> WriteAsync(
        IReadOnlyList<I3rabRuleSeedRow> rules,
        IReadOnlyList<I3rabSegmentLabel> labels,
        bool force,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(labels);

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for i3rab generation.");
        }

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);
        var beforeSnapshot = await I3rabValidationRunner.CaptureSnapshotAsync(npgsqlConnection, transaction, ct);

        try
        {
            foreach (var rule in rules.OrderBy(row => row.SortOrder))
            {
                await UpsertRuleAsync(npgsqlConnection, transaction, rule, ct);
            }

            await ExecuteNonQueryAsync(npgsqlConnection, transaction, I3rabSql.CreateStagingTable, ct);
            await CopyLabelsAsync(npgsqlConnection, labels, ct);
            await ExecuteNonQueryAsync(npgsqlConnection, transaction, I3rabSql.UpdateSegmentsFromStaging, ct);
            await writeProbe.AfterSegmentUpdateAsync(npgsqlConnection, transaction, ct);

            var afterSnapshot = await I3rabValidationRunner.CaptureSnapshotAsync(npgsqlConnection, transaction, ct);
            var checks = await I3rabValidationRunner.RunHardChecksAsync(
                npgsqlConnection,
                transaction,
                beforeSnapshot,
                afterSnapshot,
                expectedCounts,
                ct);

            var allPassed = checks.All(check => check.Passed);
            if (!allPassed)
            {
                await transaction.RollbackAsync(ct);
                return await BuildResultAsync(
                    npgsqlConnection,
                    null,
                    force,
                    expectedCounts,
                    afterSnapshot,
                    checks,
                    persisted: false,
                    ct);
            }

            await transaction.CommitAsync(ct);
            return await BuildResultAsync(
                npgsqlConnection,
                null,
                force,
                expectedCounts,
                afterSnapshot,
                checks,
                persisted: true,
                ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task<I3rabGenerationResult> BuildResultAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        bool force,
        I3rabExpectedCounts expectedCounts,
        I3rabSourceSnapshot snapshot,
        IReadOnlyList<I3rabCheckResult> checks,
        bool persisted,
        CancellationToken ct)
    {
        var approvedCount = persisted
            ? await ExecuteScalarIntAsync(connection, transaction, I3rabSql.CountApprovedSegments, ct)
            : 0;
        var needsReviewCount = persisted
            ? await ExecuteScalarIntAsync(connection, transaction, I3rabSql.CountNeedsReviewSegments, ct)
            : 0;
        var unsupportedCount = persisted
            ? await ExecuteScalarIntAsync(connection, transaction, I3rabSql.CountUnsupportedSegments, ct)
            : 0;
        var displayableWordCount = persisted
            ? await ExecuteScalarIntAsync(connection, transaction, I3rabSql.CountDisplayableWords, ct)
            : 0;
        var nullFormsPreserved = persisted
            ? snapshot.NullFormSegmentIds.Count
            : 0;

        var warnings = await I3rabValidationRunner.BuildWarningsAsync(
            connection,
            transaction,
            expectedCounts.SegmentCount,
            approvedCount,
            needsReviewCount,
            unsupportedCount,
            ct);

        return new I3rabGenerationResult(
            persisted,
            force,
            expectedCounts.SegmentCount,
            approvedCount,
            needsReviewCount,
            unsupportedCount,
            I3rabInvariants.ExpectedRuleCount,
            I3rabInvariants.ExpectedFamilyCount,
            expectedCounts.ReadableWordCount,
            displayableWordCount,
            nullFormsPreserved,
            checks,
            warnings);
    }

    private static async Task<int> ExecuteScalarIntAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        var result = await command.ExecuteScalarAsync(ct);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }

    private static async Task UpsertRuleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        I3rabRuleSeedRow rule,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(I3rabSql.UpsertRule, connection, transaction);
        command.Parameters.AddWithValue("signatureKey", rule.SignatureKey);
        command.Parameters.AddWithValue("ruleFamily", rule.RuleFamily);
        command.Parameters.AddWithValue("i3rabArabic", rule.I3rabArabic);
        command.Parameters.AddWithValue("defaultStatus", rule.DefaultStatus);
        command.Parameters.AddWithValue("description", (object?)rule.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("sortOrder", rule.SortOrder);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task CopyLabelsAsync(
        NpgsqlConnection connection,
        IReadOnlyList<I3rabSegmentLabel> labels,
        CancellationToken ct)
    {
        await using var importer = await connection.BeginBinaryImportAsync(I3rabSql.CopyStaging, ct);

        foreach (var label in labels)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(label.SegmentId, ct);
            await importer.WriteAsync(label.I3rabArabic, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(label.SignatureKey, ct);
            await importer.WriteAsync(label.Status, ct);
            await importer.WriteAsync(label.ReviewReason, NpgsqlDbType.Text, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task ExecuteNonQueryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(ct);
    }
}
