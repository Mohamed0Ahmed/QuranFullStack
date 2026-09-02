using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.DefaultPaths;
using QuranDashboard.DataImporter.Import.QuranTopicsBook;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class ImportQuranTopicsBookRunner
{
    internal static async Task<int> RunAsync(
        string[] args,
        Func<IHost> createHost,
        Action printUsage)
    {
        if (!QuranTopicsBookImportArguments.TryParse(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            printUsage();
            return 1;
        }

        var runAtUtc = DateTimeOffset.UtcNow;
        var reportDirectory = options.ReportDirectory
            ?? DataImporterDefaults.ResolveDefaultQuranTopicsBookImportReportDir();
        var reader = new QuranTopicsBookSourceReader();
        QuranTopicsBookSourcePackage? package = null;
        string? maskedTarget = null;
        var checks = new List<string>();
        var warnings = new List<string>();
        var errors = new List<string>();
        var verdict = "fail";
        var persisted = "false";

        try
        {
            package = await reader.LoadAsync(options.SourcePath, CancellationToken.None);
            checks.AddRange(package.Checks);
            warnings.AddRange(package.Warnings);
        }
        catch (QuranTopicsBookImportException exception)
        {
            checks.AddRange(exception.Checks);
            warnings.AddRange(exception.Warnings);
            errors.Add(exception.Message);
            return await FinishAsync();
        }
        catch (Exception exception)
        {
            errors.Add($"Quran topics book source load failed ({exception.GetType().Name}).");
            return await FinishAsync();
        }

        IHost host;
        try
        {
            host = createHost();
        }
        catch (Exception exception)
        {
            errors.Add($"Quran topics book import configuration failed ({exception.GetType().Name}).");
            return await FinishAsync();
        }

        using var hostLifetime = host;
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("QuranDashboardDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("ConnectionStrings:QuranDashboardDb is not configured.");
            return await FinishAsync();
        }

        AbwabSnapshotDatabaseTarget target;
        try
        {
            target = AbwabSnapshotDatabaseTargetParser.Parse(connectionString);
            maskedTarget = target.Masked;
        }
        catch (Exception exception)
        {
            errors.Add($"The target connection string is invalid ({exception.GetType().Name}).");
            return await FinishAsync();
        }

        if (!target.IsLoopback && !(options.AllowRemote && options.Confirmed))
        {
            errors.Add("Remote import is refused unless --allow-remote and --yes are supplied together.");
            return await FinishAsync();
        }

        if (!target.IsLoopback)
        {
            warnings.Add("REMOTE-TARGET-AUTHORIZED: --allow-remote and --yes were both supplied.");
        }

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        try
        {
            var compiledMigrationHead = db.Database.GetMigrations().LastOrDefault()
                ?? throw new InvalidOperationException("No compiled EF migrations were found.");
            var databaseMigrationHead = (await db.Database.GetAppliedMigrationsAsync()).LastOrDefault();
            if (!string.Equals(compiledMigrationHead, databaseMigrationHead, StringComparison.Ordinal))
            {
                throw new QuranTopicsBookImportException(
                    $"Target migration head '{databaseMigrationHead ?? "none"}' does not match compiled head '{compiledMigrationHead}'.");
            }

            checks.Add("current-migration-head");
            using var cancellation = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
            };
            Console.CancelKeyPress += cancelHandler;
            try
            {
                var result = await new QuranTopicsBookImporter(db).ImportAsync(
                    package,
                    options.ActorUserId,
                    options.ValidateOnly,
                    token => reader.SourceUnchangedAsync(package, token),
                    cancellation.Token);
                verdict = result.Verdict;
                persisted = result.Persisted;
                checks = result.Checks.ToList();
                warnings = result.Warnings.ToList();
                errors = result.Errors.ToList();
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
        catch (OperationCanceledException)
        {
            errors.Add("Quran topics book import was cancelled before completion.");
        }
        catch (QuranTopicsBookImportException exception)
        {
            checks.AddRange(exception.Checks.Where(check => !checks.Contains(check, StringComparer.Ordinal)));
            warnings.AddRange(exception.Warnings.Where(warning => !warnings.Contains(warning, StringComparer.Ordinal)));
            errors.Add(exception.Message);
        }
        catch (QuranTopicsBookCommitUnknownException exception)
        {
            persisted = "unknown";
            errors.Add(exception.Message);
        }
        catch (Exception exception)
        {
            errors.Add($"Quran topics book database operation failed ({exception.GetType().Name}).");
        }

        return await FinishAsync();

        async Task<int> FinishAsync()
        {
            QuranTopicsBookAuditReport report = new(
                1,
                runAtUtc,
                options.SourcePath,
                package?.Sha256 ?? "unavailable",
                maskedTarget,
                options.ActorUserId,
                options.ValidateOnly,
                verdict,
                persisted,
                package?.Metrics,
                checks.Distinct(StringComparer.Ordinal).ToArray(),
                warnings.Distinct(StringComparer.Ordinal).ToArray(),
                errors.Distinct(StringComparer.Ordinal).ToArray());
            try
            {
                var paths = await QuranTopicsBookReportWriter.WriteAsync(
                    reportDirectory,
                    report,
                    CancellationToken.None);
                Console.WriteLine($"target={maskedTarget ?? "unavailable"}");
                Console.WriteLine($"mode={(options.ValidateOnly ? "validate-only" : "import")}");
                Console.WriteLine($"verdict={verdict}");
                Console.WriteLine($"persisted={persisted}");
                Console.WriteLine($"report_json={paths.Json}");
                Console.WriteLine($"report_markdown={paths.Markdown}");
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Quran topics book report publication failed ({exception.GetType().Name}).");
                return 1;
            }

            foreach (var message in errors)
            {
                Console.Error.WriteLine($"error={message}");
            }

            return verdict == "pass" && (options.ValidateOnly || persisted == "true") ? 0 : 1;
        }
    }
}
