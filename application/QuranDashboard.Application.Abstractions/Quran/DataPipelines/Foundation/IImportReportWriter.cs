namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

public interface IImportReportWriter
{
    Task WriteAsync(QuranImportValidationResult result, string outputDir, CancellationToken ct);
}
