using QuranDashboard.Application.Abstractions.Quran.FullI3rab;

namespace QuranDashboard.Application.Quran.FullI3rab.ImportFullI3rab;

public sealed class ImportFullI3rabHandler
{
    private readonly IFullI3rabImportSource importSource;
    private readonly IFullI3rabImportWriter importWriter;

    public ImportFullI3rabHandler(
        IFullI3rabImportSource importSource,
        IFullI3rabImportWriter importWriter)
    {
        this.importSource = importSource;
        this.importWriter = importWriter;
    }

    public async Task<ImportFullI3rabResult> HandleAsync(ImportFullI3rabCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourcePath);

        var sourcePath = Path.GetFullPath(command.SourcePath);
        var expectedCounts = command.ExpectedCounts ?? FullI3rabInvariants.Production;
        var reportDir = string.IsNullOrWhiteSpace(command.ReportOutDir)
            ? null
            : Path.GetFullPath(command.ReportOutDir);

        FullI3rabSourceData source;
        try
        {
            source = await importSource.LoadAsync(sourcePath, expectedCounts, ct);
        }
        catch (FullI3rabValidationException ex)
        {
            return ImportFullI3rabResult.Failure(BuildFirstFailureMessage(ex), reportDir);
        }
        catch (FullI3rabSourceException)
        {
            return ImportFullI3rabResult.Refused(FullI3rabInvariants.SourceMismatch, reportDir);
        }
        catch (IOException)
        {
            return ImportFullI3rabResult.Refused(FullI3rabInvariants.SourceMismatch, reportDir);
        }
        catch (InvalidOperationException ex) when (ex.Message == FullI3rabInvariants.AyahsMissing)
        {
            return ImportFullI3rabResult.Refused(ex.Message, reportDir);
        }
        catch (InvalidDataException ex)
        {
            return ImportFullI3rabResult.Failure(ex.Message, reportDir);
        }

        if (!command.Force && await importWriter.AnyTargetTableHasDataAsync(ct))
        {
            return ImportFullI3rabResult.Refused(FullI3rabInvariants.TargetsNotEmpty, reportDir);
        }

        FullI3rabImportResult result;

        try
        {
            result = await importWriter.ExecuteAcceptedImportAsync(
                source,
                command.Force,
                expectedCounts,
                token => importSource.SourceUnchangedAsync(sourcePath, token),
                NoOpReportWriteAsync,
                ct);
        }
        catch (InvalidOperationException ex) when (ex.Message == FullI3rabInvariants.TargetsNotEmpty)
        {
            return ImportFullI3rabResult.Refused(ex.Message, reportDir);
        }

        if (!result.Persisted)
        {
            var message = result.Errors.Count > 0
                ? result.Errors[0]
                : "Full i'rab import validation failed.";

            return ImportFullI3rabResult.Failure(message, reportDir, result.Warnings.Count);
        }

        return ImportFullI3rabResult.Success(result.Totals, reportDir, result.Warnings.Count);
    }

    private static Task NoOpReportWriteAsync(FullI3rabImportResult _, CancellationToken __) => Task.CompletedTask;

    private static string BuildFirstFailureMessage(FullI3rabValidationException ex)
    {
        var firstFailed = ex.FailedChecks.FirstOrDefault();
        return firstFailed is null
            ? ex.Message
            : $"{firstFailed.Id}: expected {firstFailed.Expected}, observed {firstFailed.Observed}";
    }
}
