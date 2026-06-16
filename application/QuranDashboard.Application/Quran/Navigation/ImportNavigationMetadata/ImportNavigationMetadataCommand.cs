using QuranDashboard.Application.Abstractions.Quran.Navigation;

namespace QuranDashboard.Application.Quran.Navigation.ImportNavigationMetadata;

public sealed record ImportNavigationMetadataCommand(
    string SourcePath,
    bool Force,
    NavigationExpectedCounts? ExpectedCounts = null,
    string? ReportOutDir = null);
