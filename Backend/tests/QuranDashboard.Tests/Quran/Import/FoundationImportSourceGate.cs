namespace QuranDashboard.Tests.Quran.Import;

internal static class FoundationImportSourceGate
{
    public static string SourceRoot { get; } = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "..",
        "resources", "import-sources", "quran-foundation"));

    public static bool IsMissing => !Directory.Exists(SourceRoot);

    public static string MissingReason =>
        $"Staged foundation import source package is missing: {SourceRoot}";
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
