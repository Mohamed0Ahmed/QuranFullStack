using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Application.Quran.Words.GenerateI3rab;

public sealed class GenerateI3rabHandler(
    II3rabCommandExecutor commandExecutor,
    II3rabGenerationSource generationSource,
    II3rabRuleCatalog ruleCatalog,
    II3rabAssembler assembler,
    II3rabGenerationWriter generationWriter,
    II3rabGenerationReportWriter reportWriter)
{
    public async Task<GenerateI3rabResult> HandleAsync(GenerateI3rabCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ct.ThrowIfCancellationRequested();

        var reportDir = ResolveReportOutDir(command);

        var refusal = commandExecutor.TryRefuse(command.Force, reportDir);
        if (refusal is not null)
        {
            return GenerateI3rabResult.Refused(refusal.Message, refusal.ReportPath);
        }

        var segments = generationSource.LoadSegments();
        var labels = assembler.Assemble(segments);
        var rules = ruleCatalog.Rows();
        var writeResult = await generationWriter.WriteAsync(rules, labels, command.Force, ct);

        var reportPath = reportWriter.Write(writeResult, reportDir);

        var succeeded = writeResult.Persisted && writeResult.Checks.All(check => check.Passed);
        var message = succeeded
            ? $"I‘rab generation completed successfully for {writeResult.SegmentCount:N0} segments ({writeResult.ApprovedCount:N0} approved)."
            : "I‘rab generation failed validation and was rolled back.";

        return succeeded
            ? GenerateI3rabResult.Success(message, reportPath)
            : GenerateI3rabResult.Failure(message, reportPath);
    }

    private static string ResolveReportOutDir(GenerateI3rabCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.ReportOutDir))
        {
            return command.ReportOutDir;
        }

        return Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(),
            "resources",
            "report",
            "words-simple-i3rab"));
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

        return Directory.GetCurrentDirectory();
    }
}
