using System.Globalization;
using QuranDashboard.Application.Abstractions.Quran.FullI3rab;

namespace QuranDashboard.Infrastructure.Files.Quran.FullI3rab;

public sealed class FullI3rabManifestReader
{
    private static readonly HashSet<string> RequiredRootFiles =
    [
        "README.md",
        "manifest.json",
        "package-report.md"
    ];

    public async Task<FullI3rabPackageManifest> ReadAsync(string packagePath, CancellationToken ct) =>
        await ReadAsync(packagePath, ct, expectedCounts: null);

    public async Task<FullI3rabPackageManifest> ReadAsync(
        string packagePath,
        CancellationToken ct,
        FullI3rabExpectedCounts? expectedCounts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        if (!Directory.Exists(packagePath))
        {
            throw new FullI3rabSourceException(
                $"Full i'rab source package directory was not found: {packagePath}");
        }

        var checks = new List<FullI3rabCheckResult>();
        checks.Add(ValidatePackageShape(packagePath));
        FullI3rabValidationChecks.EnsureAllHardChecksPassed(checks);

        var manifestPath = Path.Combine(packagePath, "manifest.json");
        await using var manifestStream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);
        var root = document.RootElement;

        var manifestType = root.GetProperty("manifestType").GetString()
            ?? throw new FullI3rabSourceException("manifest.json is missing manifestType.");
        var isFinalImportManifest = root.GetProperty("isFinalImportManifest").GetBoolean();
        var sourceCount = root.GetProperty("sourceCount").GetInt32();

        var licenseStatus = ReadOptionalString(root, "licenseStatus", FullI3rabImportConstants.LicenseStatus);
        var provenanceStatus = ReadOptionalString(root, "provenanceStatus", FullI3rabImportConstants.ProvenanceStatus);
        var usageScope = ReadOptionalString(root, "usageScope", FullI3rabImportConstants.UsageScope);

        checks.Add(ValidateManifestFinal(manifestType, isFinalImportManifest));
        FullI3rabValidationChecks.EnsureAllHardChecksPassed(checks);

        var approvedSources = new List<FullI3rabManifestSourceRecord>();
        foreach (var sourceElement in root.GetProperty("sources").EnumerateArray())
        {
            var record = ReadSourceRecord(sourceElement, packagePath);

            var fullPath = Path.Combine(packagePath, record.PackageFile.Replace('\\', '/'));
            if (!File.Exists(fullPath))
            {
                checks.Add(FullI3rabValidationChecks.Hard(
                    FullI3rabInvariants.CheckSourceSet,
                    record.PackageFile,
                    "missing",
                    false));
                FullI3rabValidationChecks.EnsureAllHardChecksPassed(checks);
            }

            checks.Add(ValidateChecksum(fullPath, record.Sha256));
            checks.Add(ValidateFileSize(fullPath, record.FileSizeBytes));
            checks.Add(ValidateCoverageCount(record));
            FullI3rabValidationChecks.EnsureAllHardChecksPassed(checks);

            approvedSources.Add(record with { FullPath = fullPath });
        }

        checks.Add(ValidateApprovedSourceFileSet(packagePath, approvedSources));
        checks.Add(ValidateSourceCount(sourceCount, approvedSources, expectedCounts));
        FullI3rabValidationChecks.EnsureAllHardChecksPassed(checks);

