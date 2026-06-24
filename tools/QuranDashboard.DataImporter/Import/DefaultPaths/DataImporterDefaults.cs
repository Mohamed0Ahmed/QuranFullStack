namespace QuranDashboard.DataImporter.Import.DefaultPaths;

internal static class DataImporterDefaults
{
    internal static string ResolveDefaultMutashabihatSourcePath() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "import-sources", "mutashabihat"));

    internal static string ResolveDefaultTafsirSourcePath() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "import-sources", "quran-tafsirs"));

    internal static string ResolveDefaultTafsirReportDir() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "report", "quran-tafsirs"));

    internal static string ResolveDefaultTranslationSourcePath() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "import-sources", "quran-translations"));

    internal static string ResolveDefaultTranslationReportDir() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "Backend", "report", "feature-008-quran-translations-foundation"));

    internal static string ResolveDefaultMorphologySourcePath() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "import-sources", "quran-morphology"));

    internal static string ResolveDefaultFullI3rabSourcePath() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "import-sources", "quran-full-i3rab"));

    internal static string ResolveDefaultFullI3rabReportDir() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "report", "quran-full-i3rab"));

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
