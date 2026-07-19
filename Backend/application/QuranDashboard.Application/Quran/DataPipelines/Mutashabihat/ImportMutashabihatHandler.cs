using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;

namespace QuranDashboard.Application.Quran.DataPipelines.Mutashabihat;

public sealed class ImportMutashabihatHandler
{
    private readonly IMutashabihatImportSource importSource;
    private readonly IMutashabihatImportWriter importWriter;
    private readonly IMutashabihatReportWriter reportWriter;

    public ImportMutashabihatHandler(
        IMutashabihatImportSource importSource,
        IMutashabihatImportWriter importWriter,
        IMutashabihatReportWriter reportWriter)
    {
        this.importSource = importSource;
        this.importWriter = importWriter;
        this.reportWriter = reportWriter;
    }

    public async Task<ImportMutashabihatResult> HandleAsync(
        ImportMutashabihatCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourcePath);

        MutashabihatSourceData source;
        try
        {
            source = await importSource.LoadAsync(command.SourcePath, ct);
        }
        catch (MutashabihatSourceException)
        {
            return ImportMutashabihatResult.Refused(MutashabihatInvariants.SourceMismatch);
        }
        catch (IOException)
        {
            return ImportMutashabihatResult.Refused(MutashabihatInvariants.SourceMismatch);
        }
        catch (InvalidOperationException ex) when (ex.Message == MutashabihatInvariants.AyahsMissing)
        {
            return ImportMutashabihatResult.Refused(ex.Message);
        }
        catch (InvalidDataException ex)
        {
            return ImportMutashabihatResult.Failure(ex.Message);
        }

        if (!command.Force && await importWriter.AnyTargetTableHasDataAsync(ct))
        {
            return ImportMutashabihatResult.Refused(MutashabihatInvariants.TargetsNotEmpty);
        }

        var result = await importWriter.ImportAsync(
            source,
            command.Force,
            command.ExpectedCounts ?? MutashabihatInvariants.Production,
            token => importSource.SourceUnchangedAsync(command.SourcePath, token),
            ct);

        var reportDir = ResolveReportOutDir(command);
        try
        {
            await reportWriter.WriteAsync(result, reportDir, ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var importSummary = string.Equals(result.Verdict, "pass", StringComparison.Ordinal)
                ? "Mutashabihat import committed successfully"
                : "Mutashabihat import failed validation";

            return ImportMutashabihatResult.Failure(
                $"{importSummary}, but the report could not be written: {ex.Message}");
        }

        return string.Equals(result.Verdict, "pass", StringComparison.Ordinal)
            ? ImportMutashabihatResult.Success(result.Totals, reportDir)
            : ImportMutashabihatResult.Failure(
                result.Errors.Count > 0
                    ? result.Errors[0]
                    : "Mutashabihat import validation failed.",
                reportDir);
    }

    private static string ResolveReportOutDir(ImportMutashabihatCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ReportOutDir))
        {
            throw new InvalidOperationException(
                "A report output directory must be provided by the caller.");
        }

        return Path.GetFullPath(command.ReportOutDir);
    }
}
