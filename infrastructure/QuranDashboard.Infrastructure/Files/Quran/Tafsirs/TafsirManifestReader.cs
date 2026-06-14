using System.Security.Cryptography;
using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Files.Quran.Tafsirs;

public sealed class TafsirManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> RequiredRootFiles =
    [
        "README.md",
        "manifest.json",
        "package-report.md"
    ];

    public async Task<TafsirPackageManifest> ReadAsync(string packagePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        if (!Directory.Exists(packagePath))
        {
            throw new TafsirSourceException($"Tafsir source package directory was not found: {packagePath}");
        }

        ValidatePackageShape(packagePath);

        var manifestPath = Path.Combine(packagePath, "manifest.json");
        await using var manifestStream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);
        var root = document.RootElement;

        var manifestType = root.GetProperty("manifestType").GetString()
            ?? throw new TafsirSourceException("manifest.json is missing manifestType.");
        var isFinalImportManifest = root.GetProperty("isFinalImportManifest").GetBoolean();
        var sourceCount = root.GetProperty("sourceCount").GetInt32();

        var summary = root.GetProperty("summary");
        var approvedCount = summary.GetProperty("copiedApprovedTafsirSources").GetInt32();
        var excludedCount = summary.GetProperty("excludedSources").GetInt32();
        var arabicCount = summary.GetProperty("arabicApprovedCopied").GetInt32();
        var nonArabicCount = summary.GetProperty("nonArabicApprovedCopied").GetInt32();
        var languageCount = summary.GetProperty("languageCount").GetInt32();

        var approvedSources = new List<TafsirManifestSourceRecord>();
        foreach (var sourceElement in root.GetProperty("sources").EnumerateArray())
        {
            var record = sourceElement.Deserialize<TafsirManifestSourceRecord>(JsonOptions)
                ?? throw new TafsirSourceException("A manifest source entry could not be parsed.");

            var fullPath = Path.Combine(packagePath, record.PackageFile.Replace('\\', '/'));
            if (!File.Exists(fullPath))
            {
                throw new TafsirSourceException($"Approved source file was not found: {fullPath}");
            }

            ValidateChecksum(fullPath, record.Sha256);
            ValidateFileSize(fullPath, record.FileSizeBytes);

            approvedSources.Add(record with { FullPath = fullPath });
        }

        ValidateApprovedSourceFileSet(packagePath, approvedSources);

        var excludedSources = new List<ExcludedTafsirSourceDto>();
        if (root.TryGetProperty("excludedSourceSummary", out var excludedElement)
            && excludedElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in excludedElement.EnumerateArray())
            {
                excludedSources.Add(new ExcludedTafsirSourceDto(
                    item.GetProperty("sourceKey").GetString() ?? string.Empty,
                    item.GetProperty("status").GetString() ?? string.Empty,
                    item.GetProperty("resourceKind").GetString() ?? string.Empty,
                    item.TryGetProperty("contentCoverageCount", out var coverage)
                        ? coverage.GetInt32()
                        : 0,
                    item.GetProperty("sourceFileOriginal").GetString() ?? string.Empty,
                    item.GetProperty("reviewReason").GetString() ?? string.Empty));
            }
        }

        return new TafsirPackageManifest(
            manifestType,
            isFinalImportManifest,
            sourceCount,
            approvedCount,
            excludedCount,
            arabicCount,
            nonArabicCount,
            languageCount,
            approvedSources,
            excludedSources,
            root.GetRawText());
    }

    public async Task<TafsirFileDigests> CaptureDigestsAsync(string packagePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        var manifest = await ReadAsync(packagePath, ct);
        var digests = new Dictionary<string, TafsirFileDigest>(StringComparer.Ordinal);

        foreach (var source in manifest.ApprovedSources)
        {
            var relativePath = source.PackageFile.Replace('\\', '/');
            var fullPath = source.FullPath
                ?? throw new TafsirSourceException($"Manifest source '{source.SourceKey}' has no resolved path.");

            await using var stream = File.OpenRead(fullPath);
            var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
            digests[relativePath] = new TafsirFileDigest(new FileInfo(fullPath).Length, sha256);
        }

        return new TafsirFileDigests(digests);
    }

    public async Task<bool> VerifyDigestsUnchangedAsync(
        string packagePath,
        TafsirFileDigests before,
        CancellationToken ct)
    {
        var after = await CaptureDigestsAsync(packagePath, ct);
        return before.Equals(after);
    }

    private static void ValidatePackageShape(string packagePath)
    {
        var errors = new List<string>();

        foreach (var requiredFile in RequiredRootFiles)
        {
            if (!File.Exists(Path.Combine(packagePath, requiredFile)))
            {
                errors.Add($"Missing required file: {requiredFile}");
            }
        }

        var sourcesDir = Path.Combine(packagePath, "sources");
        if (!Directory.Exists(sourcesDir))
        {
            errors.Add("Missing required directory: sources/");
        }

        if (errors.Count > 0)
        {
            throw new TafsirSourceException(
                $"Tafsir package shape validation failed. {string.Join("; ", errors)}");
        }
    }

    private static void ValidateApprovedSourceFileSet(
        string packagePath,
        IReadOnlyList<TafsirManifestSourceRecord> approvedSources)
    {
        var sourcesDir = Path.Combine(packagePath, "sources");
        var onDiskFiles = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in Directory.EnumerateFiles(sourcesDir))
        {
            onDiskFiles.Add(NormalizeRelativePath(Path.GetRelativePath(packagePath, entry)));
        }

        var declaredFiles = approvedSources
            .Select(source => source.PackageFile.Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

        var errors = new List<string>();
        var missing = declaredFiles.Except(onDiskFiles).ToList();
        if (missing.Count > 0)
        {
            errors.Add($"Missing approved source files on disk: {string.Join(", ", missing)}");
        }

        var extra = onDiskFiles.Except(declaredFiles).ToList();
        if (extra.Count > 0)
        {
            errors.Add($"Unexpected source files outside approved manifest set: {string.Join(", ", extra)}");
        }

        if (errors.Count > 0)
        {
            throw new TafsirSourceException(
                $"Tafsir approved source file set validation failed. {string.Join("; ", errors)}");
        }
    }

    private static string NormalizeRelativePath(string relativePath) =>
        relativePath.Replace('\\', '/');

    private static void ValidateChecksum(string fullPath, string expectedSha256)
    {
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath)));
        if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new TafsirSourceException($"Checksum mismatch for '{fullPath}'.");
        }
    }

    private static void ValidateFileSize(string fullPath, long expectedSize)
    {
        var actual = new FileInfo(fullPath).Length;
        if (actual != expectedSize)
        {
            throw new TafsirSourceException(
                $"File size mismatch for '{fullPath}': expected={expectedSize}, observed={actual}.");
        }
    }
}

