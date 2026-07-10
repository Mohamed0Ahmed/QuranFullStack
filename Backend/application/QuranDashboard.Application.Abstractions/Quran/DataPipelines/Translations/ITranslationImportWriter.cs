namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

public interface ITranslationImportWriter
{
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    Task<TranslationImportResult> ExecuteAcceptedImportAsync(
        TranslationSourceData source,
        bool force,
        TranslationExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<TranslationImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct);
}
