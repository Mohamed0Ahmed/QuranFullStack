namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

public interface INavigationMetadataImportReportBuilder
{
    NavigationMetadataImportReport BuildValidationFailure(
        string sourcePath,
        NavigationMetadataSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        NavigationImportTotals totals,
        IReadOnlyList<NavigationCheckResult> checks,
        IReadOnlyList<string> errors,
        NavigationExpectedCounts? expected);

    NavigationMetadataImportReport BuildRefusal(
        string sourcePath,
        NavigationMetadataSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage,
        NavigationExpectedCounts? expected);

    NavigationMetadataImportReport BuildCandidateSuccess(
        string sourcePath,
        NavigationMetadataSourceData source,
        bool forced,
        DateTimeOffset runAtUtc,
        NavigationImportTotals totals,
        IReadOnlyList<NavigationCheckResult> checks,
        NavigationExpectedCounts expected);
}