        return new FullI3rabPackageManifest(
            manifestType,
            isFinalImportManifest,
            sourceCount,
            licenseStatus,
            provenanceStatus,
            usageScope,
            approvedSources,
            root.GetRawText());
    }

    public async Task<FullI3rabFileDigests> CaptureDigestsAsync(string packagePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        var manifest = await ReadAsync(packagePath, ct);
        var digests = new Dictionary<string, FullI3rabFileDigest>(StringComparer.Ordinal);

        foreach (var source in manifest.ApprovedSources)
        {
            var relativePath = source.PackageFile.Replace('\\', '/');
            var fullPath = source.FullPath
                ?? throw new FullI3rabSourceException(
                    $"Manifest source '{source.SourceKey}' has no resolved path.");

            await using var stream = File.OpenRead(fullPath);
            var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
            digests[relativePath] = new FullI3rabFileDigest(new FileInfo(fullPath).Length, sha256);
        }

        return new FullI3rabFileDigests(digests);
    }

    public async Task<bool> VerifyDigestsUnchangedAsync(
        string packagePath,
        FullI3rabFileDigests before,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentNullException.ThrowIfNull(before);

        foreach (var (relativePath, priorDigest) in before.Digests)
        {
            var fullPath = Path.Combine(packagePath, relativePath.Replace('\\', '/'));
            if (!File.Exists(fullPath))
            {
                return false;
            }

            var fileInfo = new FileInfo(fullPath);
            await using var stream = File.OpenRead(fullPath);
            var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
            var current = new FullI3rabFileDigest(fileInfo.Length, sha256);
            if (!current.Equals(priorDigest))
            {
                return false;
            }
        }

        return true;
    }

    private static FullI3rabManifestSourceRecord ReadSourceRecord(JsonElement element, string packagePath)
    {
        var sourceKey = element.GetProperty("sourceKey").GetString()
            ?? throw new FullI3rabSourceException("A manifest source entry is missing sourceKey.");

        var packageFile = element.GetProperty("packageFile").GetString()
            ?? throw new FullI3rabSourceException($"Manifest source '{sourceKey}' is missing packageFile.");

        var sourceFileOriginal = ReadOptionalString(element, "sourceFileOriginal", string.Empty);
        var sha256 = ReadOptionalString(element, "sha256", string.Empty);
        var fileSizeBytes = element.TryGetProperty("fileSizeBytes", out var sizeElement)
            ? sizeElement.GetInt64()
            : 0L;
        var coverageCount = element.TryGetProperty("canonicalAyahCoverage", out var coverageElement)
            ? (short)coverageElement.GetInt32()
            : (short)0;

        var resourceKind = ReadOptionalString(element, "resourceKind", FullI3rabImportConstants.ResourceKind);
        var markupFormat = ReadOptionalString(element, "markupFormat", FullI3rabImportConstants.MarkupFormatHtml);
        var languageCode = ReadOptionalString(element, "languageCode", FullI3rabImportConstants.LanguageCode);
        var direction = ReadOptionalString(element, "direction", FullI3rabImportConstants.DirectionRtl);

        var displayNameAr = ReadOptionalString(element, "displayNameAr", string.Empty);
        var shortNameAr = ReadOptionalString(element, "shortNameAr", displayNameAr);
        var displayNameEn = ReadOptionalString(element, "displayNameEn", string.Empty);
        var shortNameEn = ReadOptionalString(element, "shortNameEn", displayNameEn);

        var contributorNameAr = element.TryGetProperty("attributedAuthorAr", out var authorArElement)
            && authorArElement.ValueKind == JsonValueKind.String
                ? authorArElement.GetString()
                : null;
        var contributorNameEn = element.TryGetProperty("attributedAuthorEn", out var authorEnElement)
            && authorEnElement.ValueKind == JsonValueKind.String
                ? authorEnElement.GetString()
                : null;

        var hasQuranQuotationMarkup = element.TryGetProperty("shape", out var shapeElement)
            && shapeElement.ValueKind == JsonValueKind.Object
            && shapeElement.TryGetProperty("hasQpcHafsQuotationMarkup", out var qpcElement)
            && qpcElement.ValueKind == JsonValueKind.True;

        var shapeJson = element.TryGetProperty("shape", out var shapeForRaw)
            && shapeForRaw.ValueKind == JsonValueKind.Object
                ? shapeForRaw.GetRawText()
                : "{}";

        var license = ReadOptionalString(element, "license", FullI3rabImportConstants.LicenseStatus);
        var provenance = ReadOptionalString(element, "provenance", FullI3rabImportConstants.ProvenanceStatus);

        return new FullI3rabManifestSourceRecord(
            sourceKey,
            displayNameAr,
            shortNameAr,
            displayNameEn,
            shortNameEn,
            languageCode,
            direction,
            contributorNameAr,
            contributorNameEn,
            resourceKind,
            markupFormat,
            hasQuranQuotationMarkup,
            coverageCount,
            packageFile,
            sourceFileOriginal,
            sha256,
            fileSizeBytes,
            license,
            provenance,
            shapeJson);
    }

    private static string ReadOptionalString(JsonElement element, string propertyName, string fallback)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            return property.GetString() ?? fallback;
        }

        return fallback;
    }

    private static FullI3rabCheckResult ValidatePackageShape(string packagePath)
    {
        var errors = new List<string>();

        foreach (var requiredFile in RequiredRootFiles)
        {
            if (!File.Exists(Path.Combine(packagePath, requiredFile)))
            {
                errors.Add(requiredFile);
            }
        }

        if (!Directory.Exists(Path.Combine(packagePath, "sources")))
        {
            errors.Add("sources/");
        }

        return FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckPackageShape,
            "README.md, manifest.json, package-report.md, sources/",
            errors.Count == 0 ? "present" : $"missing: {string.Join(", ", errors)}",
            errors.Count == 0);
    }

    private static FullI3rabCheckResult ValidateManifestFinal(string manifestType, bool isFinalImportManifest)
    {
        var passed = string.Equals(
                manifestType,
                FullI3rabImportConstants.ManifestType,
                StringComparison.Ordinal)
            && isFinalImportManifest;

        return FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckManifestFinal,
            $"manifestType={FullI3rabImportConstants.ManifestType}, isFinalImportManifest=true",
            $"manifestType={manifestType}, isFinalImportManifest={isFinalImportManifest.ToString().ToLowerInvariant()}",
            passed);
    }

    private static FullI3rabCheckResult ValidateSourceCount(
        int sourceCount,
        IReadOnlyList<FullI3rabManifestSourceRecord> approvedSources,
        FullI3rabExpectedCounts? expectedCounts)
    {
        var observed = approvedSources.Count;
        var passed = sourceCount == observed
            && (expectedCounts is null || observed == expectedCounts.Sources);

        return FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckSourceCount,
            expectedCounts?.Sources.ToString(CultureInfo.InvariantCulture)
                ?? sourceCount.ToString(CultureInfo.InvariantCulture),
            observed.ToString(CultureInfo.InvariantCulture),
            passed);
    }

    private static FullI3rabCheckResult ValidateCoverageCount(FullI3rabManifestSourceRecord source)
    {
        var passed = source.ContentCoverageCount == FullI3rabInvariants.ExpectedAyahsPerSource;

        return FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckCoverageCount,
            FullI3rabInvariants.ExpectedAyahsPerSource.ToString(CultureInfo.InvariantCulture),
            source.ContentCoverageCount.ToString(CultureInfo.InvariantCulture),
            passed);
    }

    private static FullI3rabCheckResult ValidateApprovedSourceFileSet(
        string packagePath,
        IReadOnlyList<FullI3rabManifestSourceRecord> approvedSources)
    {
        var sourcesDir = Path.Combine(packagePath, "sources");
        var onDiskFiles = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in Directory.EnumerateFiles(sourcesDir))
        {
            onDiskFiles.Add(Path.GetRelativePath(packagePath, entry).Replace('\\', '/'));
        }

        var declaredFiles = approvedSources
            .Select(source => source.PackageFile.Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

        var missing = declaredFiles.Except(onDiskFiles).ToList();
        var extra = onDiskFiles.Except(declaredFiles).ToList();
        var passed = missing.Count == 0 && extra.Count == 0;

        var observed = passed
            ? "exact match"
            : string.Join(
                "; ",
                new[]
                {
                    missing.Count > 0 ? $"missing: {string.Join(", ", missing)}" : null,
                    extra.Count > 0 ? $"extra: {string.Join(", ", extra)}" : null
                }.Where(part => part is not null));

        return FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckSourceSet,
            "sources/ exactly matches manifest approved package files",
            observed,
            passed);
    }

    private static FullI3rabCheckResult ValidateChecksum(string fullPath, string expectedSha256)
    {
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath)));
        var passed = string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);

        return FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckSourceHash,
            expectedSha256,
            passed ? expectedSha256 : actual,
            passed);
    }

    private static FullI3rabCheckResult ValidateFileSize(string fullPath, long expectedSize)
    {
        var actual = new FileInfo(fullPath).Length;
        var passed = actual == expectedSize;

        return FullI3rabValidationChecks.Hard(
            FullI3rabInvariants.CheckSourceHash,
            expectedSize.ToString(CultureInfo.InvariantCulture),
            actual.ToString(CultureInfo.InvariantCulture),
            passed);
    }
}

