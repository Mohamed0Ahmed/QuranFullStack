using QuranDashboard.Application.Abstractions.Quran.Words.Display;

namespace QuranDashboard.Application.Quran.Words.RebuildDisplayWords;

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
            Directory.CreateDirectory(reportDir);
            await reportWriter.WriteAsync(result, reportDir, ct);

            return string.Equals(result.Verdict, "pass", StringComparison.Ordinal)
                ? RebuildDisplayWordsResult.Success(result.Totals, reportDir)
                : RebuildDisplayWordsResult.Failure(
                    result.Errors.Count > 0
                        ? result.Errors[0]
                        : "Display words rebuild validation failed.",
                    reportDir);
        }
        catch (Exception ex)
        {
            return RebuildDisplayWordsResult.Failure(ex.Message);
        }
    }

    private static string ResolveReportOutDir(RebuildDisplayWordsCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.ReportOutDir))
        {
            return Path.GetFullPath(command.ReportOutDir);
        }

        return Path.GetFullPath(Path.Combine(ResolveRepositoryRoot(), "resources", "report", "words-display"));
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var resourcesPath = Path.Combine(directory.FullName, "resources");
            var backendPath = Path.Combine(directory.FullName, "Backend");

            if (Directory.Exists(resourcesPath) && Directory.Exists(backendPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not resolve the repository root directory.");
    }
}
