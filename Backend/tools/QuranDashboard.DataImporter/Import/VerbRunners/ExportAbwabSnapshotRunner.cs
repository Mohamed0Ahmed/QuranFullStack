using Microsoft.Extensions.Configuration;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;
using QuranDashboard.DataImporter.Import.DefaultPaths;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class ExportAbwabSnapshotRunner
{
    internal static async Task<int> RunAsync(
        string[] args,
        Func<IHost> createHost,
        Action printUsage)
    {
        if (!TryParse(args, out var outputDirectory, out var error))
        {
            Console.Error.WriteLine(error);
            printUsage();
            return 1;
        }

        outputDirectory ??= DataImporterDefaults.ResolveDefaultAbwabSnapshotExportDir();
        IHost? host = null;
        string? connectionString;
        try
        {
            host = createHost();
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            connectionString = configuration.GetConnectionString("QuranDashboardDb");
        }
        catch (Exception exception)
        {
            host?.Dispose();
            Console.Error.WriteLine(
                $"Abwab snapshot export configuration failed ({exception.GetType().Name}); no credentials were emitted.");
            return 1;
        }

        using var hostLifetime = host;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("ConnectionStrings:QuranDashboardDb is not configured.");
            return 1;
        }

        string maskedTarget;
        try
        {
            maskedTarget = AbwabSnapshotDatabaseTargetParser.Parse(connectionString).Masked;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"ConnectionStrings:QuranDashboardDb is invalid ({exception.GetType().Name}); no credentials were emitted.");
            return 1;
        }

        Console.WriteLine($"target={maskedTarget}");
        Console.WriteLine("transaction=repeatable-read/read-only");

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        try
        {
            var exportedAtUtc = DateTimeOffset.UtcNow;
            var reader = new AbwabSnapshotDatabaseReader();
            var readResult = await reader.ReadAsync(connectionString, exportedAtUtc, cancellation.Token);
            var validation = AbwabSnapshotValidator.Validate(readResult);
            var paths = AbwabSnapshotArtifactWriter.BuildPaths(outputDirectory, exportedAtUtc);
            if (!validation.Succeeded)
            {
                await AbwabSnapshotArtifactWriter.WriteFailureAuditAsync(
                    readResult.Snapshot,
                    validation,
                    maskedTarget,
                    paths,
                    cancellation.Token);
                Console.Error.WriteLine(
                    "Abwab snapshot validation failed; no snapshot or checksum was written.");
                Console.Error.WriteLine("verdict=fail");
                foreach (var validationError in validation.Errors)
                {
                    Console.Error.WriteLine($"error={validationError}");
                }

                Console.WriteLine($"report_json={paths.JsonReport}");
                Console.WriteLine($"report_markdown={paths.MarkdownReport}");

                return 1;
            }

            var (report, _) = await AbwabSnapshotArtifactWriter.WriteAsync(
                readResult.Snapshot,
                validation,
                maskedTarget,
                paths,
                cancellation.Token);
            Console.WriteLine("verdict=pass");
            Console.WriteLine($"snapshot={paths.Snapshot}");
            Console.WriteLine($"sha256={report.SnapshotSha256}");
            Console.WriteLine($"report_json={paths.JsonReport}");
            Console.WriteLine($"report_markdown={paths.MarkdownReport}");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Abwab snapshot export was cancelled; no complete artifact set was written.");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Abwab snapshot export failed ({exception.GetType().Name}); no credentials were emitted.");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    private static bool TryParse(string[] args, out string? outputDirectory, out string error)
    {
        outputDirectory = null;
        error = string.Empty;
        for (var index = 0; index < args.Length; index++)
        {
            if (!string.Equals(args[index], "--output-dir", StringComparison.Ordinal))
            {
                error = $"Unknown argument '{args[index]}'.";
                return false;
            }

            if (outputDirectory is not null)
            {
                error = "--output-dir may be supplied only once.";
                return false;
            }

            if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]) || args[index].StartsWith('-'))
            {
                error = "Missing value for --output-dir.";
                return false;
            }

            outputDirectory = Path.GetFullPath(args[index]);
        }

        return true;
    }

}
