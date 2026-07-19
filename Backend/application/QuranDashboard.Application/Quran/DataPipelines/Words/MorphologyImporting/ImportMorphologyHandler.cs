using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;

public sealed class ImportMorphologyHandler
{
    private readonly IMorphologyImportSource importSource;
    private readonly IMorphologyImportWriter importWriter;
    private readonly IMorphologyReportWriter reportWriter;

    public ImportMorphologyHandler(
        IMorphologyImportSource importSource,
        IMorphologyImportWriter importWriter,
        IMorphologyReportWriter reportWriter)
    {
        this.importSource = importSource;
        this.importWriter = importWriter;
        this.reportWriter = reportWriter;
    }

    public async Task<ImportMorphologyResult> HandleAsync(
        ImportMorphologyCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourcePath);

        try
        {
            var source = await importSource.LoadAsync(command.SourcePath, ct);

            if (!command.Force && await importWriter.AnyTargetTableHasDataAsync(ct))
            {
                return ImportMorphologyResult.Refused(MorphologyInvariants.TargetsNotEmpty);
            }

            var result = await importWriter.ImportAsync(
                source,
                command.Force,
                command.ExpectedReadableWords,
                token => importSource.SourceUnchangedAsync(command.SourcePath, token),
                ct);

            var reportDir = ResolveReportOutDir(command);
            await reportWriter.WriteAsync(result, reportDir, ct);

            return string.Equals(result.Verdict, "pass", StringComparison.Ordinal)
                ? ImportMorphologyResult.Success(result.Totals, reportDir)
                : ImportMorphologyResult.Failure(
                    result.Errors.Count > 0
                        ? result.Errors[0]
                        : "Morphology import validation failed.",
                    reportDir);
        }
        catch (InvalidOperationException ex) when (
            ex.Message == MorphologyInvariants.TargetsNotEmpty
            || ex.Message == MorphologyInvariants.FoundationNotLoaded)
        {
            return ImportMorphologyResult.Refused(ex.Message);
        }
        catch (InvalidDataException ex)
        {
            return ImportMorphologyResult.Failure(ex.Message);
        }
        catch (Exception ex) when (ex is FileNotFoundException or IOException)
        {
            return ImportMorphologyResult.Refused(MorphologyInvariants.SourceMismatch);
        }
    }

    private static string ResolveReportOutDir(ImportMorphologyCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ReportOutDir))
        {
            throw new InvalidOperationException(
                "A report output directory must be provided by the caller.");
        }

        return Path.GetFullPath(command.ReportOutDir);
    }
}
