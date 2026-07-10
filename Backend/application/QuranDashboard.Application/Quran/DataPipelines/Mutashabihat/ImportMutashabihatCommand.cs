using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;

namespace QuranDashboard.Application.Quran.DataPipelines.Mutashabihat;

public sealed record ImportMutashabihatCommand(
    string SourcePath,
    bool Force,
    MutashabihatExpectedCounts? ExpectedCounts = null,
    string? ReportOutDir = null);
