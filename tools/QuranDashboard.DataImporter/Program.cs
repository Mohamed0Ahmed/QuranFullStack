using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuranDashboard.Application;
using QuranDashboard.Application.Quran.Import.ImportQuranFoundation;
using QuranDashboard.Infrastructure;

namespace QuranDashboard.DataImporter;

internal static class Program
{
    internal static async Task<int> Main(string[] args)
    {
        if (!TryParseArguments(args, out var sourceRoot, out var reportOutDir, out var force, out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            PrintUsage();
            return ImportQuranFoundationResult.FailureExitCode;
        }

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddApplication();
                services.AddInfrastructure(context.Configuration);
            })
            .Build();

        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportQuranFoundationHandler>();
        var result = await handler.HandleAsync(
            new ImportQuranFoundationCommand(sourceRoot!, reportOutDir, force),
            CancellationToken.None);

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"Imported surahs={result.Totals.Surahs}, ayahs={result.Totals.Ayahs}, pages={result.Totals.Pages}, lines={result.Totals.Lines}, words={result.Totals.Words}.");
            }

            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        return result.ExitCode;
    }

    private static bool TryParseArguments(
        string[] args,
        out string? sourceRoot,
        out string? reportOutDir,
        out bool force,
        out string errorMessage)
    {
        sourceRoot = null;
        reportOutDir = null;
        force = false;
        errorMessage = string.Empty;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--source":
                    if (!TryReadValue(args, ref index, out sourceRoot))
                    {
                        errorMessage = "Missing value for --source.";
                        return false;
                    }

                    break;
                case "--report-out":
                    if (!TryReadValue(args, ref index, out reportOutDir))
                    {
                        errorMessage = "Missing value for --report-out.";
                        return false;
                    }

                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    errorMessage = $"Unknown argument '{args[index]}'.";
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(sourceRoot))
        {
            errorMessage = "--source is required.";
            return false;
        }

        sourceRoot = Path.GetFullPath(sourceRoot);
        if (!Directory.Exists(sourceRoot))
        {
            errorMessage = $"Source directory was not found: {sourceRoot}";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            reportOutDir = Path.GetFullPath(reportOutDir);
        }

        return true;
    }

    private static bool TryReadValue(string[] args, ref int index, out string? value)
    {
        if (index + 1 >= args.Length)
        {
            value = null;
            return false;
        }

        value = args[++index];
        return true;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine(
            "Usage: QuranDashboard.DataImporter --source <path> [--report-out <path>] [--force]");
    }
}
