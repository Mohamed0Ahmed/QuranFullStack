namespace QuranDashboard.Application.Abstractions.Quran.Tafsirs;

public interface ITafsirReportWriter
{
    Task WriteAsync(TafsirImportReport report, string outputDir, CancellationToken ct);
}
