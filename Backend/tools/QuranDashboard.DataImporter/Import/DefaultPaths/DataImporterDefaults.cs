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

    internal static string ResolveDefaultMorphologyReportDir() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "report", "words-morphology"));

    internal static string ResolveDefaultEnrichedMorphologySourcePath() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "import-sources", "quran-enriched-morphology"));

    internal static string ResolveDefaultDisplayWordsReportDir() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "report", "words-display"));

    internal static string ResolveDefaultSimpleI3rabReportDir() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "report", "words-simple-i3rab"));

    internal static string ResolveDefaultMutashabihatReportDir() =>
        Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(), "resources", "report", "mutashabihat"));

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
            var solutionPath = Path.Combine(directory.FullName, "Backend", "QuranDashboard.sln");
            var gitPath = Path.Combine(directory.FullName, ".git");
            var backendPath = Path.Combine(directory.FullName, "Backend");

            if (File.Exists(solutionPath) ||
                ((Directory.Exists(gitPath) || File.Exists(gitPath)) && Directory.Exists(backendPath)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not resolve the repository root directory.");
    }
}
