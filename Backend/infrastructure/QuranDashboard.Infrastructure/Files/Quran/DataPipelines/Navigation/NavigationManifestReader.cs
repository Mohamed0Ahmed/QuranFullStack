using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Navigation;

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
        JsonDocument document;
        try
        {
            await using var manifestStream = File.OpenRead(manifestPath);
            document = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            checks.Add(NavigationValidationChecks.Hard(
                NavigationMetadataInvariants.CheckJsonShape,
                "valid manifest.json",
                ex.Message,
                false));
            NavigationValidationChecks.EnsureAllHardChecksPassed(checks);
            throw new InvalidOperationException("Navigation validation failed.");
        }

        using (document)
        {
            var root = document.RootElement;

            if (!root.TryGetProperty("packageType", out var packageTypeElement)
                || packageTypeElement.ValueKind != JsonValueKind.String)
            {
                checks.Add(NavigationValidationChecks.Hard(
                    NavigationMetadataInvariants.CheckJsonShape,
                    "manifest packageType string",
                    "missing or invalid",
                    false));
                NavigationValidationChecks.EnsureAllHardChecksPassed(checks);
            }

            if (!root.TryGetProperty("isFinalImportManifest", out var finalElement)
                || (finalElement.ValueKind != JsonValueKind.True && finalElement.ValueKind != JsonValueKind.False))
            {
                checks.Add(NavigationValidationChecks.Hard(
                    NavigationMetadataInvariants.CheckJsonShape,
                    "manifest isFinalImportManifest boolean",
                    "missing or invalid",
                    false));
                NavigationValidationChecks.EnsureAllHardChecksPassed(checks);
            }

            var packageType = packageTypeElement.GetString() ?? string.Empty;
            var isFinalImportManifest = finalElement.GetBoolean();

            checks.Add(ValidateManifestFinal(packageType, isFinalImportManifest));
            NavigationValidationChecks.EnsureAllHardChecksPassed(checks);

            if (!root.TryGetProperty("sourceFiles", out var sourceFilesElement)
                || sourceFilesElement.ValueKind != JsonValueKind.Array)
            {
                checks.Add(NavigationValidationChecks.Hard(
                    NavigationMetadataInvariants.CheckJsonShape,
                    "manifest sourceFiles array",
                    "missing or invalid",
                    false));
                NavigationValidationChecks.EnsureAllHardChecksPassed(checks);
            }

            var sourceFiles = new List<NavigationManifestFileRecord>();
            foreach (var fileElement in sourceFilesElement.EnumerateArray())
            {
                var record = fileElement.Deserialize<NavigationManifestFileRecord>(JsonOptions);
                if (record is null)
                {
                    checks.Add(NavigationValidationChecks.Hard(
                        NavigationMetadataInvariants.CheckJsonShape,
                        "manifest sourceFiles entry",
                        "could not be parsed",
                        false));
                    NavigationValidationChecks.EnsureAllHardChecksPassed(checks);
                    continue;
                }

                checks.Add(ValidateManifestSourceFileEntry(record));
                NavigationValidationChecks.EnsureAllHardChecksPassed(checks);

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
    }

    public async Task<NavigationFileDigests> CapturePackageDigestsAsync(string packagePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        var manifestPath = Path.Combine(packagePath, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new NavigationMetadataSourceException(
                $"Navigation source package directory was not found: {packagePath}");
        }

        await using var manifestStream = File.OpenRead(manifestPath);
        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            throw new NavigationMetadataSourceException(
                $"Navigation source package manifest is invalid JSON: {ex.Message}");
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("sourceFiles", out var sourceFilesElement)
                || sourceFilesElement.ValueKind != JsonValueKind.Array)
            {
                throw new NavigationMetadataSourceException(
                    "Navigation source package manifest is missing sourceFiles.");
            }

            var digests = new Dictionary<string, NavigationFileDigest>(StringComparer.Ordinal);
            foreach (var fileElement in sourceFilesElement.EnumerateArray())
            {
                if (!fileElement.TryGetProperty("relativePath", out var relativePathElement)
                    || relativePathElement.ValueKind != JsonValueKind.String)
                {
                    throw new NavigationMetadataSourceException(
                        "A manifest sourceFiles entry is missing relativePath.");
                }

                var relativePath = relativePathElement.GetString()!.Replace('\\', '/');
                var fullPath = Path.Combine(packagePath, relativePath);

                await using var stream = File.OpenRead(fullPath);
                var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
                digests[relativePath] = new NavigationFileDigest(new FileInfo(fullPath).Length, sha256);
            }

            return new NavigationFileDigests(digests);
        }
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

    private static NavigationCheckResult ValidateManifestSourceFileEntry(NavigationManifestFileRecord record)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(record.RelativePath))
        {
            issues.Add("relativePath");
        }

        if (string.IsNullOrWhiteSpace(record.DatasetKey))
        {
            issues.Add("datasetKey");
        }

        if (string.IsNullOrWhiteSpace(record.Sha256))
        {
            issues.Add("sha256");
        }

        if (record.RecordCount < 0)
        {
            issues.Add("recordCount");
        }

        if (record.SizeBytes < 0)
        {
            issues.Add("sizeBytes");
        }

        return NavigationValidationChecks.Hard(
            NavigationMetadataInvariants.CheckJsonShape,
            "manifest sourceFiles entry with relativePath, datasetKey, sha256, recordCount, sizeBytes",
            issues.Count == 0 ? "valid" : $"missing or invalid: {string.Join(", ", issues)}",
            issues.Count == 0);
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
        var actualSha256 = ManifestChecksum.ComputeSha256Hex(fullPath);
        var passed = ManifestChecksum.Matches(actualSha256, expectedSha256);

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
        try
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
        catch (JsonException)
        {
            return NavigationValidationChecks.Hard(
                NavigationMetadataInvariants.CheckJsonShape,
                $"{datasetKey} valid JSON object",
                $"{Path.GetFileName(fullPath)}: invalid JSON",
                false);
        }
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

        var juzFile = FindSourceFile(sourceFiles, "juz");
        var hizbFile = FindSourceFile(sourceFiles, "hizb");
        var rubFile = FindSourceFile(sourceFiles, "rub");
        var sajdaFile = FindSourceFile(sourceFiles, "sajda");

        if (juzFile is null || hizbFile is null || rubFile is null || sajdaFile is null)
        {
            return NavigationValidationChecks.Hard(
                NavigationMetadataInvariants.CheckPackageShape,
                "juz,hizb,rub,sajda dataset keys",
                string.Join(",", sourceFiles.Select(file => file.DatasetKey)),
                false);
        }

        var juz = juzFile.RecordCount;
        var hizb = hizbFile.RecordCount;
        var rub = rubFile.RecordCount;
        var sajda = sajdaFile.RecordCount;

        return NavigationValidationChecks.ValidateSourceCounts(juz, hizb, rub, sajda, expectedCounts);
    }

    private static NavigationManifestFileRecord? FindSourceFile(
        IReadOnlyList<NavigationManifestFileRecord> sourceFiles,
        string datasetKey) =>
        sourceFiles.FirstOrDefault(file =>
            string.Equals(file.DatasetKey, datasetKey, StringComparison.Ordinal));
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
