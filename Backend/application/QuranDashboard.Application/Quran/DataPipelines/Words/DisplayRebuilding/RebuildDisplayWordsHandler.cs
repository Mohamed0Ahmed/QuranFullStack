using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;

public sealed class RebuildDisplayWordsHandler
{
    private readonly IDisplayWordsRebuilder rebuilder;
    private readonly IDisplayWordsReportWriter reportWriter;

    public RebuildDisplayWordsHandler(
        IDisplayWordsRebuilder rebuilder,
        IDisplayWordsReportWriter reportWriter)
    {
        this.rebuilder = rebuilder;
        this.reportWriter = reportWriter;
    }

    public async Task<RebuildDisplayWordsResult> HandleAsync(
        RebuildDisplayWordsCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.Force && await rebuilder.AnyTargetTableHasDataAsync(ct))
        {
            return RebuildDisplayWordsResult.Refused(DisplayWordsInvariants.TargetsNotEmpty);
        }

        try
        {
            var result = await rebuilder.RebuildAsync(command.Force, command.ExpectedReadableWords, ct);
            var reportDir = ResolveReportOutDir(command);
            await reportWriter.WriteAsync(result, reportDir, ct);

            if (string.Equals(result.Verdict, "pass", StringComparison.Ordinal))
            {
                return result.Warnings.Count == 0
                    ? RebuildDisplayWordsResult.Success(result.Totals, reportDir)
                    : RebuildDisplayWordsResult.SuccessWithWarnings(result.Totals, reportDir);
            }

            return RebuildDisplayWordsResult.Failure(
                    result.Errors.Count > 0
                        ? result.Errors[0]
                        : "Display words rebuild validation failed.",
                    reportDir);
        }
        catch (InvalidOperationException ex) when (ex.Message == DisplayWordsInvariants.TargetsNotEmpty)
        {
            return RebuildDisplayWordsResult.Refused(ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return RebuildDisplayWordsResult.Failure(ex.Message);
        }
    }

    private static string ResolveReportOutDir(RebuildDisplayWordsCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ReportOutDir))
        {
            throw new InvalidOperationException(
                "A report output directory must be provided by the caller.");
        }

        return Path.GetFullPath(command.ReportOutDir);
    }
}
