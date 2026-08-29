using QuranDashboard.Application.Abstractions.Quran.DataPipelines;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Application.Quran.DataPipelines.Tafsirs;

public sealed class ImportTafsirsHandler
{
    private readonly ITafsirImportSource importSource;
    private readonly ITafsirImportWriter importWriter;
    private readonly ITafsirImportReportBuilder reportBuilder;
    private readonly TafsirImportReportEmitter reportEmitter;

    public ImportTafsirsHandler(
        ITafsirImportSource importSource,
        ITafsirImportWriter importWriter,
        ITafsirReportWriter reportWriter,
        ITafsirImportReportBuilder reportBuilder)
    {
        this.importSource = importSource;
        this.importWriter = importWriter;
        this.reportBuilder = reportBuilder;
        this.reportEmitter = new TafsirImportReportEmitter(reportWriter);
    }

    public async Task<ImportTafsirsResult> HandleAsync(ImportTafsirsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourcePath);
        if (!QuranImportProfiles.IsSupported(command.Profile))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command.Profile),
                command.Profile,
                "Unsupported tafsir import profile.");
        }

        var sourcePath = Path.GetFullPath(command.SourcePath);
        var expectedCounts = command.ExpectedCounts ?? TafsirInvariants.Production;
        var reportDir = ResolveReportOutDir(command);

        TafsirSourceData source;
        try
        {
            source = await importSource.LoadAsync(sourcePath, expectedCounts, ct);
        }
        catch (TafsirValidationException ex)
        {
            return await WriteValidationFailureAsync(command, sourcePath, reportDir, ex, ct);
        }
        catch (TafsirSourceException)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, TafsirInvariants.SourceMismatch, refused: true, ct);
        }
        catch (IOException)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, TafsirInvariants.SourceMismatch, refused: true, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message == TafsirInvariants.AyahsMissing)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, ex.Message, refused: true, ct);
        }
        catch (InvalidDataException ex)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, ex.Message, refused: false, ct);
        }

        if (!command.Force && await importWriter.AnyTargetTableHasDataAsync(ct))
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source, TafsirInvariants.TargetsNotEmpty, refused: true, ct);
        }

        var successWarningCount = 0;
        TafsirImportResult result;

        try
        {
            result = await importWriter.ExecuteAcceptedImportAsync(
                source,
                command.Force,
                expectedCounts,
                token => importSource.SourceUnchangedAsync(sourcePath, token),
                async (candidateResult, token) =>
                {
                    var report = reportBuilder.BuildCandidateSuccess(
                        sourcePath,
                        command.Profile,
                        source,
                        command.Force,
                        candidateResult.RunAtUtc,
                        candidateResult.Totals,
                        candidateResult.Checks,
                        expectedCounts);
                    successWarningCount = report.Warnings.Count;
                    await reportEmitter.WriteOrThrowAsync(report, reportDir, token);
                },
                ct);
        }
        catch (InvalidOperationException ex) when (ex.Message == TafsirInvariants.TargetsNotEmpty)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source, ex.Message, refused: true, ct);
        }
        catch (IOException ex)
        {
            return ImportTafsirsResult.Failure(
                $"{TafsirInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ImportTafsirsResult.Failure(
                $"{TafsirInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }

        if (!result.Persisted)
        {
            var failureReport = reportBuilder.BuildValidationFailure(
                sourcePath,
                command.Profile,
                source,
                command.Force,
                result.RunAtUtc,
                result.Totals,
                result.Checks,
                result.Errors);

            var writeFailure = await reportEmitter.TryWriteAsync(failureReport, reportDir, ct);
            if (writeFailure is not null)
            {
                return writeFailure;
            }

            return ImportTafsirsResult.Failure(
                result.Errors.Count > 0
                    ? result.Errors[0]
                    : "Tafsir import validation failed.",
                reportDir,
                failureReport.Warnings.Count);
        }

        return ImportTafsirsResult.Success(result.Totals, reportDir, successWarningCount);
    }

    private async Task<ImportTafsirsResult> EmitPrePersistenceOutcomeAsync(
        ImportTafsirsCommand command,
        string sourcePath,
        string reportDir,
        TafsirSourceData? source,
        string message,
        bool refused,
        CancellationToken ct)
    {
        var report = reportBuilder.BuildRefusal(
            sourcePath,
            command.Profile,
            source,
            command.Force,
            DateTimeOffset.UtcNow,
            message);

        var writeFailure = await reportEmitter.TryWriteAsync(report, reportDir, ct);
        if (writeFailure is not null)
        {
            return writeFailure;
        }

        return refused
            ? ImportTafsirsResult.Refused(message, reportDir)
            : ImportTafsirsResult.Failure(message, reportDir);
    }

    private async Task<ImportTafsirsResult> WriteValidationFailureAsync(
        ImportTafsirsCommand command,
        string sourcePath,
        string reportDir,
        TafsirValidationException ex,
        CancellationToken ct)
    {
        var report = reportBuilder.BuildValidationFailure(
            sourcePath,
            command.Profile,
            source: null,
            command.Force,
            DateTimeOffset.UtcNow,
            TafsirImportTotals.Empty,
            ex.Checks,
            ex.FailedChecks
                .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
                .ToList());

        var writeFailure = await reportEmitter.TryWriteAsync(report, reportDir, ct);
        if (writeFailure is not null)
        {
            return writeFailure;
        }

        var firstFailed = ex.FailedChecks.FirstOrDefault();
        if (firstFailed is null)
        {
            return ImportTafsirsResult.Failure(ex.Message, reportDir);
        }

        return ImportTafsirsResult.Failure(
            $"{firstFailed.Id}: expected {firstFailed.Expected}, observed {firstFailed.Observed}",
            reportDir);
    }

    private static string ResolveReportOutDir(ImportTafsirsCommand command)
    {

        if (string.IsNullOrWhiteSpace(command.ReportOutDir))
        {
            throw new InvalidOperationException(
                "A report output directory must be provided by the caller.");
        }

        return Path.GetFullPath(command.ReportOutDir);
    }
}
