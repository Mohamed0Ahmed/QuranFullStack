using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

namespace QuranDashboard.Application.Quran.DataPipelines.Translations;

public sealed class ImportTranslationsHandler
{
    private readonly ITranslationImportSource importSource;
    private readonly ITranslationImportWriter importWriter;
    private readonly ITranslationImportReportBuilder reportBuilder;
    private readonly TranslationImportReportEmitter reportEmitter;

    public ImportTranslationsHandler(
        ITranslationImportSource importSource,
        ITranslationImportWriter importWriter,
        ITranslationReportWriter reportWriter,
        ITranslationImportReportBuilder reportBuilder)
    {
        this.importSource = importSource;
        this.importWriter = importWriter;
        this.reportBuilder = reportBuilder;
        this.reportEmitter = new TranslationImportReportEmitter(reportWriter);
    }

    public async Task<ImportTranslationsResult> HandleAsync(ImportTranslationsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourcePath);
        if (!QuranImportProfiles.IsSupported(command.Profile))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command.Profile),
                command.Profile,
                "Unsupported translation import profile.");
        }

        var sourcePath = Path.GetFullPath(command.SourcePath);
        var expectedCounts = command.ExpectedCounts ?? TranslationInvariants.Production;
        var reportDir = ResolveReportOutDir(command);

        TranslationSourceData source;
        try
        {
            source = await importSource.LoadAsync(sourcePath, expectedCounts, ct);
        }
        catch (TranslationValidationException ex)
        {
            return await WriteValidationFailureAsync(command, sourcePath, reportDir, ex, ct);
        }
        catch (TranslationSourceException)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, TranslationInvariants.SourceMismatch, refused: true, ct);
        }
        catch (JsonException)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, TranslationInvariants.SourceMismatch, refused: true, ct);
        }
        catch (IOException)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source: null, TranslationInvariants.SourceMismatch, refused: true, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message == TranslationInvariants.AyahsMissing)
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
                command, sourcePath, reportDir, source, TranslationInvariants.TargetsNotEmpty, refused: true, ct);
        }

        var successWarningCount = 0;
        TranslationImportResult result;

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
                    await reportEmitter.WriteSuccessAsync(report, reportDir, token);
                },
                ct);
        }
        catch (InvalidOperationException ex) when (ex.Message == TranslationInvariants.TargetsNotEmpty)
        {
            return await EmitPrePersistenceOutcomeAsync(
                command, sourcePath, reportDir, source, ex.Message, refused: true, ct);
        }
        catch (IOException ex)
        {
            return ImportTranslationsResult.Failure(
                $"{TranslationInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ImportTranslationsResult.Failure(
                $"{TranslationInvariants.ReportRequired} ({ex.Message})",
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

            var writeFailure = await reportEmitter.TryWriteFailureAsync(failureReport, reportDir, ct);
            if (writeFailure is not null)
            {
                return writeFailure;
            }

            return ImportTranslationsResult.Failure(
                result.Errors.Count > 0
                    ? result.Errors[0]
                    : "Translation import validation failed.",
                reportDir,
                failureReport.Warnings.Count);
        }

        return ImportTranslationsResult.Success(result.Totals, reportDir, successWarningCount);
    }

    private async Task<ImportTranslationsResult> EmitPrePersistenceOutcomeAsync(
        ImportTranslationsCommand command,
        string sourcePath,
        string reportDir,
        TranslationSourceData? source,
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

        var writeFailure = await reportEmitter.TryWriteRefusalAsync(report, reportDir, ct);
        if (writeFailure is not null)
        {
            return writeFailure;
        }

        return refused
            ? ImportTranslationsResult.Refused(message, reportDir)
            : ImportTranslationsResult.Failure(message, reportDir);
    }

    private async Task<ImportTranslationsResult> WriteValidationFailureAsync(
        ImportTranslationsCommand command,
        string sourcePath,
        string reportDir,
        TranslationValidationException ex,
        CancellationToken ct)
    {
        var report = reportBuilder.BuildValidationFailure(
            sourcePath,
            command.Profile,
            source: null,
            command.Force,
            DateTimeOffset.UtcNow,
            TranslationImportTotals.Empty,
            ex.Checks,
            ex.FailedChecks
                .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
                .ToList());

        var writeFailure = await reportEmitter.TryWriteFailureAsync(report, reportDir, ct);
        if (writeFailure is not null)
        {
            return writeFailure;
        }

        var firstFailed = ex.FailedChecks.FirstOrDefault();
        if (firstFailed is null)
        {
            return ImportTranslationsResult.Failure(ex.Message, reportDir);
        }

        return ImportTranslationsResult.Failure(
            $"{firstFailed.Id}: expected {firstFailed.Expected}, observed {firstFailed.Observed}",
            reportDir);
    }

    private static string ResolveReportOutDir(ImportTranslationsCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ReportOutDir))
        {
            throw new InvalidOperationException(
                "A report output directory must be provided by the caller.");
        }

        return Path.GetFullPath(command.ReportOutDir);
    }
}
