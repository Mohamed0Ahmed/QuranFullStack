using QuranDashboard.Application.Quran.DataPipelines.PhraseSearch;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class BuildPhraseIndexRunner
{
    internal static async Task<int> RunAsync(
        string[] args,
        Func<IHost> createHost,
        Action printUsage)
    {
        if (!ImportArguments.TryParseWithoutSource(
                args,
                out var reportOutDir,
                out var force,
                out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            printUsage();
            return BuildPhraseIndexResult.FailureExitCode;
        }

        reportOutDir ??= DataImporterDefaults.ResolveDefaultPhraseIndexReportDir();
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            var host = createHost();
            await using var scope = host.Services.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<BuildPhraseIndexHandler>();
            var result = await handler.HandleAsync(
                new BuildPhraseIndexCommand(force, reportOutDir),
                cancellation.Token);
            var output = result.Succeeded ? Console.Out : Console.Error;
            output.WriteLine(result.Message);
            output.WriteLine($"build_id={result.BuildId}");
            output.WriteLine($"source_revision={result.SourceRevision}");
            output.WriteLine($"source_fingerprint={result.SourceFingerprint}");
            output.WriteLine(
                $"variants={result.Totals.Variants}, occurrences={result.Totals.Occurrences}, "
                + $"edges={result.Totals.SimilarityEdges}, anchor_stats={result.Totals.SimilarityAnchorStats}");
            VerbConsole.WriteReportPath(result.ReportDirectory);
            return result.ExitCode;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }
}
