using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Quran.DataPipelines.Foundation;
using QuranDashboard.DataImporter.Import.ArgumentParsing;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

/// <summary>
/// Runs the <c>import-foundation</c> verb. <c>--source</c> is required and must
/// point to an existing directory.
/// </summary>
internal static class ImportFoundationRunner
{
    internal static async Task<int> RunAsync(string[] args, Func<IHost> createHost, Action printUsage)
    {
        if (!ImportArguments.TryParse(
                args,
                requireSource: true,
                validateSourceExists: true,
                out var sourceRoot,
                out var reportOutDir,
                out var force,
                out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            printUsage();
            return ImportQuranFoundationResult.FailureExitCode;
        }

        var host = createHost();
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportQuranFoundationHandler>();
        var result = await handler.HandleAsync(
            new ImportQuranFoundationCommand(sourceRoot!, reportOutDir, force),
            CancellationToken.None);

        return VerbConsole.WriteHandlerResult(result.Succeeded, result.Message, result.ExitCode, () =>
        {
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"Imported surahs={result.Totals.Surahs}, ayahs={result.Totals.Ayahs}, pages={result.Totals.Pages}, lines={result.Totals.Lines}, words={result.Totals.Words}.");
            }
        });
    }
}
