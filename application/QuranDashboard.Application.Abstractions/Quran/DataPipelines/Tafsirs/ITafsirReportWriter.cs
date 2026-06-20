namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

public interface ITafsirReportWriter
{
    Task WriteAsync(TafsirImportReport report, string outputDir, CancellationToken ct);
}
