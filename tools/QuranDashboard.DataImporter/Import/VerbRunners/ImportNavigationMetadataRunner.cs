using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Application.Quran.DataPipelines.Navigation;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

/// <summary>
/// Runs the <c>import-navigation-metadata</c> verb. <c>--source</c> is optional and is
/// <strong>not</strong> existence-validated at parse time (the navigation source is
/// validated by the handler against the package contract). Both source and report-out
/// fall back to staged defaults when omitted.
/// </summary>
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
