using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

namespace QuranDashboard.Application.Quran.DataPipelines.Navigation;

public sealed class ImportNavigationMetadataHandler
{
    private readonly INavigationMetadataImportSource importSource;
    private readonly INavigationMetadataImportWriter importWriter;
    private readonly INavigationMetadataImportReportBuilder reportBuilder;
    private readonly NavigationMetadataImportReportEmitter reportEmitter;

    public ImportNavigationMetadataHandler(
        INavigationMetadataImportSource importSource,
        INavigationMetadataImportWriter importWriter,
        INavigationMetadataReportWriter reportWriter,
        INavigationMetadataImportReportBuilder reportBuilder)
    {
        this.importSource = importSource;
        this.importWriter = importWriter;
        this.reportBuilder = reportBuilder;
        this.reportEmitter = new NavigationMetadataImportReportEmitter(reportWriter);
    }

    public async Task<ImportNavigationMetadataResult> HandleAsync(
        ImportNavigationMetadataCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourcePath);

        var sourcePath = Path.GetFullPath(command.SourcePath);
        var expectedCounts = command.ExpectedCounts ?? NavigationMetadataInvariants.Production;
        var reportDir = ResolveReportOutDir(command);

        NavigationMetadataSourceData source;
        try
        {
            source = await importSource.LoadAsync(sourcePath, expectedCounts, ct);
        }
        catch (NavigationMetadataValidationException ex)
        {
            return await WriteValidationFailureAsync(command, sourcePath, reportDir, ex, ct);
        }
        catch (NavigationMetadataSourceException ex) when (ex.FailedChecks.Count > 0)
        {
            return await WriteValidationFailureAsync(
                command,
                sourcePath,
                reportDir,
                new NavigationMetadataValidationException(ex.FailedChecks),
                ct);
        }
        catch (NavigationMetadataSourceException)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, NavigationMetadataInvariants.SourceMismatch, refused: true, ct);
        }
        catch (JsonException)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, NavigationMetadataInvariants.SourceMismatch, refused: true, ct);
        }
        catch (IOException)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, NavigationMetadataInvariants.SourceMismatch, refused: true, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message == NavigationMetadataInvariants.AyahsMissing)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, ex.Message, refused: true, ct);
        }

        if (!command.Force && await importWriter.AnyTargetTableHasDataAsync(ct))
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source, NavigationMetadataInvariants.TargetsNotEmpty, refused: true, ct);
        }

        var successWarningCount = 0;
        NavigationMetadataImportResult result;

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
                    successWarningCount = report.Warnings.Count;
                    await reportEmitter.WriteSuccessAsync(report, reportDir, token);
                },
                ct);
        }
        catch (InvalidOperationException ex) when (ex.Message == NavigationMetadataInvariants.TargetsNotEmpty)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source, ex.Message, refused: true, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message == NavigationMetadataInvariants.AyahsMissing)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source, ex.Message, refused: true, ct);
        }
        catch (IOException ex)
        {
            return ImportNavigationMetadataResult.Failure(
                $"{NavigationMetadataInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ImportNavigationMetadataResult.Failure(
                $"{NavigationMetadataInvariants.ReportRequired} ({ex.Message})",
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
                result.Errors,
                expectedCounts);

            var writeFailure = await reportEmitter.TryWriteFailureAsync(failureReport, reportDir, ct);
            if (writeFailure is not null)
            {
                return writeFailure;
            }

            return ImportNavigationMetadataResult.Failure(
                result.Errors.Count > 0
                    ? result.Errors[0]
                    : "Navigation metadata import validation failed.",
                reportDir,
                failureReport.Warnings.Count);
        }

        return ImportNavigationMetadataResult.Success(result.Totals, reportDir, successWarningCount);
    }

    private async Task<ImportNavigationMetadataResult> EmitPrePersistenceOutcomeAsync(
        ImportNavigationMetadataCommand command,
        string sourcePath,
        string reportDir,
        NavigationMetadataSourceData? source,
        string message,
        bool refused,
        CancellationToken ct)
    {
        var report = reportBuilder.BuildRefusal(
            sourcePath,
            source,
            command.Force,
            DateTimeOffset.UtcNow,
            message,
            command.ExpectedCounts ?? NavigationMetadataInvariants.Production);

        var writeFailure = await reportEmitter.TryWriteRefusalAsync(report, reportDir, ct);
        if (writeFailure is not null)
        {
            return writeFailure;
        }

        return refused
            ? ImportNavigationMetadataResult.Refused(message, reportDir)
            : ImportNavigationMetadataResult.Failure(message, reportDir);
    }

    private async Task<ImportNavigationMetadataResult> WriteValidationFailureAsync(
        ImportNavigationMetadataCommand command,
        string sourcePath,
        string reportDir,
        NavigationMetadataValidationException ex,
        CancellationToken ct)
    {
        var report = reportBuilder.BuildValidationFailure(
            sourcePath,
            source: null,
            command.Force,
            DateTimeOffset.UtcNow,
            NavigationImportTotals.Empty,
            ex.Checks,
            ex.FailedChecks
                .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
                .ToList(),
            command.ExpectedCounts ?? NavigationMetadataInvariants.Production);

        var writeFailure = await reportEmitter.TryWriteFailureAsync(report, reportDir, ct);
        if (writeFailure is not null)
        {
            return writeFailure;
        }

        var firstFailed = ex.FailedChecks.FirstOrDefault();
        if (firstFailed is null)
        {
            return ImportNavigationMetadataResult.Failure(ex.Message, reportDir);
        }

        return ImportNavigationMetadataResult.Failure(
            $"{firstFailed.Id}: expected {firstFailed.Expected}, observed {firstFailed.Observed}",
            reportDir);
    }

    private static string ResolveReportOutDir(ImportNavigationMetadataCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ReportOutDir))
        {
            throw new InvalidOperationException(
                "A report output directory must be provided by the caller.");
        }

        return Path.GetFullPath(command.ReportOutDir);
    }
}
