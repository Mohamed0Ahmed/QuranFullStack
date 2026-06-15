using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Translations;

public sealed class EfBulkTranslationImportWriter : ITranslationImportWriter
{
    private readonly QuranDashboardDbContext dbContext;
    private readonly TranslationValidationRunner validationRunner;

    public EfBulkTranslationImportWriter(
        QuranDashboardDbContext dbContext,
        TranslationValidationRunner validationRunner)
    {
        this.dbContext = dbContext;
        this.validationRunner = validationRunner;
    }

    public async Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct)
    {
        return await dbContext.TranslationSources.AnyAsync(ct)
            || await dbContext.TranslationAyahEntries.AnyAsync(ct);
    }

    public async Task<TranslationImportResult> ExecuteAcceptedImportAsync(
        TranslationSourceData source,
        bool force,
        TranslationExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<TranslationImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceUnchangedCheck);
        ArgumentNullException.ThrowIfNull(acceptanceReportWrite);

        if (!force && await AnyTargetTableHasDataAsync(ct))
        {
            throw new InvalidOperationException(TranslationInvariants.TargetsNotEmpty);
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
            throw new InvalidOperationException("Expected an Npgsql connection for translation import.");
        }

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);

        try
        {
            if (force)
            {
                await TranslationCommandExecutor.ExecuteNonQueryAsync(
                    npgsqlConnection, transaction, TranslationSql.TruncateTranslationTables, ct);
            }

            await TranslationBulkCopier.CopySourceFilesAsync(
                npgsqlConnection, transaction, source, ct);

            var totals = TranslationImportTotals.FromSource(source);
            var checks = await validationRunner.RunPostCopyChecksAsync(
                npgsqlConnection,
                transaction,
                source,
                expected,
                ayahTextsByVerseKey,
                sourceUnchangedCheck,
                ct);

            var hardChecks = checks.Where(check => check.Severity == TranslationImportConstants.HardSeverity).ToList();
            var allHardPassed = hardChecks.All(check => check.Passed);

            if (!allHardPassed)
            {
                await transaction.RollbackAsync(ct);
                return BuildFailureResult(runAtUtc, force, totals, checks);
            }

            var acceptanceChecks = checks
                .Append(new TranslationCheckResult(
                    TranslationInvariants.CheckReportWritten,
                    TranslationImportConstants.HardSeverity,
                    "required Markdown and JSON reports written",
                    "written",
                    Passed: true))
                .ToList();

            var successResult = new TranslationImportResult(
                runAtUtc,
                TranslationImportConstants.PassVerdict,
                Persisted: true,
                force,
                totals,
                acceptanceChecks,
                Warnings: [],
                Errors: [],
                InfoNotes: []);

            await acceptanceReportWrite(successResult, ct);

            successResult = successResult with
            {
                InfoNotes = ["Translation import committed; all hard checks passed."]
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

    private static TranslationImportResult BuildFailureResult(
        DateTimeOffset runAtUtc,
        bool force,
        TranslationImportTotals totals,
        IReadOnlyList<TranslationCheckResult> checks)
    {
        var errors = checks
            .Where(check => check.Severity == TranslationImportConstants.HardSeverity && !check.Passed)
            .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
            .ToList();

        return new TranslationImportResult(
            runAtUtc,
            TranslationImportConstants.FailVerdict,
            Persisted: false,
            force,
            totals,
            checks,
            Warnings: [],
            errors,
            InfoNotes: ["Translation import failed validation; no translation rows were persisted."]);
    }
}
