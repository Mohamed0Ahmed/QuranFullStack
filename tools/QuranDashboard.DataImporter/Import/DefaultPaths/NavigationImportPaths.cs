namespace QuranDashboard.DataImporter.Import.DefaultPaths;

/// <summary>
/// Backwards-compatible facade over <see cref="DataImporterDefaults"/> for the navigation
/// default source/report paths. Preserved so existing call sites (notably
/// <c>NavigationSourcePathTests</c>) keep working unchanged after the
/// <c>Program.cs</c> structural split.
/// </summary>
internal static class NavigationImportPaths
{
    internal static string ResolveDefaultNavigationSourcePath() =>
        DataImporterDefaults.ResolveDefaultNavigationSourcePath();

    internal static string ResolveDefaultNavigationReportDir() =>
        DataImporterDefaults.ResolveDefaultNavigationReportDir();

    internal static string ResolveRepositoryRoot() =>
        DataImporterDefaults.ResolveRepositoryRoot();
}
