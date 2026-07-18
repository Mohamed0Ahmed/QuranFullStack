using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class RebuildDisplayWordsRunner
{
    internal static async Task<int> RunAsync(string[] args, Func<IHost> createHost, Action printUsage)
    {
        if (!ImportArguments.TryParseWithoutSource(args, out var reportOutDir, out var force, out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            printUsage();
            return RebuildDisplayWordsResult.FailureExitCode;
        }

        reportOutDir ??= DataImporterDefaults.ResolveDefaultDisplayWordsReportDir();

        var host = createHost();
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<RebuildDisplayWordsHandler>();
        var result = await handler.HandleAsync(
            new RebuildDisplayWordsCommand(force, reportOutDir),
            CancellationToken.None);

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"ordered_tashkeel={result.Totals.OrderedTashkeelRows}, ordered_simple={result.Totals.OrderedSimpleRows}, unique_tashkeel={result.Totals.UniqueTashkeelRows}, unique_simple={result.Totals.UniqueSimpleRows}.");
            }

            VerbConsole.WriteReportPath(result.ReportOutDir);
            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        VerbConsole.WriteReportPath(result.ReportOutDir);
        return result.ExitCode;
    }
}
