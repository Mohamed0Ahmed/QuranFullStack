namespace QuranDashboard.Application.Abstractions.Quran.Translations;

public interface ITranslationReportWriter
{
    Task WriteAsync(TranslationImportReport report, string outputDir, CancellationToken ct);
}
