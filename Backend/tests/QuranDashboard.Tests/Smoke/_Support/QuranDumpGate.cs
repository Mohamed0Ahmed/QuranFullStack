namespace QuranDashboard.Tests.Smoke._Support;

internal static class QuranDumpGate
{
    public static string DumpRoot { get; } = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "..",
        "resources", "db-dumps", "quran-canonical"));

    public static string DumpPath => Path.Combine(DumpRoot, "quran-canonical.dump");

    public static string ManifestPath => Path.Combine(DumpRoot, "manifest.json");

    public static string MissingReason =>
        $"Canonical Quran dump is missing (run Backend/scripts/create-smoke-dump on a staged machine): {DumpRoot}";

    public static bool IsMissing => !File.Exists(DumpPath) || !File.Exists(ManifestPath);
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class QuranDumpFactAttribute : FactAttribute
{
    public QuranDumpFactAttribute()
    {
        if (QuranDumpGate.IsMissing)
        {
            Skip = QuranDumpGate.MissingReason;
        }
    }
}
