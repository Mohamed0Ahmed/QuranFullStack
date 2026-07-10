using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;
using QuranDashboard.Application.Quran.DataPipelines.Translations;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class ImportTranslationsRunner
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
            return ImportTranslationsResult.FailureExitCode;
        }

        var host = createHost();
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportTranslationsHandler>();

        sourcePath ??= DataImporterDefaults.ResolveDefaultTranslationSourcePath();
        reportOutDir ??= DataImporterDefaults.ResolveDefaultTranslationReportDir();

        var result = await handler.HandleAsync(
            new ImportTranslationsCommand(
                sourcePath,
                force,
                TranslationInvariants.Production,
                reportOutDir),
            CancellationToken.None);

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"sources={result.Totals.SourceRows}, ayahMappings={result.Totals.AyahMappingRows}, languages={result.Totals.LanguageCount}, types=simple:{result.Totals.SimpleSources},with_footnotes:{result.Totals.WithFootnotesSources}, warnings={result.WarningCount}.");
            }

            if (force)
            {
                Console.WriteLine(
                    "forced=true (translation-owned tables cleared and rebuilt after package validation).");
            }

            VerbConsole.WriteReportPath(result.ReportOutDir);
            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        VerbConsole.WriteReportPath(result.ReportOutDir);
        return result.ExitCode;
    }
}
