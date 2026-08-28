using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Application.Quran.DataPipelines.Tafsirs;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class ImportTafsirsRunner
{
    internal static async Task<int> RunAsync(string[] args, Func<IHost> createHost, Action printUsage)
    {
        if (!DataImporterProfileArguments.TryExtract(
                args,
                out var profile,
                out var importArgs,
                out var profileError))
        {
            Console.Error.WriteLine(profileError);
            printUsage();
            return ImportTafsirsResult.FailureExitCode;
        }

        if (!ImportArguments.TryParse(
                importArgs,
                requireSource: false,
                validateSourceExists: true,
                out var sourcePath,
                out var reportOutDir,
                out var force,
                out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            printUsage();
            return ImportTafsirsResult.FailureExitCode;
        }

        var host = createHost();
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportTafsirsHandler>();

        sourcePath ??= DataImporterDefaults.ResolveDefaultTafsirSourcePath(profile);
        reportOutDir ??= DataImporterDefaults.ResolveDefaultTafsirReportDir(profile);
        var expectedCounts = profile == DataImporterProfile.Full
            ? TafsirInvariants.Production
            : TafsirInvariants.CuratedTen;

        var result = await handler.HandleAsync(
            new ImportTafsirsCommand(
                sourcePath,
                force,
                expectedCounts,
                reportOutDir,
                DataImporterProfileArguments.GetValue(profile)),
            CancellationToken.None);

        Console.WriteLine($"profile={DataImporterProfileArguments.GetValue(profile)}.");

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"sources={result.Totals.SourceRows}, ayahMappings={result.Totals.AyahMappingRows}, languages={result.Totals.LanguageCount}, warnings={result.WarningCount}.");
            }

            VerbConsole.WriteReportPath(result.ReportOutDir);
            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        VerbConsole.WriteReportPath(result.ReportOutDir);
        return result.ExitCode;
    }
}
