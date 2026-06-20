using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

/// <summary>
/// Runs the <c>import-morphology</c> verb. <c>--source</c> is optional and, when
/// supplied, must exist; a missing source falls back to the staged morphology package.
/// </summary>
internal static class ImportMorphologyRunner
{
    internal static async Task<int> RunAsync(string[] args, Func<IHost> createHost, Action printUsage)
    {
        if (!ImportArguments.TryParse(
                args,
                requireSource: false,
                validateSourceExists: true,
                out var sourcePath,
                out var reportOutDir,
                out var force,
                out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            printUsage();
            return ImportMorphologyResult.FailureExitCode;
        }

        var host = createHost();
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();

        sourcePath ??= DataImporterDefaults.ResolveDefaultMorphologySourcePath();

        var result = await handler.HandleAsync(
            new ImportMorphologyCommand(sourcePath, force, ReportOutDir: reportOutDir),
            CancellationToken.None);

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"morphology={result.Totals.MorphologyRows}, segments={result.Totals.SegmentRows}, roots={result.Totals.RootRows}, lemmas={result.Totals.LemmaRows}, stems={result.Totals.StemRows}, pos_tags={result.Totals.PosTagRows}.");
            }

            VerbConsole.WriteReportPath(result.ReportOutDir);
            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        VerbConsole.WriteReportPath(result.ReportOutDir);
        return result.ExitCode;
    }
}
