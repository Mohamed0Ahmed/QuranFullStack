using System.Data;
using Npgsql;
using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Tafsirs;

public sealed class EfBulkTafsirImportWriter : ITafsirImportWriter
{
    private readonly QuranDashboardDbContext dbContext;
    private readonly TafsirValidationRunner validationRunner;

    public EfBulkTafsirImportWriter(
        QuranDashboardDbContext dbContext,
        TafsirValidationRunner validationRunner)
    {
        this.dbContext = dbContext;
        this.validationRunner = validationRunner;
    }

    public async Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct)
    {
        return await dbContext.TafsirSources.AnyAsync(ct)
            || await dbContext.TafsirEntries.AnyAsync(ct)
            || await dbContext.TafsirAyahEntries.AnyAsync(ct);
    }

    public async Task<TafsirImportResult> ExecuteAcceptedImportAsync(
        TafsirSourceData source,
        bool force,
        TafsirExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<TafsirImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceUnchangedCheck);
        ArgumentNullException.ThrowIfNull(acceptanceReportWrite);

        if (!force && await AnyTargetTableHasDataAsync(ct))
        {
            throw new InvalidOperationException(TafsirInvariants.TargetsNotEmpty);
        }

        var runAtUtc = DateTimeOffset.UtcNow;
        var connection = dbContext.Database.GetDbConnection();
        var ayahTextsByVerseKey = await dbContext.QuranAyahs
            .AsNoTracking()
            .ToDictionaryAsync(ayah => ayah.VerseKey, ayah => ayah.TextUthmani, ct);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for tafsir import.");
        }

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);

        try
        {
            if (force)
            {
                await TafsirCommandExecutor.ExecuteNonQueryAsync(
                    npgsqlConnection, transaction, TafsirSql.TruncateTafsirTables, ct);
            }

            await TafsirBulkCopier.CopySourceFilesAsync(
                npgsqlConnection, transaction, source, runAtUtc, ct);

            var totals = BuildTotals(source);
            var checks = await validationRunner.RunPostCopyChecksAsync(
                npgsqlConnection,
                transaction,
                source,
                expected,
                ayahTextsByVerseKey,
                sourceUnchangedCheck,
                ct);

            var hardChecks = checks.Where(check => check.Severity == TafsirImportConstants.HardSeverity).ToList();
            var allHardPassed = hardChecks.All(check => check.Passed);

            if (!allHardPassed)
            {
                await transaction.RollbackAsync(ct);
                return BuildFailureResult(runAtUtc, force, totals, checks);
            }

            var acceptanceChecks = checks
                .Append(new TafsirCheckResult(
                    TafsirInvariants.CheckReportWritten,
                    TafsirImportConstants.HardSeverity,
                    "required Markdown and JSON reports written",
                    "written",
                    Passed: true))
                .ToList();

            var successResult = new TafsirImportResult(
                runAtUtc,
                TafsirImportConstants.PassVerdict,
                Persisted: true,
                force,
                totals,
                acceptanceChecks,
                Warnings: [],
                Errors: [],
                InfoNotes: ["Tafsir import passed validation; writing acceptance reports before commit."]);

            try
            {
                await acceptanceReportWrite(successResult, ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }

            successResult = successResult with
            {
                InfoNotes = ["Tafsir import committed; all hard checks passed."]
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

    private static TafsirImportTotals BuildTotals(TafsirSourceData source)
    {
        var arabicSources = source.Sources.Count(row => row.LanguageCode == "ar");
        var languages = source.Sources.Select(row => row.LanguageCode).Distinct().Count();
        var distinctAyahs = source.AyahEntries.Select(row => row.AyahId).Distinct().Count();

        return new TafsirImportTotals(
            source.Sources.Count,
            source.Entries.Count,
            source.AyahEntries.Count,
            source.Sources.Count,
            source.ExcludedSources.Count,
            arabicSources,
            source.Sources.Count - arabicSources,
            languages,
            distinctAyahs);
    }

    private static TafsirImportResult BuildFailureResult(
        DateTimeOffset runAtUtc,
        bool force,
        TafsirImportTotals totals,
        IReadOnlyList<TafsirCheckResult> checks)
    {
        var errors = checks
            .Where(check => check.Severity == TafsirImportConstants.HardSeverity && !check.Passed)
            .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
            .ToList();

        return new TafsirImportResult(
            runAtUtc,
            TafsirImportConstants.FailVerdict,
            Persisted: false,
            force,
            totals,
            checks,
            Warnings: [],
            errors,
            InfoNotes: ["Tafsir import failed validation; no tafsir rows were persisted."]);
    }
}
