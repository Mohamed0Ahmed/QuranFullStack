using QuranDashboard.Application.Abstractions.Quran.FullI3rab;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.FullI3rab;

public sealed class EfBulkFullI3rabImportWriter : IFullI3rabImportWriter
{
    private readonly QuranDashboardDbContext dbContext;

    public EfBulkFullI3rabImportWriter(QuranDashboardDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct)
    {
        return await dbContext.FullI3rabSources.AnyAsync(ct)
            || await dbContext.FullI3rabEntries.AnyAsync(ct)
            || await dbContext.FullI3rabAyahEntries.AnyAsync(ct);
    }

    public async Task<FullI3rabImportResult> ExecuteAcceptedImportAsync(
        FullI3rabSourceData source,
        bool force,
        FullI3rabExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<FullI3rabImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceUnchangedCheck);
        ArgumentNullException.ThrowIfNull(acceptanceReportWrite);

        if (!force && await AnyTargetTableHasDataAsync(ct))
        {
            throw new InvalidOperationException(FullI3rabInvariants.TargetsNotEmpty);
        }

        var runAtUtc = DateTimeOffset.UtcNow;
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for full i'rab import.");
        }

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);

        try
        {
            if (force)
            {
                await FullI3rabCommandExecutor.ExecuteNonQueryAsync(
                    npgsqlConnection, transaction, FullI3rabSql.TruncateFullI3rabTables, ct);
            }

            await FullI3rabBulkCopier.CopyAllSourcesAsync(
                npgsqlConnection, transaction, source, runAtUtc, ct);

            var totals = FullI3rabImportTotals.FromSource(source);
            var checks = await RunPostCopyChecksAsync(
                npgsqlConnection, transaction, expected, ct);

            var hardChecks = checks
                .Where(check => check.Severity == FullI3rabImportConstants.HardSeverity)
                .ToList();
            var allHardPassed = hardChecks.All(check => check.Passed);

            if (!allHardPassed)
            {
                await transaction.RollbackAsync(ct);
                return BuildFailureResult(runAtUtc, force, totals, checks, source.Warnings);
            }

            var acceptanceChecks = checks
                .Append(new FullI3rabCheckResult(
                    FullI3rabInvariants.CheckReportWritten,
                    FullI3rabImportConstants.HardSeverity,
                    "required Markdown and JSON reports written",
                    "written",
                    Passed: true))
                .ToList();

            var successResult = new FullI3rabImportResult(
                runAtUtc,
                FullI3rabImportConstants.PassVerdict,
                Persisted: true,
                force,
                totals,
                acceptanceChecks,
                source.Warnings,
                Errors: [],
                InfoNotes: []);

            await acceptanceReportWrite(successResult, ct);

            successResult = successResult with
            {
                InfoNotes = ["Full i'rab import committed; all hard checks passed."]
            };

            await transaction.CommitAsync(ct);

            return successResult;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task<IReadOnlyList<FullI3rabCheckResult>> RunPostCopyChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FullI3rabExpectedCounts expected,
        CancellationToken ct)
    {
        var sourceCount = await FullI3rabCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, FullI3rabSql.CheckSourceCount, ct);
        var ayahMappingCount = await FullI3rabCommandExecutor.ExecuteScalarLongAsync(
            connection, transaction, FullI3rabSql.CheckAyahMappingCount, ct);
        var coverageSum = await FullI3rabCommandExecutor.ExecuteScalarLongAsync(
            connection, transaction, FullI3rabSql.CheckCoverageSum, ct);
        var distinctAyahs = await FullI3rabCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, FullI3rabSql.CheckDistinctAyahCount, ct);

        var expectedMappings = expected.Sources * expected.AyahsPerSource;

        return
        [
            new FullI3rabCheckResult(
                FullI3rabInvariants.CheckPostCopySourceRows,
                FullI3rabImportConstants.HardSeverity,
                expected.Sources.ToString(CultureInfo.InvariantCulture),
                sourceCount.ToString(CultureInfo.InvariantCulture),
                sourceCount == expected.Sources),
            new FullI3rabCheckResult(
                FullI3rabInvariants.CheckPostCopyAyahMappings,
                FullI3rabImportConstants.HardSeverity,
                expectedMappings.ToString(CultureInfo.InvariantCulture),
                ayahMappingCount.ToString(CultureInfo.InvariantCulture),
                ayahMappingCount == expectedMappings),
            new FullI3rabCheckResult(
                FullI3rabInvariants.CheckPostCopyCoverageSum,
                FullI3rabImportConstants.HardSeverity,
                expectedMappings.ToString(CultureInfo.InvariantCulture),
                coverageSum.ToString(CultureInfo.InvariantCulture),
                coverageSum == expectedMappings),
            new FullI3rabCheckResult(
                FullI3rabInvariants.CheckPostCopyAyahResolved,
                FullI3rabImportConstants.InfoSeverity,
                expected.AyahsPerSource.ToString(CultureInfo.InvariantCulture),
                distinctAyahs.ToString(CultureInfo.InvariantCulture),
                distinctAyahs == expected.AyahsPerSource)
        ];
    }

    private static FullI3rabImportResult BuildFailureResult(
        DateTimeOffset runAtUtc,
        bool force,
        FullI3rabImportTotals totals,
        IReadOnlyList<FullI3rabCheckResult> checks,
        IReadOnlyList<string> warnings)
    {
        var errors = checks
            .Where(check => check.Severity == FullI3rabImportConstants.HardSeverity && !check.Passed)
            .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
            .ToList();

        return new FullI3rabImportResult(
            runAtUtc,
            FullI3rabImportConstants.FailVerdict,
            Persisted: false,
            force,
            totals,
            checks,
            warnings,
            errors,
            InfoNotes: ["Full i'rab import failed validation; no rows were persisted."]);
    }
}
