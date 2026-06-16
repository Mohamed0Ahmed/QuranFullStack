using QuranDashboard.Application.Abstractions.Quran.Navigation;
using QuranDashboard.Infrastructure.Files.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Navigation;

public sealed class EfBulkNavigationMetadataImportWriter : INavigationMetadataImportWriter
{
    private readonly QuranDashboardDbContext dbContext;
    private readonly NavigationMetadataValidationRunner validationRunner;

    public EfBulkNavigationMetadataImportWriter(
        QuranDashboardDbContext dbContext,
        NavigationMetadataValidationRunner validationRunner)
    {
        this.dbContext = dbContext;
        this.validationRunner = validationRunner;
    }

    public async Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for navigation import.");
        }

        await using var command = new NpgsqlCommand(NavigationMetadataSql.ProbeTargetHasData, npgsqlConnection);
        command.CommandTimeout = NavigationMetadataCommandExecutor.CommandTimeoutSeconds;
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1;
    }

    public async Task<NavigationMetadataImportResult> ExecuteAcceptedImportAsync(
        NavigationMetadataSourceData source,
        bool force,
        NavigationExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<NavigationMetadataImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceUnchangedCheck);
        ArgumentNullException.ThrowIfNull(acceptanceReportWrite);

        if (!force && await AnyTargetTableHasDataAsync(ct))
        {
            throw new InvalidOperationException(NavigationMetadataInvariants.TargetsNotEmpty);
        }

        var runAtUtc = DateTimeOffset.UtcNow;
        var ayahRows = await dbContext.QuranAyahs
            .AsNoTracking()
            .Select(ayah => new { ayah.VerseKey, ayah.Id })
            .ToListAsync(ct);

        if (ayahRows.Count == 0)
        {
            throw new InvalidOperationException(NavigationMetadataInvariants.AyahsMissing);
        }

        var ayahIdsByVerseKey = ayahRows.ToDictionary(
            row => row.VerseKey,
            row => row.Id,
            StringComparer.Ordinal);

        AssembledNavigationMetadata assembled;
        try
        {
            assembled = validationRunner.AssembleAndValidate(source, ayahIdsByVerseKey, expected);
        }
        catch (NavigationMetadataValidationException ex)
        {
            return BuildValidationFailureResult(runAtUtc, force, ex.Checks);
        }

        var totals = new NavigationImportTotals(
            assembled.Juz.Count,
            assembled.Hizb.Count,
            assembled.Rub.Count,
            assembled.Sajda.Count,
            assembled.AyahAssignments.Count);

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for navigation import.");
        }

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);

        try
        {
            if (force)
            {
                await NavigationMetadataCommandExecutor.ExecuteNonQueryAsync(
                    npgsqlConnection, transaction, NavigationMetadataSql.ClearNavigationData, ct);
            }

            await NavigationMetadataBulkCopier.CopyNavigationDataAsync(
                npgsqlConnection, transaction, assembled, ct);

            var checks = assembled.Warnings
                .Concat(await validationRunner.RunPostCopyChecksAsync(
                    npgsqlConnection,
                    transaction,
                    expected,
                    sourceUnchangedCheck,
                    ct))
                .ToList();

            var hardChecks = checks
                .Where(check => check.Severity == NavigationImportConstants.HardSeverity)
                .ToList();
            var allHardPassed = hardChecks.All(check => check.Passed);

            if (!allHardPassed)
            {
                await transaction.RollbackAsync(ct);
                return BuildFailureResult(runAtUtc, force, totals, checks);
            }

            var acceptanceChecks = checks
                .Append(new NavigationCheckResult(
                    NavigationMetadataInvariants.CheckReportWritten,
                    NavigationImportConstants.HardSeverity,
                    "required Markdown and JSON reports written",
                    "written",
                    Passed: true))
                .Append(new NavigationCheckResult(
                    NavigationMetadataInvariants.CheckRollbackOnFail,
                    NavigationImportConstants.HardSeverity,
                    "rollback on hard failure",
                    "not needed",
                    Passed: true))
                .ToList();

            var successResult = new NavigationMetadataImportResult(
                runAtUtc,
                NavigationImportConstants.AcceptedVerdict,
                Persisted: true,
                force,
                totals,
                acceptanceChecks,
                BuildWarningMessages(assembled.Warnings),
                Errors: []);

            await acceptanceReportWrite(successResult, ct);
            await transaction.CommitAsync(ct);
            return successResult;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static NavigationMetadataImportResult BuildValidationFailureResult(
        DateTimeOffset runAtUtc,
        bool force,
        IReadOnlyList<NavigationCheckResult> checks) =>
        BuildFailureResult(
            runAtUtc,
            force,
            NavigationImportTotals.Empty,
            checks);

    private static NavigationMetadataImportResult BuildFailureResult(
        DateTimeOffset runAtUtc,
        bool force,
        NavigationImportTotals totals,
        IReadOnlyList<NavigationCheckResult> checks)
    {
        var errors = checks
            .Where(check => check.Severity == NavigationImportConstants.HardSeverity && !check.Passed)
            .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
            .ToList();

        return new NavigationMetadataImportResult(
            runAtUtc,
            NavigationImportConstants.ValidationFailedVerdict,
            Persisted: false,
            force,
            totals,
            checks,
            BuildWarningMessages(checks.Where(check => check.Severity == NavigationImportConstants.WarningSeverity).ToList()),
            errors);
    }

    private static IReadOnlyList<string> BuildWarningMessages(IReadOnlyList<NavigationCheckResult> warnings) =>
        warnings
            .Where(check => !check.Passed)
            .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
            .ToList();
}
