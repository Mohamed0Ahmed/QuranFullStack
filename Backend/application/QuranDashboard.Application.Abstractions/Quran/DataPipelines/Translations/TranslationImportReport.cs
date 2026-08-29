namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

public sealed record TranslationImportReport(
    DateTimeOffset RunAtUtc,
    string Verdict,
    bool Persisted,
    bool Forced,
    string Profile,
    string SourcePath,
    TranslationImportTotals Totals,
    IReadOnlyList<TranslationSourceSummary> SourceSummaries,
    IReadOnlyList<TranslationExcludedSourceSummary> ExcludedSourceSummaries,
    IReadOnlyList<TranslationCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record TranslationSourceSummary(
    string SourceKey,
    string LanguageCode,
    string Direction,
    string TranslationType,
    string DisplayNameEn,
    string DisplayNameAr,
    string PackageFile,
    string Sha256,
    long FileSizeBytes,
    bool ContainsInlineFootnotes,
    bool ContainsHtmlMarkup,
    bool ReclassifiedFromSimpleByContent);

public sealed record TranslationExcludedSourceSummary(
    string SourceKey,
    string Status,
    string Reason);
