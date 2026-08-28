namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

public interface ITranslationImportReportBuilder
{
    TranslationImportReport BuildValidationFailure(
        string sourcePath,
        string profile,
        TranslationSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        TranslationImportTotals totals,
        IReadOnlyList<TranslationCheckResult> checks,
        IReadOnlyList<string> errors);

    TranslationImportReport BuildRefusal(
        string sourcePath,
        string profile,
        TranslationSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage);

    TranslationImportReport BuildCandidateSuccess(
        string sourcePath,
        string profile,
        TranslationSourceData source,
        bool forced,
        DateTimeOffset runAtUtc,
        TranslationImportTotals totals,
        IReadOnlyList<TranslationCheckResult> postCopyChecks,
        TranslationExpectedCounts expected);
}