public sealed record TafsirPackageManifest(
    string ManifestType,
    bool IsFinalImportManifest,
    int SourceCount,
    int ApprovedSourceCount,
    int ExcludedSourceCount,
    int ArabicSourceCount,
    int NonArabicSourceCount,
    int LanguageCount,
    IReadOnlyList<TafsirManifestSourceRecord> ApprovedSources,
    IReadOnlyList<ExcludedTafsirSourceDto> ExcludedSources,
    string ManifestJson);

public sealed record TafsirManifestSourceRecord(
    string SourceKey,
    string LanguageCode,
    string LanguageNameAr,
    string LanguageNameEn,
    string Direction,
    string DisplayNameAr,
    string ShortNameAr,
    string DisplayNameEn,
    string ShortNameEn,
    string? ContributorKey,
    string? ContributorNameAr,
    string? ContributorNameEn,
    string ContributorType,
    string ResourceKind,
    string TafsirKind,
    short ContentCoverageCount,
    string PackageFile,
    string SourceFileOriginal,
    string Sha256,
    long FileSizeBytes,
    string? License,
    string? Provenance)
{
    public string? FullPath { get; init; }
}

public sealed class TafsirFileDigests(IReadOnlyDictionary<string, TafsirFileDigest> digests)
{
    public IReadOnlyDictionary<string, TafsirFileDigest> Digests { get; } = digests;

    public bool Equals(TafsirFileDigests? other)
    {
        if (other is null || Digests.Count != other.Digests.Count)
        {
            return false;
        }

        foreach (var (key, value) in Digests)
        {
            if (!other.Digests.TryGetValue(key, out var otherDigest) || !value.Equals(otherDigest))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode() => Digests.Count;
}

public sealed record TafsirFileDigest(long Size, string Sha256);
