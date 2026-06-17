namespace QuranDashboard.Application.Abstractions.Quran.FullI3rab;

public interface IFullI3rabImportWriter
{
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    Task<FullI3rabImportResult> ExecuteAcceptedImportAsync(
        FullI3rabSourceData source,
        bool force,
        FullI3rabExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<FullI3rabImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct);
}
