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

        if (force)
        {
            Console.Error.WriteLine(
                "build-phrase-index does not support --force or replacement builds. "
                + "Full database reset is required before rebuilding an existing PhraseSearch generation.");
            printUsage();
            return BuildPhraseIndexResult.RefusedExitCode;
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
                new BuildPhraseIndexCommand(reportOutDir),
                cancellation.Token);
            var output = result.Succeeded ? Console.Out : Console.Error;
            output.WriteLine(result.Message);
            output.WriteLine($"outcome={result.Outcome}");
            output.WriteLine($"build_id={result.BuildId}");
            output.WriteLine($"active_build_id={result.ActiveBuildId?.ToString() ?? "none"}");
            output.WriteLine($"source_revision={result.SourceRevision}");
            output.WriteLine($"source_fingerprint={result.SourceFingerprint}");
            output.WriteLine($"report_available={result.ReportAvailable.ToString().ToLowerInvariant()}");
            output.WriteLine($"report_linked={result.ReportLinked.ToString().ToLowerInvariant()}");
            output.WriteLine(
                $"variants={result.Totals.Variants}, occurrences={result.Totals.Occurrences}, "
                + $"edges={result.Totals.SimilarityEdges}, anchor_stats={result.Totals.SimilarityAnchorStats}");
            if (result.ReportAvailable)
            {
                output.WriteLine($"Report written to: {result.ReportDirectory}");
            }
            else
            {
                output.WriteLine($"Report unavailable; target directory: {result.ReportDirectory}");
            }

            return result.ExitCode;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }
}
