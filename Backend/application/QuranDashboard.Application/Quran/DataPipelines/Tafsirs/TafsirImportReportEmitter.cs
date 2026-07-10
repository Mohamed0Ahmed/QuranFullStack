using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Application.Quran.DataPipelines.Tafsirs;

internal sealed class TafsirImportReportEmitter
{
    private readonly ITafsirReportWriter reportWriter;

    public TafsirImportReportEmitter(ITafsirReportWriter reportWriter)
    {
        this.reportWriter = reportWriter;
    }

    public async Task WriteOrThrowAsync(
        TafsirImportReport report,
        string reportDir,
        CancellationToken ct)
    {
        await reportWriter.WriteAsync(report, reportDir, ct);
    }

    public async Task<ImportTafsirsResult?> TryWriteAsync(
        TafsirImportReport report,
        string reportDir,
        CancellationToken ct)
    {
        try
        {
            await reportWriter.WriteAsync(report, reportDir, ct);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ImportTafsirsResult.Failure(
                $"{TafsirInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }
    }
}
