using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

namespace QuranDashboard.Application.Quran.DataPipelines.Translations;

public sealed record ImportTranslationsCommand(
    string SourcePath,
    bool Force,
    TranslationExpectedCounts? ExpectedCounts = null,
    string? ReportOutDir = null);
