using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Application.Quran.DataPipelines.FullI3rab;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class ImportFullI3rabRunner
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
            return ImportFullI3rabResult.FailureExitCode;
        }

        var host = createHost();
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportFullI3rabHandler>();

        sourcePath ??= DataImporterDefaults.ResolveDefaultFullI3rabSourcePath();
        reportOutDir ??= DataImporterDefaults.ResolveDefaultFullI3rabReportDir();

        var result = await handler.HandleAsync(
            new ImportFullI3rabCommand(
                sourcePath,
                force,
                FullI3rabInvariants.Production,
                reportOutDir),
            CancellationToken.None);

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"sources={result.Totals.SourceRows}, entries={result.Totals.EntryRows}, ayahMappings={result.Totals.AyahMappingRows}, distinctAyahs={result.Totals.DistinctAyahs}, contentWarnings={result.WarningCount}.");
            }

            if (force)
            {
                Console.WriteLine(
                    "forced=true (full-i'rab tables cleared and rebuilt after package validation).");
            }

            VerbConsole.WriteReportPath(result.ReportOutDir);
            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        VerbConsole.WriteReportPath(result.ReportOutDir);
        return result.ExitCode;
    }
}
