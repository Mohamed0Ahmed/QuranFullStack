using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

namespace QuranDashboard.Application.Quran.Mutashabihat.ImportMutashabihat;

public sealed record ImportMutashabihatCommand(
    string SourcePath,
    bool Force,
    MutashabihatExpectedCounts? ExpectedCounts = null,
    string? ReportOutDir = null);
