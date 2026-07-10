namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

public interface ITranslationReportWriter
{
    Task WriteAsync(TranslationImportReport report, string outputDir, CancellationToken ct);
}
