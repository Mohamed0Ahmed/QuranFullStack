namespace QuranDashboard.Application.Abstractions.Quran.Tafsirs;

public interface ITafsirReportWriter
{
    Task WriteAsync(TafsirImportResult result, string outputDir, CancellationToken ct);
}
