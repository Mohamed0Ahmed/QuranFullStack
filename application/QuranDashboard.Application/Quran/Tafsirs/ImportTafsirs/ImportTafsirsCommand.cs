using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Application.Quran.Tafsirs.ImportTafsirs;

public sealed record ImportTafsirsCommand(
    string SourcePath,
    bool Force,
    TafsirExpectedCounts? ExpectedCounts = null,
    string? ReportOutDir = null);
