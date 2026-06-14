using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Application.Quran.Tafsirs.ImportTafsirs;

/// <summary>
/// Phase 2 handler shell. Full orchestration is implemented in Phase 3 (T030).
/// </summary>
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

    public Task<ImportTafsirsResult> HandleAsync(ImportTafsirsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourcePath);

        _ = (importSource, importWriter, reportWriter, ct);

        throw new NotImplementedException(
            "ImportTafsirsHandler orchestration is implemented in Phase 3 (T030).");
    }
}
