namespace QuranDashboard.Application.Abstractions.Quran.Import;

public interface IImportReportWriter
{
    Task WriteAsync(QuranImportValidationResult result, string outputDir, CancellationToken ct);
}
