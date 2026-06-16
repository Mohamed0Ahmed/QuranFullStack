namespace QuranDashboard.DataImporter;

internal static class NavigationImportPaths
{
    internal static string ResolveDefaultNavigationSourcePath() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(),
            "resources",
            "import-sources",
            "quran-navigation-metadata"));

    internal static string ResolveDefaultNavigationReportDir() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(),
            "Backend",
            "report",
            "feature-009-quran-navigation-metadata-foundation"));

    internal static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var resourcesPath = Path.Combine(directory.FullName, "resources");
            var backendPath = Path.Combine(directory.FullName, "Backend");

            if (Directory.Exists(resourcesPath) && Directory.Exists(backendPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not resolve the repository root directory.");
    }
}
