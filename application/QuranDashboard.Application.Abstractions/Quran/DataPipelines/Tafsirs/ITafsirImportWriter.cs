namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

public interface ITafsirImportWriter
{
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    Task<TafsirImportResult> ExecuteAcceptedImportAsync(
        TafsirSourceData source,
        bool force,
        TafsirExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<TafsirImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct);
}
