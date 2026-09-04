using QuranDashboard.DataImporter.Import.DefaultPaths;

namespace QuranDashboard.Tests.Quran.Navigation;

public sealed class NavigationDefaultPathTests
{
    [Fact]
    public void DataImporter_default_navigation_source_path_resolves_from_repository_root()
    {
        var repositoryRoot = NavigationImportPaths.ResolveRepositoryRoot();
        var expectedDefault = Path.GetFullPath(Path.Combine(
            repositoryRoot,
            "resources",
            "import-sources",
            "quran-navigation-metadata"));

        NavigationImportPaths.ResolveDefaultNavigationSourcePath().Should().Be(expectedDefault);
        NavigationImportPaths.ResolveDefaultNavigationSourcePath()
            .Should().EndWith(Path.Combine("resources", "import-sources", "quran-navigation-metadata"));
    }

    [Fact]
    public void DataImporter_default_navigation_report_dir_resolves_from_repository_root()
    {
        var repositoryRoot = NavigationImportPaths.ResolveRepositoryRoot();
        var expectedDefault = Path.GetFullPath(Path.Combine(
            repositoryRoot,
            "Backend",
            "report",
            "feature-009-quran-navigation-metadata-foundation"));

        NavigationImportPaths.ResolveDefaultNavigationReportDir().Should().Be(expectedDefault);
        NavigationImportPaths.ResolveDefaultNavigationReportDir()
            .Should().EndWith(Path.Combine("Backend", "report", "feature-009-quran-navigation-metadata-foundation"));
    }
}
