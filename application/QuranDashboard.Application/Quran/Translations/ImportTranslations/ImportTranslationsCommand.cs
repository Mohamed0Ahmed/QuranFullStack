using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Application.Quran.Translations.ImportTranslations;

public sealed record ImportTranslationsCommand(
    string SourcePath,
    bool Force,
    TranslationExpectedCounts? ExpectedCounts = null,
    string? ReportOutDir = null);
