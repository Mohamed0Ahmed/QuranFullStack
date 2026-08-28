namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

public interface ITafsirImportReportBuilder
{
    TafsirImportReport BuildValidationFailure(
        string sourcePath,
        string profile,
        TafsirSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        TafsirImportTotals totals,
        IReadOnlyList<TafsirCheckResult> checks,
        IReadOnlyList<string> errors);

    TafsirImportReport BuildRefusal(
        string sourcePath,
        string profile,
        TafsirSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage);

    TafsirImportReport BuildCandidateSuccess(
        string sourcePath,
        string profile,
        TafsirSourceData source,
        bool forced,
        DateTimeOffset runAtUtc,
        TafsirImportTotals totals,
        IReadOnlyList<TafsirCheckResult> postCopyChecks,
        TafsirExpectedCounts expected);
}
