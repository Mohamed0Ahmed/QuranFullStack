using QuranDashboard.Application.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;
using QuranDashboard.DataImporter.Import.Safety;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class GenerateI3rabRunner
{
    internal static async Task<int> RunAsync(string[] args, Func<IHost> createHost, Action printUsage)
    {
        if (!ImportArguments.TryParseWithoutSource(args, out var reportOutDir, out var force, out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            printUsage();
            return GenerateI3rabResult.FailureExitCode;
        }

        reportOutDir ??= DataImporterDefaults.ResolveDefaultSimpleI3rabReportDir();

        var gate = DestructiveImportGate.Evaluate(force, sourcePath: null);
        if (!gate.Allowed)
        {
            Console.Error.WriteLine(gate.Reason);
            return GenerateI3rabResult.FailureExitCode;
        }

        try
        {
            var host = createHost();
            await using var scope = host.Services.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<GenerateI3rabHandler>();

            var result = await handler.HandleAsync(
                new GenerateI3rabCommand(force, reportOutDir),
                CancellationToken.None);

            if (result.Succeeded)
            {
                Console.WriteLine(result.Message);
                VerbConsole.WriteReportPath(result.ReportPath);
                return result.ExitCode;
            }

            Console.Error.WriteLine(result.Message);
            VerbConsole.WriteReportPath(result.ReportPath);
            return result.ExitCode;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return GenerateI3rabResult.FailureExitCode;
        }
    }
}
