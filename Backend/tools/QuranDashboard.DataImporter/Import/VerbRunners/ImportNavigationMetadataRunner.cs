using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Application.Quran.DataPipelines.Navigation;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;
using QuranDashboard.DataImporter.Import.Safety;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class ImportNavigationMetadataRunner
{
    internal static async Task<int> RunAsync(string[] args, Func<IHost> createHost, Action printUsage)
    {
        if (!ImportArguments.TryParse(
                args,
                requireSource: false,
                validateSourceExists: false,
                out var sourcePath,
                out var reportOutDir,
                out var force,
                out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            printUsage();
            return ImportNavigationMetadataResult.FailureExitCode;
        }

        sourcePath ??= NavigationImportPaths.ResolveDefaultNavigationSourcePath();
        reportOutDir ??= NavigationImportPaths.ResolveDefaultNavigationReportDir();

        var gate = DestructiveImportGate.Evaluate(force, sourcePath);
        if (!gate.Allowed)
        {
            Console.Error.WriteLine(gate.Reason);
            return ImportNavigationMetadataResult.FailureExitCode;
        }

        if (gate.Warning is not null)
        {
            Console.Error.WriteLine(gate.Warning);
        }

        var host = createHost();
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportNavigationMetadataHandler>();

        var result = await handler.HandleAsync(
            new ImportNavigationMetadataCommand(
                sourcePath,
                force,
                NavigationMetadataInvariants.Production,
                reportOutDir),
            CancellationToken.None);

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"juz={result.Totals.Juz}, hizb={result.Totals.Hizb}, rub={result.Totals.Rub}, sajda={result.Totals.Sajda}, ayahsTagged={result.Totals.AyahsTagged}, warnings={result.WarningCount}.");
            }

            if (force)
            {
                Console.WriteLine(
                    "forced=true (navigation-owned tables cleared and rebuilt after package validation).");
            }

            VerbConsole.WriteReportPath(result.ReportOutDir);
            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        VerbConsole.WriteReportPath(result.ReportOutDir);
        return result.ExitCode;
    }
}
