using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuranDashboard.Application;
using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.DataImporter.Import.VerbRunners;
using QuranDashboard.Infrastructure;

namespace QuranDashboard.DataImporter;

/// <summary>
/// Thin entry point: parses the leading verb token and dispatches to the matching
/// verb runner in <see cref="Import.VerbRunners"/>. No business logic lives here.
/// </summary>
/// <remarks>
/// The DI host is built lazily (via the <c>createHost</c> delegate) only when a runner
/// reaches the point at which the pre-split code built it — i.e. only after argument
/// parsing has succeeded — preserving the original control flow and exit behavior.
/// </remarks>
internal static class Program
{
    internal static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0].StartsWith('-'))
        {
            PrintUsage();
            return RebuildDisplayWordsResult.FailureExitCode;
        }

        var verb = args[0];
        var verbArgs = args[1..];

        // Built lazily by each runner, only after its argument parse succeeds,
        // exactly mirroring the pre-split per-verb control flow.
        Func<IHost> createHost = () => CreateHost(args);

        return verb switch
        {
            "import-foundation" => await ImportFoundationRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "rebuild-words" => await RebuildDisplayWordsRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-morphology" => await ImportMorphologyRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-mutashabihat" => await ImportMutashabihatRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-tafsirs" => await ImportTafsirsRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-translations" => await ImportTranslationsRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-navigation-metadata" => await ImportNavigationMetadataRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-full-i3rab" => await ImportFullI3rabRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "generate-i3rab" => await GenerateI3rabRunner.RunAsync(verbArgs, createHost, PrintUsage),
            _ => UnknownVerb(verb)
        };
    }

    private static int UnknownVerb(string verb)
    {
        Console.Error.WriteLine($"Unknown verb '{verb}'.");
        PrintUsage();
        return RebuildDisplayWordsResult.FailureExitCode;
    }

    private static IHost CreateHost(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, configuration) =>
            {
                configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                if (context.HostingEnvironment.IsDevelopment())
                {
                    configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
                }

                configuration.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddApplication();
                services.AddInfrastructure(context.Configuration);
            })
            .Build();
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine(
            "Usage:");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-foundation --source <path> [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter rebuild-words [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-morphology [--source <path>] [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-mutashabihat [--source <path>] [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-tafsirs [--source <path>] [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-translations [--source <path>] [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-navigation-metadata [--source <path>] [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-full-i3rab [--source <path>] [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter generate-i3rab [--report-out <path>] [--force]");
    }
}
