namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;

public interface IFullI3rabReportWriter
{
    Task WriteAsync(FullI3rabImportReport report, string outputDir, CancellationToken ct);
}
