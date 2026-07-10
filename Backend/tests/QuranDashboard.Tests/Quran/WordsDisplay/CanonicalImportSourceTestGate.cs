namespace QuranDashboard.Tests.Quran.WordsDisplay;

internal static class CanonicalImportSourceTestGate
{
    public static string SourceRoot { get; } = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "..",
        "resources", "import-sources", "quran-foundation"));

    public static string MissingReason =>
        $"Canonical import source tree is missing: {SourceRoot}";

    public static bool IsMissing => !Directory.Exists(SourceRoot);
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class CanonicalImportSourceFactAttribute : FactAttribute
{
    public CanonicalImportSourceFactAttribute()
    {
        if (CanonicalImportSourceTestGate.IsMissing)
        {
            Skip = CanonicalImportSourceTestGate.MissingReason;
        }
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class CanonicalImportSourceTheoryAttribute : TheoryAttribute
{
    public CanonicalImportSourceTheoryAttribute()
    {
        if (CanonicalImportSourceTestGate.IsMissing)
        {
            Skip = CanonicalImportSourceTestGate.MissingReason;
        }
    }
}
