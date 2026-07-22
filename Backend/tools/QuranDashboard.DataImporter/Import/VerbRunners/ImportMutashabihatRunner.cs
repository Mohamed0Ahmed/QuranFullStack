using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Application.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;
using QuranDashboard.DataImporter.Import.Safety;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class ImportMutashabihatRunner
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
            return ImportMutashabihatResult.FailureExitCode;
        }

        var host = createHost();
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportMutashabihatHandler>();

        sourcePath ??= DataImporterDefaults.ResolveDefaultMutashabihatSourcePath();
        reportOutDir ??= DataImporterDefaults.ResolveDefaultMutashabihatReportDir();

        var gate = DestructiveImportGate.Evaluate(force, sourcePath);
        if (!gate.Allowed)
        {
            Console.Error.WriteLine(gate.Reason);
            return ImportMutashabihatResult.FailureExitCode;
        }

        if (gate.Warning is not null)
        {
            Console.Error.WriteLine(gate.Warning);
        }

        var result = await handler.HandleAsync(
            new ImportMutashabihatCommand(
                sourcePath,
                force,
                MutashabihatInvariants.Production,
                reportOutDir),
            CancellationToken.None);

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"groups={result.Totals.GroupRows}, occurrences={result.Totals.StoredOccurrenceRows}, links={result.Totals.LinkRows}, sources={result.Totals.DistinctSimilarSources}.");
            }

            VerbConsole.WriteReportPath(result.ReportOutDir);
            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        VerbConsole.WriteReportPath(result.ReportOutDir);
        return result.ExitCode;
    }
}
