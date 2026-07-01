using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Tests.Quran.FullI3rab;

public sealed class FullI3rabSyntheticPackage : IDisposable
{
    private readonly List<string> tempDirs = [];
    private bool disposed;

    public const short SyntheticSurahNumber = 900;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        foreach (var dir in tempDirs)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        disposed = true;
    }

    public async Task<string> WriteAsync(
        IReadOnlyList<SyntheticFullI3rabSourceSpec>? sources = null,
        string manifestType = FullI3rabImportConstants.ManifestType,
        bool isFinalImportManifest = true,
        int? sourceCountOverride = null,
        short? coverageOverride = null)
    {
        var sourceSpecs = sources ?? FullI3rabSyntheticSeed.DefaultSources;

        var packageDir = Path.Combine(Path.GetTempPath(), $"quran-full-i3rab-{Guid.NewGuid():N}");
        tempDirs.Add(packageDir);
        Directory.CreateDirectory(packageDir);
        Directory.CreateDirectory(Path.Combine(packageDir, "sources"));

        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "README.md"),
            "Synthetic full-i'rab source package for tests. Source-safe; not for import into production.");
        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "package-report.md"),
            "# Synthetic full-i'rab package report\n\nGenerated for Phase-2 tests.");

        var sourceRecords = new List<object>();
        foreach (var spec in sourceSpecs)
        {
            var packageFile = $"sources/{spec.SourceKey}.json";
            var fullPath = Path.Combine(packageDir, packageFile);
            await WriteJsonAsync(fullPath, spec.Entries);

            var fileInfo = new FileInfo(fullPath);
            var sha256 = ComputeSha256(fullPath);
            sourceRecords.Add(BuildSourceRecord(spec, packageFile, fileInfo.Length, sha256, coverageOverride));
        }

        var manifest = BuildManifest(
            manifestType,
            isFinalImportManifest,
            sourceCountOverride ?? sourceSpecs.Count,
            sourceRecords);

        await WriteJsonAsync(Path.Combine(packageDir, "manifest.json"), manifest);
        return packageDir;
    }

    public string GetSourceFilePath(string packageDir, string sourceKey) =>
        Path.Combine(packageDir, "sources", $"{sourceKey}.json");

    private static object BuildSourceRecord(
        SyntheticFullI3rabSourceSpec spec,
        string packageFile,
        long fileSizeBytes,
        string sha256,
        short? coverageOverride) => new
        {
            sourceKey = spec.SourceKey,
            displayNameAr = $"إعراب اختباري {spec.SourceKey}",
            displayNameEn = $"Synthetic i'rab {spec.SourceKey}",
            shortNameAr = $"اختبار {spec.SourceKey}",
            shortNameEn = $"Test {spec.SourceKey}",
            resourceKind = "full-i3rab",
            contentGrain = "ayah-level-free-text",
            markupFormat = "html",
            direction = "rtl",
            languageCode = "ar",
            attributedWorkAr = $"عمل اختباري {spec.SourceKey}",
            attributedAuthorAr = $"مؤلف اختباري {spec.SourceKey}",
            attributedAuthorEn = $"Synthetic author {spec.SourceKey}",
            attributionConfidence = "unverified",
            license = FullI3rabImportConstants.LicenseStatus,
            provenance = FullI3rabImportConstants.ProvenanceStatus,
            sourceFileOriginal = $"/synthetic/provenance/{spec.SourceKey}.json",
            packageFile,
            sha256,
            sha256MatchesOriginal = true,
            fileSizeBytes,
            canonicalAyahCoverage = coverageOverride ?? spec.CanonicalAyahCoverage,
            shape = new
            {
                totalTopLevelKeys = spec.Entries.Count,
                hasQpcHafsQuotationMarkup = spec.HasQuranQuotationMarkup,
                observedHtmlTags = spec.ObservedHtmlTags,
                observedHtmlClasses = spec.ObservedHtmlClasses
            },
            validationAllPass = true
        };

    private static object BuildManifest(
        string manifestType,
        bool isFinalImportManifest,
        int sourceCount,
        IReadOnlyList<object> sourceRecords) => new
        {
            manifestType,
            isFinalImportManifest,
            createdAtUtc = "2026-06-17T00:00:00Z",
            feature = "feature-010-quran-full-i3rab-foundation",
            sourceRoot = "sources",
            sourceCount,
            canonicalAyahCount = FullI3rabInvariants.ExpectedAyahsPerSource,
            licenseStatus = FullI3rabImportConstants.LicenseStatus,
            provenanceStatus = FullI3rabImportConstants.ProvenanceStatus,
            usageScope = FullI3rabImportConstants.UsageScope,
            licenseWarning = "Synthetic test package; license/provenance unknown.",
            sources = sourceRecords
        };

    private static async Task WriteJsonAsync(string path, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    private static string ComputeSha256(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}

public sealed record SyntheticFullI3rabSourceSpec(
    string SourceKey,
    bool HasQuranQuotationMarkup,
    short CanonicalAyahCoverage,
    IReadOnlyDictionary<string, object> Entries,
    IReadOnlyDictionary<string, int> ObservedHtmlTags,
    IReadOnlyDictionary<string, int> ObservedHtmlClasses);

internal static class FullI3rabSyntheticSeed
{
    public static string SyntheticHtml(string verseKey) =>
        $"<div class=\"ar\"><p>نص إعراب اختباري مُصطنع للمفتاح {verseKey}.</p></div>";

    public static IReadOnlyList<SyntheticFullI3rabSourceSpec> DefaultSources =>
    [
        new SyntheticFullI3rabSourceSpec(
            SourceKey: "test-muyassar",
            HasQuranQuotationMarkup: true,
            CanonicalAyahCoverage: FullI3rabInvariants.ExpectedAyahsPerSource,
            Entries: new Dictionary<string, object>
            {
                ["900:1"] = new
                {
                    text = SyntheticHtml("900:1"),
                    ayah_keys = new[] { "900:1", "900:2" }
                },
                ["900:2"] = "900:1",
                ["900:3"] = new { text = SyntheticHtml("900:3") }
            },
            ObservedHtmlTags: new Dictionary<string, int> { ["div"] = 2, ["p"] = 2 },
            ObservedHtmlClasses: new Dictionary<string, int> { ["ar"] = 2 })
    ];

    public static (int Id, string VerseKey)[] DefaultAyahs =>
    [
        (1, "900:1"),
        (2, "900:2"),
        (3, "900:3")
    ];

    public static IReadOnlyList<SyntheticFullI3rabSourceSpec> FlatOnlySources =>
    [
        new SyntheticFullI3rabSourceSpec(
            SourceKey: "test-flat",
            HasQuranQuotationMarkup: false,
            CanonicalAyahCoverage: FullI3rabInvariants.ExpectedAyahsPerSource,
            Entries: new Dictionary<string, object>
            {
                ["900:1"] = new { text = SyntheticHtml("900:1") },
                ["900:2"] = new { text = SyntheticHtml("900:2") },
                ["900:3"] = new { text = SyntheticHtml("900:3") }
            },
            ObservedHtmlTags: new Dictionary<string, int> { ["div"] = 3, ["p"] = 3 },
            ObservedHtmlClasses: new Dictionary<string, int> { ["ar"] = 3 })
    ];
}
