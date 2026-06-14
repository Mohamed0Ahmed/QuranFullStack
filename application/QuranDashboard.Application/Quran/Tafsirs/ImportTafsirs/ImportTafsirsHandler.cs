using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Application.Quran.Tafsirs.ImportTafsirs;

public sealed class ImportTafsirsHandler
{
    private readonly ITafsirImportSource importSource;
    private readonly ITafsirImportWriter importWriter;
    private readonly ITafsirReportWriter reportWriter;

    public ImportTafsirsHandler(
        ITafsirImportSource importSource,
        ITafsirImportWriter importWriter,
        ITafsirReportWriter reportWriter)
    {
        this.importSource = importSource;
        this.importWriter = importWriter;
        this.reportWriter = reportWriter;
    }

    public async Task<ImportTafsirsResult> HandleAsync(ImportTafsirsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourcePath);

        TafsirSourceData source;
        try
        {
            source = await importSource.LoadAsync(command.SourcePath, ct);
        }
        catch (TafsirSourceException)
        {
            return ImportTafsirsResult.Refused(TafsirInvariants.SourceMismatch);
        }
        catch (IOException)
        {
            return ImportTafsirsResult.Refused(TafsirInvariants.SourceMismatch);
        }
        catch (InvalidOperationException ex) when (ex.Message == TafsirInvariants.AyahsMissing)
        {
            return ImportTafsirsResult.Refused(ex.Message);
        }
        catch (InvalidDataException ex)
        {
            return ImportTafsirsResult.Failure(ex.Message);
        }

        if (!command.Force && await importWriter.AnyTargetTableHasDataAsync(ct))
        {
            return ImportTafsirsResult.Refused(TafsirInvariants.TargetsNotEmpty);
        }

        var reportDir = ResolveReportOutDir(command);
        TafsirImportResult result;

        try
        {
            result = await importWriter.ExecuteAcceptedImportAsync(
                source,
                command.Force,
                command.ExpectedCounts ?? TafsirInvariants.Production,
                token => importSource.SourceUnchangedAsync(command.SourcePath, token),
                async (candidateResult, token) =>
                {
                    Directory.CreateDirectory(reportDir);
                    await reportWriter.WriteAsync(candidateResult, reportDir, token);
                },
                ct);
        }
        catch (InvalidOperationException ex) when (ex.Message == TafsirInvariants.TargetsNotEmpty)
        {
            return ImportTafsirsResult.Refused(ex.Message);
        }
        catch (IOException ex)
        {
            return ImportTafsirsResult.Failure(
                $"{TafsirInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ImportTafsirsResult.Failure(
                $"{TafsirInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }

        if (!result.Persisted)
        {
            try
            {
                Directory.CreateDirectory(reportDir);
                await reportWriter.WriteAsync(result, reportDir, ct);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return ImportTafsirsResult.Failure(
                    $"{TafsirInvariants.ReportRequired} ({ex.Message})",
                    reportDir);
            }

            return ImportTafsirsResult.Failure(
                result.Errors.Count > 0
                    ? result.Errors[0]
                    : "Tafsir import validation failed.",
                reportDir);
        }

        return ImportTafsirsResult.Success(result.Totals, reportDir);
    }

    private static string ResolveReportOutDir(ImportTafsirsCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.ReportOutDir))
        {
            return Path.GetFullPath(command.ReportOutDir);
        }

        return Path.GetFullPath(Path.Combine(ResolveRepositoryRoot(), "resources", "report", "quran-tafsirs"));
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
}
