namespace QuranDashboard.Application.Abstractions.Quran.Tafsirs;

public interface ITafsirImportReportBuilder
{
    TafsirImportReport BuildValidationFailure(
        string sourcePath,
        TafsirSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        TafsirImportTotals totals,
        IReadOnlyList<TafsirCheckResult> checks,
        IReadOnlyList<string> errors);

    TafsirImportReport BuildRefusal(
        string sourcePath,
        TafsirSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage);

    TafsirImportReport BuildCandidateSuccess(
        string sourcePath,
        TafsirSourceData source,
        bool forced,
        DateTimeOffset runAtUtc,
        TafsirImportTotals totals,
        IReadOnlyList<TafsirCheckResult> checks);
}
