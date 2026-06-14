using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuranDashboard.Application;
using QuranDashboard.Application.Quran.Import.ImportQuranFoundation;
using QuranDashboard.Application.Quran.Mutashabihat.ImportMutashabihat;
using QuranDashboard.Application.Quran.Words.GenerateI3rab;
using QuranDashboard.Application.Quran.Words.ImportMorphology;
using QuranDashboard.Application.Quran.Words.RebuildDisplayWords;
using QuranDashboard.Application.Quran.Tafsirs.ImportTafsirs;
using QuranDashboard.Application.Abstractions.Quran.Tafsirs;
using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;
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

        return verb switch
        {
            "import-foundation" => await RunImportFoundationAsync(verbArgs),
            "rebuild-words" => await RunRebuildWordsAsync(verbArgs),
            "import-morphology" => await RunImportMorphologyAsync(verbArgs),
            "import-mutashabihat" => await RunImportMutashabihatAsync(verbArgs),
            "import-tafsirs" => await RunImportTafsirsAsync(verbArgs),
            "generate-i3rab" => await RunGenerateI3rabAsync(verbArgs),
            _ => UnknownVerb(verb)
        };
    }

    private static async Task<int> RunImportFoundationAsync(string[] args)
    {
        if (!TryParseImportArguments(args, out var sourceRoot, out var reportOutDir, out var force, out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            PrintUsage();
            return ImportQuranFoundationResult.FailureExitCode;
        }

        var host = CreateHost(args);
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportQuranFoundationHandler>();
        var result = await handler.HandleAsync(
            new ImportQuranFoundationCommand(sourceRoot!, reportOutDir, force),
            CancellationToken.None);

        return WriteHandlerResult(result.Succeeded, result.Message, result.ExitCode, () =>
        {
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"Imported surahs={result.Totals.Surahs}, ayahs={result.Totals.Ayahs}, pages={result.Totals.Pages}, lines={result.Totals.Lines}, words={result.Totals.Words}.");
            }
        });
    }

    private static async Task<int> RunRebuildWordsAsync(string[] args)
    {
        if (!TryParseRebuildArguments(args, out var reportOutDir, out var force, out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            PrintUsage();
            return RebuildDisplayWordsResult.FailureExitCode;
        }

        var host = CreateHost(args);
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<RebuildDisplayWordsHandler>();
        var result = await handler.HandleAsync(
            new RebuildDisplayWordsCommand(force, reportOutDir),
            CancellationToken.None);

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"ordered_tashkeel={result.Totals.OrderedTashkeelRows}, ordered_simple={result.Totals.OrderedSimpleRows}, unique_tashkeel={result.Totals.UniqueTashkeelRows}, unique_simple={result.Totals.UniqueSimpleRows}.");
            }

            WriteReportPath(result.ReportOutDir);
            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        WriteReportPath(result.ReportOutDir);
        return result.ExitCode;
    }

    private static void WriteReportPath(string? reportOutDir)
    {
        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            Console.WriteLine($"Report written to: {reportOutDir}");
        }
    }

    private static int UnknownVerb(string verb)
    {
        Console.Error.WriteLine($"Unknown verb '{verb}'.");
        PrintUsage();
        return RebuildDisplayWordsResult.FailureExitCode;
    }

    private static int WriteHandlerResult(
        bool succeeded,
        string message,
        int exitCode,
        Action writeSuccessDetails)
    {
        if (succeeded)
        {
            Console.WriteLine(message);
            writeSuccessDetails();
            return exitCode;
        }

        Console.Error.WriteLine(message);
        return exitCode;
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

    private static bool TryParseImportArguments(
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

    private static bool TryParseRebuildArguments(
        string[] args,
        out string? reportOutDir,
        out bool force,
        out string errorMessage)
    {
        reportOutDir = null;
        force = false;
        errorMessage = string.Empty;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
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

    private static async Task<int> RunImportMutashabihatAsync(string[] args)
    {
        if (!TryParseMutashabihatArguments(args, out var sourcePath, out var reportOutDir, out var force, out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            PrintUsage();
            return ImportMutashabihatResult.FailureExitCode;
        }

        var host = CreateHost(args);
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportMutashabihatHandler>();

        sourcePath ??= ResolveDefaultMutashabihatSourcePath();

        var result = await handler.HandleAsync(
            new ImportMutashabihatCommand(
                sourcePath,
                force,
                MutashabihatInvariants.Production,
                reportOutDir),
            CancellationToken.None);

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"groups={result.Totals.GroupRows}, occurrences={result.Totals.StoredOccurrenceRows}, links={result.Totals.LinkRows}, sources={result.Totals.DistinctSimilarSources}.");
            }

            WriteReportPath(result.ReportOutDir);
            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        WriteReportPath(result.ReportOutDir);
        return result.ExitCode;
    }

    private static string ResolveDefaultMutashabihatSourcePath()
    {
        return Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "import-sources", "mutashabihat"));
    }

    private static bool TryParseMutashabihatArguments(
        string[] args,
        out string? sourcePath,
        out string? reportOutDir,
        out bool force,
        out string errorMessage)
    {
        sourcePath = null;
        reportOutDir = null;
        force = false;
        errorMessage = string.Empty;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--source":
                    if (!TryReadValue(args, ref index, out sourcePath))
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

        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            sourcePath = Path.GetFullPath(sourcePath);
            if (!Directory.Exists(sourcePath))
            {
                errorMessage = $"Source directory was not found: {sourcePath}";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            reportOutDir = Path.GetFullPath(reportOutDir);
        }

        return true;
    }

    private static async Task<int> RunImportTafsirsAsync(string[] args)
    {
        if (!TryParseTafsirArguments(args, out var sourcePath, out var reportOutDir, out var force, out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            PrintUsage();
            return ImportTafsirsResult.FailureExitCode;
        }

        var host = CreateHost(args);
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportTafsirsHandler>();

        sourcePath ??= ResolveDefaultTafsirSourcePath();
        reportOutDir ??= ResolveDefaultTafsirReportDir();

        var result = await handler.HandleAsync(
            new ImportTafsirsCommand(
                sourcePath,
                force,
                TafsirInvariants.Production,
                reportOutDir),
            CancellationToken.None);

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"sources={result.Totals.SourceRows}, ayahMappings={result.Totals.AyahMappingRows}, languages={result.Totals.LanguageCount}, warnings={result.WarningCount}.");
            }

            WriteReportPath(result.ReportOutDir);
            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        WriteReportPath(result.ReportOutDir);
        return result.ExitCode;
    }

    private static string ResolveDefaultTafsirSourcePath()
    {
        return Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "import-sources", "quran-tafsirs"));
    }

    private static string ResolveDefaultTafsirReportDir()
    {
        return Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "report", "quran-tafsirs"));
    }

    private static bool TryParseTafsirArguments(
        string[] args,
        out string? sourcePath,
        out string? reportOutDir,
        out bool force,
        out string errorMessage)
    {
        sourcePath = null;
        reportOutDir = null;
        force = false;
        errorMessage = string.Empty;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--source":
                    if (!TryReadValue(args, ref index, out sourcePath))
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

        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            sourcePath = Path.GetFullPath(sourcePath);
            if (!Directory.Exists(sourcePath))
            {
                errorMessage = $"Source directory was not found: {sourcePath}";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            reportOutDir = Path.GetFullPath(reportOutDir);
        }

        return true;
    }

    private static async Task<int> RunImportMorphologyAsync(string[] args)
    {
        if (!TryParseMorphologyArguments(args, out var sourcePath, out var reportOutDir, out var force, out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            PrintUsage();
            return ImportMorphologyResult.FailureExitCode;
        }

        var host = CreateHost(args);
        await using var scope = host.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportMorphologyHandler>();

        sourcePath ??= ResolveDefaultMorphologySourcePath();

        var result = await handler.HandleAsync(
            new ImportMorphologyCommand(sourcePath, force, ReportOutDir: reportOutDir),
            CancellationToken.None);

        if (result.Succeeded)
        {
            Console.WriteLine(result.Message);
            if (result.Totals is not null)
            {
                Console.WriteLine(
                    $"morphology={result.Totals.MorphologyRows}, segments={result.Totals.SegmentRows}, roots={result.Totals.RootRows}, lemmas={result.Totals.LemmaRows}, stems={result.Totals.StemRows}, pos_tags={result.Totals.PosTagRows}.");
            }

            WriteReportPath(result.ReportOutDir);
            return result.ExitCode;
        }

        Console.Error.WriteLine(result.Message);
        WriteReportPath(result.ReportOutDir);
        return result.ExitCode;
    }

    private static string ResolveDefaultMorphologySourcePath()
    {
        return Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "import-sources", "quran-morphology"));
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var resourcesPath = Path.Combine(directory.FullName, "resources");
            var backendPath = Path.Combine(directory.FullName, "Backend");

            if (Directory.Exists(resourcesPath) && Directory.Exists(backendPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not resolve the repository root directory.");
    }

    private static bool TryParseMorphologyArguments(
        string[] args,
        out string? sourcePath,
        out string? reportOutDir,
        out bool force,
        out string errorMessage)
    {
        sourcePath = null;
        reportOutDir = null;
        force = false;
        errorMessage = string.Empty;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--source":
                    if (!TryReadValue(args, ref index, out sourcePath))
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

        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            sourcePath = Path.GetFullPath(sourcePath);
            if (!Directory.Exists(sourcePath))
            {
                errorMessage = $"Source directory was not found: {sourcePath}";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            reportOutDir = Path.GetFullPath(reportOutDir);
        }

        return true;
    }

    private static async Task<int> RunGenerateI3rabAsync(string[] args)
    {
        if (!TryParseGenerateI3rabArguments(args, out var reportOutDir, out var force, out var errorMessage))
        {
            Console.Error.WriteLine(errorMessage);
            PrintUsage();
            return GenerateI3rabResult.FailureExitCode;
        }

        try
        {
            var host = CreateHost(args);
            await using var scope = host.Services.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<GenerateI3rabHandler>();

            var result = await handler.HandleAsync(
                new GenerateI3rabCommand(force, reportOutDir),
                CancellationToken.None);

            if (result.Succeeded)
            {
                Console.WriteLine(result.Message);
                WriteReportPath(result.ReportPath);
                return result.ExitCode;
            }

            Console.Error.WriteLine(result.Message);
            WriteReportPath(result.ReportPath);
            return result.ExitCode;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return GenerateI3rabResult.FailureExitCode;
        }
    }

    private static bool TryParseGenerateI3rabArguments(
        string[] args,
        out string? reportOutDir,
        out bool force,
        out string errorMessage)
    {
        reportOutDir = null;
        force = false;
        errorMessage = string.Empty;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
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

        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            reportOutDir = Path.GetFullPath(reportOutDir);
        }

        return true;
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
            "  QuranDashboard.DataImporter generate-i3rab [--report-out <path>] [--force]");
    }
}
