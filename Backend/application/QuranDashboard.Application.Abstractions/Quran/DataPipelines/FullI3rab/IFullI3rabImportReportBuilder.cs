namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;

public interface IFullI3rabImportReportBuilder
{
    FullI3rabImportReport BuildValidationFailure(
        string sourcePath,
        FullI3rabSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        FullI3rabImportTotals totals,
        IReadOnlyList<FullI3rabCheckResult> checks,
        IReadOnlyList<string> errors);

    FullI3rabImportReport BuildRefusal(
        string sourcePath,
        FullI3rabSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage);

    FullI3rabImportReport BuildCandidateSuccess(
        string sourcePath,
        FullI3rabSourceData source,
        bool forced,
        DateTimeOffset runAtUtc,
        FullI3rabImportTotals totals,
        IReadOnlyList<FullI3rabCheckResult> postCopyChecks,
        FullI3rabExpectedCounts expected);
}
