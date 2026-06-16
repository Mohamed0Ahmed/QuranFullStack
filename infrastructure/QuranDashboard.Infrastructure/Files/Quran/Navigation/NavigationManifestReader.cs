using System.Security.Cryptography;
using QuranDashboard.Application.Abstractions.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Files.Quran.Navigation;

public sealed class NavigationManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] RequiredSourceFiles =
    [
        "sources/quran-metadata-juz.json",
        "sources/quran-metadata-hizb.json",
        "sources/quran-metadata-rub.json",
        "sources/quran-metadata-sajda.json"
    ];

    public async Task<NavigationPackageManifest> ReadAsync(
        string packagePath,
        CancellationToken ct,
        NavigationExpectedCounts? expectedCounts = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        if (!Directory.Exists(packagePath))
        {
            throw new NavigationMetadataSourceException(
                $"Navigation source package directory was not found: {packagePath}");
        }

        var checks = new List<NavigationCheckResult> { ValidatePackageShape(packagePath) };
        NavigationValidationChecks.EnsureAllHardChecksPassed(checks);

        var manifestPath = Path.Combine(packagePath, "manifest.json");
        await using var manifestStream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);
        var root = document.RootElement;

        var packageType = root.GetProperty("packageType").GetString()
            ?? throw new NavigationMetadataSourceException("manifest.json is missing packageType.");
        var isFinalImportManifest = root.GetProperty("isFinalImportManifest").GetBoolean();

        checks.Add(ValidateManifestFinal(packageType, isFinalImportManifest));
        NavigationValidationChecks.EnsureAllHardChecksPassed(checks);

        var sourceFiles = new List<NavigationManifestFileRecord>();
        foreach (var fileElement in root.GetProperty("sourceFiles").EnumerateArray())
        {
            var record = fileElement.Deserialize<NavigationManifestFileRecord>(JsonOptions)
                ?? throw new NavigationMetadataSourceException("A manifest sourceFiles entry could not be parsed.");

            var relativePath = record.RelativePath.Replace('\\', '/');
            var fullPath = Path.Combine(packagePath, relativePath);
            if (!File.Exists(fullPath))
            {
                checks.Add(NavigationValidationChecks.Hard(
                    NavigationMetadataInvariants.CheckPackageShape,
                    relativePath,
                    "missing",
                    false));
                NavigationValidationChecks.EnsureAllHardChecksPassed(checks);
            }

            checks.Add(ValidateChecksum(fullPath, record.Sha256));
            checks.Add(ValidateFileSize(fullPath, record.SizeBytes));
            checks.Add(ValidateRecordCount(fullPath, record.RecordCount, record.DatasetKey));
            NavigationValidationChecks.EnsureAllHardChecksPassed(checks);

            sourceFiles.Add(record with { RelativePath = relativePath, FullPath = fullPath });
        }

        checks.Add(ValidateSourceFileSet(packagePath, sourceFiles));
        checks.Add(ValidateSourceCounts(sourceFiles, expectedCounts));
        NavigationValidationChecks.EnsureAllHardChecksPassed(checks);

        return new NavigationPackageManifest(
            packageType,
            isFinalImportManifest,
            sourceFiles,
            root.GetRawText());
    }

    public async Task<NavigationFileDigests> CapturePackageDigestsAsync(string packagePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        var manifest = await ReadAsync(packagePath, ct);
        var digests = new Dictionary<string, NavigationFileDigest>(StringComparer.Ordinal);

        foreach (var sourceFile in manifest.SourceFiles)
        {
            var fullPath = sourceFile.FullPath
                ?? throw new NavigationMetadataSourceException(
                    $"Manifest file '{sourceFile.RelativePath}' has no resolved path.");

            await using var stream = File.OpenRead(fullPath);
            var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
            digests[sourceFile.RelativePath] = new NavigationFileDigest(new FileInfo(fullPath).Length, sha256);
        }

        return new NavigationFileDigests(digests);
    }

    public async Task<bool> VerifyDigestsUnchangedAsync(
        string packagePath,
        NavigationFileDigests before,
        CancellationToken ct)
    {
        var after = await CapturePackageDigestsAsync(packagePath, ct);
        return before.Equals(after);
    }

    private static NavigationCheckResult ValidatePackageShape(string packagePath)
    {
        var errors = new List<string>();

        if (!File.Exists(Path.Combine(packagePath, "manifest.json")))
        {
            errors.Add("manifest.json");
        }

        foreach (var requiredFile in RequiredSourceFiles)
        {
            if (!File.Exists(Path.Combine(packagePath, requiredFile)))
            {
                errors.Add(requiredFile);
            }
        }

        var sourcesDir = Path.Combine(packagePath, "sources");
        if (Directory.Exists(sourcesDir))
        {
            var actualFiles = Directory.GetFiles(sourcesDir, "*.json")
                .Select(path => $"sources/{Path.GetFileName(path)}")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            var expected = RequiredSourceFiles.OrderBy(path => path, StringComparer.Ordinal).ToList();
            if (!actualFiles.SequenceEqual(expected, StringComparer.Ordinal))
            {
                errors.Add($"unexpected sources files: {string.Join(", ", actualFiles)}");
            }
        }

        return NavigationValidationChecks.Hard(
            NavigationMetadataInvariants.CheckPackageShape,
            "manifest.json and exactly four sources/*.json files",
            errors.Count == 0 ? "present" : $"issues: {string.Join(", ", errors)}",
            errors.Count == 0);
    }

    private static NavigationCheckResult ValidateManifestFinal(string packageType, bool isFinalImportManifest)
    {
        var passed = string.Equals(
                packageType,
                NavigationImportConstants.ManifestType,
                StringComparison.Ordinal)
            && isFinalImportManifest;

        return NavigationValidationChecks.Hard(
            NavigationMetadataInvariants.CheckManifestFinal,
            $"packageType={NavigationImportConstants.ManifestType}, isFinalImportManifest=true",
            $"packageType={packageType}, isFinalImportManifest={isFinalImportManifest.ToString().ToLowerInvariant()}",
            passed);
    }

    private static NavigationCheckResult ValidateChecksum(string fullPath, string expectedSha256)
    {
        using var stream = File.OpenRead(fullPath);
        var actualSha256 = Convert.ToHexString(SHA256.HashData(stream));
        var passed = string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase);

        return NavigationValidationChecks.Hard(
            NavigationMetadataInvariants.CheckSourceHash,
            expectedSha256,
            actualSha256,
            passed);
    }

    private static NavigationCheckResult ValidateFileSize(string fullPath, long expectedSizeBytes)
    {
        var actualSize = new FileInfo(fullPath).Length;
        var passed = actualSize == expectedSizeBytes;

        return NavigationValidationChecks.Hard(
            NavigationMetadataInvariants.CheckSourceHash,
            expectedSizeBytes.ToString(CultureInfo.InvariantCulture),
            actualSize.ToString(CultureInfo.InvariantCulture),
            passed);
    }

    private static NavigationCheckResult ValidateRecordCount(string fullPath, int expectedCount, string datasetKey)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(fullPath));
        var actualCount = document.RootElement.EnumerateObject().Count();
        var passed = actualCount == expectedCount;

        return NavigationValidationChecks.Hard(
            NavigationMetadataInvariants.CheckSourceCount,
            $"{datasetKey} recordCount={expectedCount.ToString(CultureInfo.InvariantCulture)}",
            actualCount.ToString(CultureInfo.InvariantCulture),
            passed);
    }

    private static NavigationCheckResult ValidateSourceFileSet(
        string packagePath,
        IReadOnlyList<NavigationManifestFileRecord> sourceFiles)
    {
        var actualPaths = sourceFiles
            .Select(file => file.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        var expectedPaths = RequiredSourceFiles.OrderBy(path => path, StringComparer.Ordinal).ToList();
        var passed = actualPaths.SequenceEqual(expectedPaths, StringComparer.Ordinal);

        return NavigationValidationChecks.Hard(
            NavigationMetadataInvariants.CheckPackageShape,
            string.Join(", ", expectedPaths),
            string.Join(", ", actualPaths),
            passed);
    }

    private static NavigationCheckResult ValidateSourceCounts(
        IReadOnlyList<NavigationManifestFileRecord> sourceFiles,
        NavigationExpectedCounts? expectedCounts)
    {
        if (expectedCounts is null)
        {
            return NavigationValidationChecks.Hard(
                NavigationMetadataInvariants.CheckSourceCount,
                "skipped",
                "skipped",
                true);
        }

        var juz = sourceFiles.Single(file => file.DatasetKey == "juz").RecordCount;
        var hizb = sourceFiles.Single(file => file.DatasetKey == "hizb").RecordCount;
        var rub = sourceFiles.Single(file => file.DatasetKey == "rub").RecordCount;
        var sajda = sourceFiles.Single(file => file.DatasetKey == "sajda").RecordCount;
        var observed = $"{juz}/{hizb}/{rub}/{sajda}";
        var expected = $"{expectedCounts.Juz}/{expectedCounts.Hizb}/{expectedCounts.Rub}/{expectedCounts.Sajda}";
        var passed = juz == expectedCounts.Juz
            && hizb == expectedCounts.Hizb
            && rub == expectedCounts.Rub
            && sajda == expectedCounts.Sajda;

        return NavigationValidationChecks.Hard(
            NavigationMetadataInvariants.CheckSourceCount,
            expected,
            observed,
            passed);
    }
}

public sealed record NavigationPackageManifest(
    string PackageType,
    bool IsFinalImportManifest,
    IReadOnlyList<NavigationManifestFileRecord> SourceFiles,
    string ManifestJson);

public sealed record NavigationManifestFileRecord(
    string RelativePath,
    string DatasetKey,
    int RecordCount,
    string Sha256,
    long SizeBytes,
    string? FullPath = null);

public sealed record NavigationFileDigests(IReadOnlyDictionary<string, NavigationFileDigest> Files)
{
    public bool Equals(NavigationFileDigests? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Files.Count != other.Files.Count)
        {
            return false;
        }

        foreach (var (key, digest) in Files)
        {
            if (!other.Files.TryGetValue(key, out var otherDigest)
                || !digest.Equals(otherDigest))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (key, digest) in Files.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            hash.Add(key, StringComparer.Ordinal);
            hash.Add(digest);
        }

        return hash.ToHashCode();
    }
}

public sealed record NavigationFileDigest(long SizeBytes, string Sha256);
