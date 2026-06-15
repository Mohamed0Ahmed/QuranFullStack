using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Application.Quran.Translations.ImportTranslations;

internal sealed class TranslationImportReportEmitter
{
    private readonly ITranslationReportWriter reportWriter;

    public TranslationImportReportEmitter(ITranslationReportWriter reportWriter)
    {
        this.reportWriter = reportWriter;
    }

    public async Task WriteOrThrowAsync(
        TranslationImportReport report,
        string reportDir,
        CancellationToken ct)
    {
        await reportWriter.WriteAsync(report, reportDir, ct);
    }

    public async Task<ImportTranslationsResult?> TryWriteAsync(
        TranslationImportReport report,
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
            return ImportTranslationsResult.Failure(
                $"{TranslationInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }
    }
}
