using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuranDashboard.DataImporter.Import.ArgumentParsing;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;
using QuranDashboard.DataImporter.Import.AbwabSnapshotImport;
using QuranDashboard.DataImporter.Import.DefaultPaths;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class ImportAbwabSnapshotRunner
{
    internal static async Task<int> RunAsync(
        string[] args,
        Func<IHost> createHost,
        Action printUsage)
    {
        if (!AbwabSnapshotImportArguments.TryParse(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            printUsage();
            return 1;
        }

        var runAtUtc = DateTimeOffset.UtcNow;
        var reportDirectory = options.ReportDirectory
            ?? DataImporterDefaults.ResolveDefaultAbwabSnapshotImportReportDir();
        var reportPaths = AbwabSnapshotImportReportWriter.BuildPaths(reportDirectory, runAtUtc);
        var sourceReader = new AbwabSnapshotSourceReader();
        AbwabSnapshotSourcePackage? package = null;
        string? maskedTarget = null;
        string? targetMigrationHead = null;
        var warnings = new List<string>();

        try
        {
            package = await sourceReader.LoadAsync(options.SourcePath, CancellationToken.None);
            warnings.AddRange(package.Warnings);
        }
        catch (AbwabSnapshotImportException exception)
        {
            await AbwabSnapshotImportReportFactory.WriteFailureAsync(
                runAtUtc,
                options.SourcePath,
                package,
                maskedTarget,
                targetMigrationHead,
                exception.Checks,
                exception.Warnings,
                exception.Message,
                reportPaths);
            return 1;
        }
        catch (Exception exception)
        {
            await AbwabSnapshotImportReportFactory.WriteFailureAsync(
                runAtUtc,
                options.SourcePath,
                package,
                maskedTarget,
                targetMigrationHead,
                [],
                warnings,
                $"Abwab snapshot source load failed ({exception.GetType().Name}).",
                reportPaths);
            return 1;
        }

        IHost? host = null;
        string? connectionString;
        string compiledMigrationHead;
        try
        {
            host = createHost();
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            connectionString = configuration.GetConnectionString("QuranDashboardDb");
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            compiledMigrationHead = dbContext.Database.GetMigrations().LastOrDefault()
                ?? throw new InvalidOperationException("No compiled EF migrations were found.");
        }
        catch (Exception exception)
        {
            host?.Dispose();
            await AbwabSnapshotImportReportFactory.WriteFailureAsync(
                runAtUtc,
                package.SourcePath,
                package,
                maskedTarget,
                targetMigrationHead,
                package.Checks,
                warnings,
                $"Abwab snapshot import configuration failed ({exception.GetType().Name}).",
                reportPaths);
            return 1;
        }

        using var hostLifetime = host;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            await AbwabSnapshotImportReportFactory.WriteFailureAsync(
                runAtUtc,
                package.SourcePath,
                package,
                maskedTarget,
                targetMigrationHead,
                package.Checks,
                warnings,
                "ConnectionStrings:QuranDashboardDb is not configured.",
                reportPaths);
            return 1;
        }

        AbwabSnapshotDatabaseTarget target;
        try
        {
            target = AbwabSnapshotDatabaseTargetParser.Parse(connectionString);
            maskedTarget = target.Masked;
        }
        catch (Exception exception)
        {
            await AbwabSnapshotImportReportFactory.WriteFailureAsync(
                runAtUtc,
                package.SourcePath,
                package,
                maskedTarget,
                targetMigrationHead,
                package.Checks,
                warnings,
                $"The target connection string is invalid ({exception.GetType().Name}).",
                reportPaths);
            return 1;
        }

        if (!target.IsLoopback && !(options.AllowRemote && options.Confirmed))
        {
            await AbwabSnapshotImportReportFactory.WriteFailureAsync(
                runAtUtc,
                package.SourcePath,
                package,
                maskedTarget,
                targetMigrationHead,
                package.Checks,
                warnings,
                "Remote Abwab import is refused unless --allow-remote and --yes are supplied together.",
                reportPaths);
            return 1;
        }

        if (!target.IsLoopback)
        {
            warnings.Add("REMOTE-TARGET-AUTHORIZED: --allow-remote and --yes were both supplied.");
        }

        if (!string.Equals(package.Snapshot.Source.MigrationHead, compiledMigrationHead, StringComparison.Ordinal))
        {
            warnings.Add(
                $"SOURCE-MIGRATION-HEAD-DIFFERS: snapshot={package.Snapshot.Source.MigrationHead}, current={compiledMigrationHead}; exact Abwab schema matching remains mandatory.");
        }

        Console.WriteLine($"target={maskedTarget}");
        Console.WriteLine("transaction=serializable/access-exclusive-fenced");
        Console.WriteLine($"remote_authorized={(!target.IsLoopback).ToString().ToLowerInvariant()}");

        AbwabSnapshotImportReportReservation reservation;
        try
        {
            var candidate = AbwabSnapshotImportReportFactory.BuildCandidate(
                runAtUtc,
                package,
                maskedTarget,
                warnings);
            reservation = await AbwabSnapshotImportReportWriter.ReserveAsync(
                candidate,
                reportPaths,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Abwab snapshot import report reservation failed before database mutation ({exception.GetType().Name}).");
            return 1;
        }

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        AbwabSnapshotImportExecution execution;
        try
        {
            var importer = new AbwabSnapshotImporter();
            execution = await importer.ImportAsync(
                connectionString,
                package,
                compiledMigrationHead,
                token => sourceReader.SourceUnchangedAsync(package, token),
                cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            execution = new AbwabSnapshotImportExecution(
                AbwabSnapshotImportContract.FailVerdict,
                AbwabSnapshotImportContract.PersistedFalse,
                null,
                [],
                [],
                ["Abwab snapshot import was cancelled before commit; no commit was attempted."]);
        }
        catch (AbwabSnapshotImportException exception)
        {
            execution = new AbwabSnapshotImportExecution(
                AbwabSnapshotImportContract.FailVerdict,
                AbwabSnapshotImportContract.PersistedFalse,
                null,
                exception.Checks,
                exception.Warnings,
                [exception.Message]);
        }
        catch (Exception exception)
        {
            execution = new AbwabSnapshotImportExecution(
                AbwabSnapshotImportContract.FailVerdict,
                AbwabSnapshotImportContract.PersistedFalse,
                null,
                [],
                [],
                [$"Abwab snapshot database import failed before commit ({exception.GetType().Name}); no commit was attempted."]);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        var finalReport = AbwabSnapshotImportReportFactory.BuildExecution(
            runAtUtc,
            package,
            maskedTarget,
            execution,
            warnings);
        try
        {
            await AbwabSnapshotImportReportWriter.FinalizeAsync(
                reservation,
                finalReport,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Final Abwab import report publication failed ({exception.GetType().Name}); the staged candidate remains persisted=unknown.");
            Console.Error.WriteLine($"staged_report_json={reservation.StagingJson}");
            Console.Error.WriteLine($"staged_report_markdown={reservation.StagingMarkdown}");
            return 1;
        }

        foreach (var errorMessage in execution.Errors)
        {
            Console.Error.WriteLine($"error={errorMessage}");
        }

        Console.WriteLine($"verdict={execution.Verdict}");
        Console.WriteLine($"persisted={execution.Persisted}");
        Console.WriteLine($"report_json={reportPaths.Json}");
        Console.WriteLine($"report_markdown={reportPaths.Markdown}");
        return execution.Persisted == AbwabSnapshotImportContract.PersistedTrue
            && execution.Verdict == AbwabSnapshotImportContract.PassVerdict
            ? 0
            : 1;
    }

}
