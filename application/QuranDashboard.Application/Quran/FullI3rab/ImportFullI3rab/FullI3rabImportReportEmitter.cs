using QuranDashboard.Application.Abstractions.Quran.FullI3rab;

namespace QuranDashboard.Application.Quran.FullI3rab.ImportFullI3rab;

internal sealed class FullI3rabImportReportEmitter
{
    private readonly IFullI3rabReportWriter reportWriter;

    public FullI3rabImportReportEmitter(IFullI3rabReportWriter reportWriter)
    {
        this.reportWriter = reportWriter;
    }

    public async Task WriteOrThrowAsync(
        FullI3rabImportReport report,
        string reportDir,
        CancellationToken ct)
    {
        await reportWriter.WriteAsync(report, reportDir, ct);
    }

    public async Task<ImportFullI3rabResult?> TryWriteAsync(
        FullI3rabImportReport report,
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
            return ImportFullI3rabResult.Failure(
                $"{FullI3rabInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }
    }
}
