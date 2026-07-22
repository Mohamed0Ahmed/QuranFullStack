namespace QuranDashboard.DataImporter.Import.Safety;

public sealed record CanonicalSourceIdentity(string SourceName, string CanonicalId);

public static class CanonicalSourceRegistry
{
    public const string ManifestFileName = "source-identity.json";

    public static IReadOnlyDictionary<string, CanonicalSourceIdentity> Default { get; } =
        new Dictionary<string, CanonicalSourceIdentity>(StringComparer.Ordinal)
        {
            ["quran-foundation"] = new("quran-foundation", "quran-foundation@qpc-hafs-v1"),
            ["quran-morphology"] = new("quran-morphology", "quran-morphology@legacy-v1"),
            ["quran-enriched-morphology"] = new("quran-enriched-morphology", "quran-enriched-morphology@corpus-v1"),
            ["mutashabihat"] = new("mutashabihat", "mutashabihat@v1"),
            ["quran-tafsirs"] = new("quran-tafsirs", "quran-tafsirs@v1"),
            ["quran-translations"] = new("quran-translations", "quran-translations@v1"),
            ["quran-navigation-metadata"] = new("quran-navigation-metadata", "quran-navigation-metadata@v1"),
            ["quran-full-i3rab"] = new("quran-full-i3rab", "quran-full-i3rab@v1")
        };
}
