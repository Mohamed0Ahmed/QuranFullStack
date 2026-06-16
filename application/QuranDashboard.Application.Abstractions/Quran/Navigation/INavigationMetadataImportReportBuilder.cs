namespace QuranDashboard.Application.Abstractions.Quran.Navigation;

public interface INavigationMetadataImportReportBuilder
{
    NavigationMetadataImportReport BuildValidationFailure(
        string sourcePath,
        NavigationMetadataSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        NavigationImportTotals totals,
        IReadOnlyList<NavigationCheckResult> checks,
        IReadOnlyList<string> errors);

    NavigationMetadataImportReport BuildRefusal(
        string sourcePath,
        NavigationMetadataSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage);

    NavigationMetadataImportReport BuildCandidateSuccess(
        string sourcePath,
        NavigationMetadataSourceData source,
        bool forced,
        DateTimeOffset runAtUtc,
        NavigationImportTotals totals,
        IReadOnlyList<NavigationCheckResult> checks,
        NavigationExpectedCounts expected);
}
