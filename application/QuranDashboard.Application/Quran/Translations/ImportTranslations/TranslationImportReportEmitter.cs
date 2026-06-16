using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Application.Quran.Translations.ImportTranslations;

internal sealed class TranslationImportReportEmitter
{
    private readonly ITranslationReportWriter reportWriter;

    public TranslationImportReportEmitter(ITranslationReportWriter reportWriter)
    {
        this.reportWriter = reportWriter;
    }

    public async Task WriteSuccessAsync(
        TranslationImportReport report,
        string reportDir,
        CancellationToken ct)
    {
        await WriteOrThrowAsync(report, reportDir, ct);
    }

    public async Task<ImportTranslationsResult?> TryWriteFailureAsync(
        TranslationImportReport report,
        string reportDir,
        CancellationToken ct) =>
        await TryWriteAsync(report, reportDir, ct);

    public async Task<ImportTranslationsResult?> TryWriteRefusalAsync(
        TranslationImportReport report,
        string reportDir,
        CancellationToken ct) =>
        await TryWriteAsync(report, reportDir, ct);

    public async Task WriteOrThrowAsync(
        TranslationImportReport report,
        string reportDir,
        CancellationToken ct)
    {
        try
        {
            await reportWriter.WriteAsync(report, reportDir, ct);
        }
        catch (Exception ex) when (IsReportWriteFailure(ex))
        {
            throw new IOException(
                $"{TranslationInvariants.ReportRequired} ({ex.Message})",
                ex);
        }
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
        catch (Exception ex) when (IsReportWriteFailure(ex))
        {
            return ImportTranslationsResult.Failure(
                $"{TranslationInvariants.ReportRequired} ({ex.Message})",
                reportDir);
        }
    }

    private static bool IsReportWriteFailure(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException;
}
