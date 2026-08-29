using System.Reflection;
using Microsoft.Extensions.Configuration;
using QuranDashboard.Application;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.DataImporter.Import.VerbRunners;
using QuranDashboard.Infrastructure;

namespace QuranDashboard.DataImporter;

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

        Func<IHost> createHost = () => CreateHost(args);

        return verb switch
        {
            "import-foundation" => await ImportFoundationRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "rebuild-words" => await RebuildDisplayWordsRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-morphology" => await ImportMorphologyRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "validate-enriched-morphology" => await ValidateEnrichedMorphologyRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-mutashabihat" => await ImportMutashabihatRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-tafsirs" => await ImportTafsirsRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-translations" => await ImportTranslationsRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-navigation-metadata" => await ImportNavigationMetadataRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-full-i3rab" => await ImportFullI3rabRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "generate-i3rab" => await GenerateI3rabRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "build-phrase-index" => await BuildPhraseIndexRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "export-abwab-snapshot" => await ExportAbwabSnapshotRunner.RunAsync(verbArgs, createHost, PrintUsage),
            "import-abwab-snapshot" => await ImportAbwabSnapshotRunner.RunAsync(verbArgs, createHost, PrintUsage),
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
                services.AddScoped<ICurrentUser, UnavailableCurrentUser>();
            })
            .Build();
    }

    private sealed class UnavailableCurrentUser : ICurrentUser
    {
        public AuthenticatedInteractiveIdentity Identity => throw new InvalidOperationException(
            "The DataImporter has no authenticated interactive user context.");
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
            "  QuranDashboard.DataImporter import-morphology [--source <path>] [--report-out <path>] [--force] [--enriched]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter validate-enriched-morphology [--source <path>] [--report-out <path>]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-mutashabihat [--source <path>] [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-tafsirs [--profile <curated-10|full>] [--source <path>] [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-translations [--profile <curated-10|full>] [--source <path>] [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-navigation-metadata [--source <path>] [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-full-i3rab [--source <path>] [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter generate-i3rab [--report-out <path>] [--force]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter build-phrase-index [--report-out <path>]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter export-abwab-snapshot [--output-dir <path>]");
        Console.Error.WriteLine(
            "  QuranDashboard.DataImporter import-abwab-snapshot --source <snapshot-v4.json> [--report-out <path>] [--allow-remote --yes]");
    }
}
