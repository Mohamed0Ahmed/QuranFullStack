namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;

/// <summary>
/// Shared SHA-256 checksum computation for Quran DataPipeline manifest readers (decision 5, DRY).
/// Every manifest reader hashes a source file and compares it case-insensitively against an expected
/// value declared in its manifest; this type is the single place that computation lives. Callers keep
/// their own shell (throw vs. return a *CheckResult) — this type only computes and compares.
/// </summary>
internal static class ManifestChecksum
{
    public static string ComputeSha256Hex(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    public static async Task<string> ComputeSha256HexAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
    }

    public static bool Matches(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
}
