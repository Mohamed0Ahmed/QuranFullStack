namespace QuranDashboard.Tests.Quran.Import;

internal static class FoundationImportSourceGate
{
    public static string SourceRoot { get; } = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "..",
        "resources", "import-sources", "quran-foundation"));

    public static string MasaqSourceFile { get; } = Path.GetFullPath(Path.Combine(
        SourceRoot,
        "..",
        "masaq-corpus-aligned",
        "masaq-search-words.dashboard-ready.json"));

    public static bool IsMissing =>
        !Directory.Exists(SourceRoot)
        || !File.Exists(MasaqSourceFile);

    public static string MissingReason =>
        $"Staged foundation import sources are missing: foundation={SourceRoot}; masaq={MasaqSourceFile}";
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class FoundationImportSourceFactAttribute : FactAttribute
{
    public FoundationImportSourceFactAttribute()
    {
        if (FoundationImportSourceGate.IsMissing)
        {
            Skip = FoundationImportSourceGate.MissingReason;
        }
    }
}
