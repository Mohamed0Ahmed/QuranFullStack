using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.DataImporter.Import.DefaultPaths;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

// Dry validation only: never touches the DB (no import/truncate/transaction); run before the real reset+import.
internal static class ValidateEnrichedMorphologyRunner
{
    internal const int SuccessExitCode = 0;
    internal const int ValidationFailureExitCode = 1;

    internal static async Task<int> RunAsync(string[] args, Func<IHost> createHost, Action printUsage)
    {
        string? sourcePath = null;
        string? reportOutDir = null;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--source":
                    if (index + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Missing value for --source.");
                        printUsage();
                        return ValidationFailureExitCode;
                    }
                    sourcePath = args[++index];
                    break;
                case "--report-out":
                    if (index + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Missing value for --report-out.");
                        printUsage();
                        return ValidationFailureExitCode;
                    }
                    reportOutDir = args[++index];
                    break;
                default:
                    Console.Error.WriteLine($"Unknown argument '{args[index]}'.");
                    printUsage();
                    return ValidationFailureExitCode;
            }
        }

        sourcePath ??= DataImporterDefaults.ResolveDefaultEnrichedMorphologySourcePath();
        if (!Directory.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Enriched morphology source directory not found: {sourcePath}");
            return ValidationFailureExitCode;
        }

        var host = createHost();
        await using var scope = host.Services.CreateAsyncScope();
        var manifestReader = scope.ServiceProvider.GetRequiredService<EnrichedMorphologyManifestReader>();
        var sourceReader = scope.ServiceProvider.GetRequiredService<EnrichedMorphologyReader>();
        var dimensionBuilder = scope.ServiceProvider.GetRequiredService<EnrichedDimensionBuilder>();
        var validator = new EnrichedMorphologyDryValidator();

        var importSource = new EnrichedMorphologyImportSource(manifestReader, sourceReader, dimensionBuilder);
        var data = await importSource.LoadAsync(sourcePath, CancellationToken.None);
        var manifest = await manifestReader.ReadAsync(sourcePath, CancellationToken.None);
        var result = validator.Validate(data, manifest);

        WriteReport(result, reportOutDir);

        foreach (var check in result.Checks)
        {
            var mark = check.Passed ? "PASS" : "FAIL";
            Console.WriteLine($"[{mark}] {check.Id}: expected={check.Expected}, observed={check.Observed}");
        }

        Console.WriteLine(result.AllPassed
            ? $"Enriched morphology dry validation PASSED ({result.Checks.Count} checks)."
            : $"Enriched morphology dry validation FAILED ({result.Checks.Count(c => !c.Passed)} of {result.Checks.Count} checks failed).");

        return result.AllPassed ? SuccessExitCode : ValidationFailureExitCode;
    }

    private static void WriteReport(EnrichedMorphologyDryValidationResult result, string? reportOutDir)
    {
        string outDir;
        if (string.IsNullOrWhiteSpace(reportOutDir))
        {
            var repoRoot = DataImporterDefaults.ResolveRepositoryRoot();
            outDir = Path.Combine(
                repoRoot,
                "Backend",
                "report",
                "feature-020-lexical-polish-and-project-hygiene",
                "enriched-morphology-validation");
        }
        else
        {
            outDir = Path.GetFullPath(reportOutDir);
        }

        Directory.CreateDirectory(outDir);

        var runAtUtc = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Enriched Morphology Dry Validation");
        sb.AppendLine();
        sb.AppendLine($"Run at (UTC): {DateTimeOffset.UtcNow:O}");
        sb.AppendLine();
        sb.AppendLine($"Overall verdict: {(result.AllPassed ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine("| Check | Expected | Observed | Result |");
        sb.AppendLine("| --- | --- | --- | --- |");
        foreach (var check in result.Checks)
        {
            sb.AppendLine($"| {check.Id} | {check.Expected} | {check.Observed} | {(check.Passed ? "PASS" : "FAIL")} |");
        }

        File.WriteAllText(Path.Combine(outDir, $"enriched-morphology-dry-validation-{runAtUtc}.md"), sb.ToString());
        VerbConsole.WriteReportPath(outDir);
    }
}
