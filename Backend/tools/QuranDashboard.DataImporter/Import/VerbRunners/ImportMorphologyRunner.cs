using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;
using QuranDashboard.DataImporter.Import.Safety;
using QuranDashboard.Infrastructure.ServiceRegistration;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class ImportMorphologyRunner
{
    internal static async Task<int> RunAsync(string[] args, Func<IHost> createHost, Action printUsage)
    {
        var enrichedRequested = false;
        var remaining = new List<string>(args.Length);
        foreach (var arg in args)
        {
            if (string.Equals(arg, "--enriched", StringComparison.Ordinal))
            {
                enrichedRequested = true;
            }
            else
            {
                remaining.Add(arg);
            }
        }

        if (!ImportArguments.TryParse(
                remaining.ToArray(),
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

        sourcePath ??= enrichedRequested
            ? DataImporterDefaults.ResolveDefaultEnrichedMorphologySourcePath()
            : DataImporterDefaults.ResolveDefaultMorphologySourcePath();
        reportOutDir ??= DataImporterDefaults.ResolveDefaultMorphologyReportDir();

        var gate = DestructiveImportGate.Evaluate(force, sourcePath);
        if (!gate.Allowed)
        {
            Console.Error.WriteLine(gate.Reason);
            return ImportMorphologyResult.FailureExitCode;
        }

        if (gate.Warning is not null)
        {
            Console.Error.WriteLine(gate.Warning);
        }

        var host = createHost();
        await using var scope = host.Services.CreateAsyncScope();

        var sourceKey = enrichedRequested
            ? MorphologyImportSourceKeys.Enriched
            : MorphologyImportSourceKeys.Legacy;
        var importSource = scope.ServiceProvider.GetRequiredKeyedService<IMorphologyImportSource>(sourceKey);
        var importWriter = scope.ServiceProvider.GetRequiredService<IMorphologyImportWriter>();
        var reportWriter = scope.ServiceProvider.GetRequiredService<IMorphologyReportWriter>();
        var handler = new ImportMorphologyHandler(importSource, importWriter, reportWriter);

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
