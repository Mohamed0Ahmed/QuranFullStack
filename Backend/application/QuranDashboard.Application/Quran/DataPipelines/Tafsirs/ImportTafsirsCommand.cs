using QuranDashboard.Application.Abstractions.Quran.DataPipelines;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Application.Quran.DataPipelines.Tafsirs;

public sealed record ImportTafsirsCommand(
    string SourcePath,
    bool Force,
    TafsirExpectedCounts? ExpectedCounts = null,
    string? ReportOutDir = null,
    string Profile = QuranImportProfiles.Full);