public sealed record FullI3rabPackageManifest(
    string ManifestType,
    bool IsFinalImportManifest,
    int SourceCount,
    string LicenseStatus,
    string ProvenanceStatus,
    string UsageScope,
    IReadOnlyList<FullI3rabManifestSourceRecord> ApprovedSources,
    string ManifestJson);

public sealed record FullI3rabManifestSourceRecord(
    string SourceKey,
    string DisplayNameAr,
    string ShortNameAr,
    string DisplayNameEn,
    string ShortNameEn,
    string LanguageCode,
    string Direction,
    string? ContributorNameAr,
    string? ContributorNameEn,
    string ResourceKind,
    string MarkupFormat,
    bool HasQuranQuotationMarkup,
    short ContentCoverageCount,
    string PackageFile,
    string SourceFileOriginal,
    string Sha256,
    long FileSizeBytes,
    string License,
    string Provenance,
    string ShapeJson)
{
    public string? FullPath { get; init; }
}

public sealed class FullI3rabFileDigests(IReadOnlyDictionary<string, FullI3rabFileDigest> digests)
{
    public IReadOnlyDictionary<string, FullI3rabFileDigest> Digests { get; } = digests;

    public bool Equals(FullI3rabFileDigests? other)
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

public sealed record FullI3rabFileDigest(long Size, string Sha256);
