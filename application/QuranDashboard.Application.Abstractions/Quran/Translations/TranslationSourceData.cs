namespace QuranDashboard.Application.Abstractions.Quran.Translations;

public sealed record TranslationSourceData(
    IReadOnlyList<TranslationSourceDto> Sources,
    IReadOnlyList<TranslationAyahEntryDto> AyahEntries,
    IReadOnlyList<ExcludedTranslationSourceDto> ExcludedSources);

public sealed record TranslationSourceDto(
    string SourceKey,
    string LanguageCode,
    string LanguageNameEn,
    string LanguageNameAr,
    string? NativeName,
    string Direction,
    string TranslationType,
    string DisplayNameEn,
    string DisplayNameAr,
    string? TranslatorKey,
    string? TranslatorNameEn,
    string? TranslatorNameAr,
    bool ContainsInlineFootnotes,
    bool ContainsHtmlMarkup,
    bool ReclassifiedFromSimpleByContent,
    int ContentCoverageCount,
    string PackageFile,
    string Sha256,
    long FileSizeBytes);

public sealed record TranslationAyahEntryDto(
    string SourceKey,
    int AyahId,
    string VerseKey,
    string Text);

public sealed record ExcludedTranslationSourceDto(
    string SourceKey,
    string Status,
    string Reason,
    string? PackageFile);
