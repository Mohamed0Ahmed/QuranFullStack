using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

namespace QuranDashboard.Application.Quran.DataPipelines.Navigation;

public sealed record ImportNavigationMetadataCommand(
    string SourcePath,
    bool Force,
    NavigationExpectedCounts? ExpectedCounts = null,
    string? ReportOutDir = null);
