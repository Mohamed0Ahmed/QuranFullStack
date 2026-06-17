using QuranDashboard.Application.Abstractions.Quran.FullI3rab;

namespace QuranDashboard.Application.Quran.FullI3rab.ImportFullI3rab;

public sealed class ImportFullI3rabHandler
{
    private readonly IFullI3rabImportSource importSource;
    private readonly IFullI3rabImportWriter importWriter;
    private readonly IFullI3rabImportReportBuilder reportBuilder;
    private readonly FullI3rabImportReportEmitter reportEmitter;

    public ImportFullI3rabHandler(
        IFullI3rabImportSource importSource,
        IFullI3rabImportWriter importWriter,
        IFullI3rabReportWriter reportWriter,
        IFullI3rabImportReportBuilder reportBuilder)
    {
        this.importSource = importSource;
        this.importWriter = importWriter;
        this.reportBuilder = reportBuilder;
        this.reportEmitter = new FullI3rabImportReportEmitter(reportWriter);
    }

    public async Task<ImportFullI3rabResult> HandleAsync(ImportFullI3rabCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourcePath);

        var sourcePath = Path.GetFullPath(command.SourcePath);
        var expectedCounts = command.ExpectedCounts ?? FullI3rabInvariants.Production;
        var reportDir = ResolveReportOutDir(command);

        FullI3rabSourceData source;
        try
        {
            source = await importSource.LoadAsync(sourcePath, expectedCounts, ct);
        }
        catch (FullI3rabValidationException ex)
        {
            return await WriteValidationFailureAsync(command, sourcePath, reportDir, ex, ct);
        }
        catch (FullI3rabSourceException)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, FullI3rabInvariants.SourceMismatch, refused: true, ct);
        }
        catch (IOException)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, FullI3rabInvariants.SourceMismatch, refused: true, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message == FullI3rabInvariants.AyahsMissing)
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
                command, sourcePath, reportDir, source, FullI3rabInvariants.TargetsNotEmpty, refused: true, ct);
        }

        var successWarningCount = 0;
        FullI3rabImportResult result;

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
                        source,
                        command.Force,
                        candidateResult.RunAtUtc,
                        candidateResult.Totals,
                        candidateResult.Checks,
                        expectedCounts);
                    successWarningCount = source.Warnings.Count;
                    await reportEmitter.WriteOrThrowAsync(report, reportDir, token);
                },
                ct);
        }
        catch (InvalidOperationException ex) when (ex.Message == FullI3rabInvariants.TargetsNotEmpty)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source, ex.Message, refused: true, ct);
        }
        catch (IOException ex)
        {
            return ImportFullI3rabResult.Failure(
                $"{FullI3rabInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ImportFullI3rabResult.Failure(
                $"{FullI3rabInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }

        if (!result.Persisted)
        {
            var failureReport = reportBuilder.BuildValidationFailure(
                sourcePath,
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

            return ImportFullI3rabResult.Failure(
                result.Errors.Count > 0
                    ? result.Errors[0]
                    : "Full i'rab import validation failed.",
                reportDir,
                source.Warnings.Count);
        }

        return ImportFullI3rabResult.Success(result.Totals, reportDir, successWarningCount);
    }

    private async Task<ImportFullI3rabResult> EmitPrePersistenceOutcomeAsync(
        ImportFullI3rabCommand command,
        string sourcePath,
        string reportDir,
        FullI3rabSourceData? source,
        string message,
        bool refused,
        CancellationToken ct)
    {
        var report = reportBuilder.BuildRefusal(
            sourcePath,
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
            ? ImportFullI3rabResult.Refused(message, reportDir)
            : ImportFullI3rabResult.Failure(message, reportDir);
    }

    private async Task<ImportFullI3rabResult> WriteValidationFailureAsync(
        ImportFullI3rabCommand command,
        string sourcePath,
        string reportDir,
        FullI3rabValidationException ex,
        CancellationToken ct)
    {
        var report = reportBuilder.BuildValidationFailure(
            sourcePath,
            source: null,
            command.Force,
            DateTimeOffset.UtcNow,
            FullI3rabImportTotals.Empty,
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
            return ImportFullI3rabResult.Failure(ex.Message, reportDir);
        }

        return ImportFullI3rabResult.Failure(
            $"{firstFailed.Id}: expected {firstFailed.Expected}, observed {firstFailed.Observed}",
            reportDir);
    }

    private static string ResolveReportOutDir(ImportFullI3rabCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ReportOutDir);
        return Path.GetFullPath(command.ReportOutDir);
    }
}
