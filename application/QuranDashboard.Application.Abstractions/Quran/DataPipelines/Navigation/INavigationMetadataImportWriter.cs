namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

public interface INavigationMetadataImportWriter
{
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    Task<NavigationMetadataImportResult> ExecuteAcceptedImportAsync(
        NavigationMetadataSourceData source,
        bool force,
        NavigationExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<NavigationMetadataImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct);
}
