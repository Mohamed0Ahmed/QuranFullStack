using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.DataImporter.Import.ArgumentParsing;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

/// <summary>
/// Runs the <c>generate-i3rab</c> verb. Accepts only <c>--report-out</c> and
/// <c>--force</c>. The handler may throw <see cref="InvalidOperationException"/> when
/// the morphology prerequisite is not met; that is surfaced to stderr as a failure.
/// </summary>
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
