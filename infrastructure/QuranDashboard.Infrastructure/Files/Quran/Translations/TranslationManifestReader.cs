using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Infrastructure.Files.Quran.Translations;

public sealed class TranslationManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> RequiredRootFiles =
    [
        "README.md",
        "manifest.json",
        "package-report.md",
        "source-display-metadata.json"
    ];

    public async Task<TranslationPackageManifest> ReadAsync(string packagePath, CancellationToken ct) =>
        await ReadAsync(packagePath, ct, expectedCounts: null);

    public async Task<TranslationPackageManifest> ReadAsync(
        string packagePath,
        CancellationToken ct,
        TranslationExpectedCounts? expectedCounts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        if (!Directory.Exists(packagePath))
        {
            throw new TranslationSourceException(
                $"Translation source package directory was not found: {packagePath}");
        }

        var checks = new List<TranslationCheckResult> { ValidatePackageShape(packagePath) };
        TranslationValidationChecks.EnsureAllHardChecksPassed(checks);

        var manifestPath = Path.Combine(packagePath, "manifest.json");
        await using var manifestStream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);
        var root = document.RootElement;

        var manifestType = root.GetProperty("manifestType").GetString()
            ?? throw new TranslationSourceException("manifest.json is missing manifestType.");
        var isFinalImportManifest = root.GetProperty("isFinalImportManifest").GetBoolean();
        var sourceCount = root.GetProperty("sourceCount").GetInt32();
        var languageCount = root.GetProperty("languageCount").GetInt32();
        var contentCoverageCount = root.GetProperty("contentCoverageCount").GetInt32();
        var approvedAyahMappingCount = root.GetProperty("approvedAyahMappingCount").GetInt64();

        checks.Add(ValidateManifestFinal(manifestType, isFinalImportManifest));
        TranslationValidationChecks.EnsureAllHardChecksPassed(checks);

        var typeCounts = root.GetProperty("typeCounts");
        var simpleCount = typeCounts.GetProperty("simple").GetInt32();
        var withFootnotesCount = typeCounts.GetProperty("with_footnotes").GetInt32();

        var excludedCounts = root.GetProperty("excludedCounts");
        var excludedTotal = excludedCounts.GetProperty("total").GetInt32();

        var approvedSources = new List<TranslationManifestSourceRecord>();
        foreach (var sourceElement in root.GetProperty("sources").EnumerateArray())
        {
            var record = sourceElement.Deserialize<TranslationManifestSourceRecord>(JsonOptions)
                ?? throw new TranslationSourceException("A manifest source entry could not be parsed.");

            var fullPath = Path.Combine(packagePath, record.PackageFile.Replace('\\', '/'));
            if (!File.Exists(fullPath))
            {
                checks.Add(TranslationValidationChecks.Hard(
                    TranslationInvariants.CheckSourceSet,
                    record.PackageFile,
                    "missing",
                    false));
                TranslationValidationChecks.EnsureAllHardChecksPassed(checks);
            }

            checks.Add(ValidateChecksum(fullPath, record.Sha256));
            checks.Add(ValidateFileSize(fullPath, record.FileSizeBytes));
            TranslationValidationChecks.EnsureAllHardChecksPassed(checks);

            approvedSources.Add(record with { FullPath = fullPath });
        }

        checks.Add(ValidateApprovedSourceFileSet(packagePath, approvedSources));
        TranslationValidationChecks.EnsureAllHardChecksPassed(checks);

        var excludedSources = new List<ExcludedTranslationSourceDto>();
        if (root.TryGetProperty("excludedSourceSummary", out var excludedElement)
            && excludedElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in excludedElement.EnumerateArray())
            {
                excludedSources.Add(new ExcludedTranslationSourceDto(
                    item.GetProperty("sourceKey").GetString() ?? string.Empty,
                    item.GetProperty("status").GetString() ?? string.Empty,
                    item.GetProperty("reason").GetString() ?? string.Empty,
                    item.TryGetProperty("packageFile", out var packageFile)
                        ? packageFile.GetString()
                        : null));
            }
        }

        checks.AddRange(ValidateManifestCounts(
            sourceCount,
            simpleCount,
            withFootnotesCount,
            excludedTotal,
            languageCount,
            contentCoverageCount,
            approvedAyahMappingCount,
            approvedSources,
            excludedSources,
            expectedCounts));
        TranslationValidationChecks.EnsureAllHardChecksPassed(checks);

        return new TranslationPackageManifest(
            manifestType,
            isFinalImportManifest,
            sourceCount,
            simpleCount,
            withFootnotesCount,
            excludedTotal,
            languageCount,
            contentCoverageCount,
            approvedAyahMappingCount,
            approvedSources,
            excludedSources,
            root.GetRawText());
    }

    public async Task<TranslationFileDigests> CaptureDigestsAsync(string packagePath, CancellationToken ct) =>
        await CapturePackageDigestsAsync(packagePath, ct);

    public async Task<TranslationFileDigests> CapturePackageDigestsAsync(string packagePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        var digests = new Dictionary<string, TranslationFileDigest>(StringComparer.Ordinal);

        foreach (var relativePath in RequiredRootFiles)
        {
            await AddPackageFileDigestAsync(packagePath, relativePath, digests, ct);
        }

        var manifestPath = Path.Combine(packagePath, "manifest.json");
        await using var manifestStream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);

        foreach (var sourceElement in document.RootElement.GetProperty("sources").EnumerateArray())
        {
            var packageFile = sourceElement.GetProperty("packageFile").GetString()
                ?? throw new TranslationSourceException("A manifest source entry is missing packageFile.");

            await AddPackageFileDigestAsync(
                packagePath,
                packageFile.Replace('\\', '/'),
                digests,
                ct);
        }

        return new TranslationFileDigests(digests);
    }

    public async Task<bool> VerifyDigestsUnchangedAsync(
        string packagePath,
        TranslationFileDigests before,
        CancellationToken ct)
    {
        var after = await CapturePackageDigestsAsync(packagePath, ct);
        return before.Equals(after);
    }

    private static async Task AddPackageFileDigestAsync(
        string packagePath,
        string relativePath,
        IDictionary<string, TranslationFileDigest> digests,
        CancellationToken ct)
    {
        var fullPath = Path.Combine(packagePath, relativePath.Replace('\\', '/'));
        if (!File.Exists(fullPath))
        {
            throw new TranslationSourceException($"Package file was not found: {fullPath}");
        }

        await using var stream = File.OpenRead(fullPath);
        var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
        digests[relativePath.Replace('\\', '/')] = new TranslationFileDigest(new FileInfo(fullPath).Length, sha256);
    }

    private static TranslationCheckResult ValidatePackageShape(string packagePath)
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

        return TranslationValidationChecks.Hard(
            TranslationInvariants.CheckPackageShape,
            "README.md, manifest.json, package-report.md, source-display-metadata.json, sources/",
            errors.Count == 0 ? "present" : $"missing: {string.Join(", ", errors)}",
            errors.Count == 0);
    }

    private static TranslationCheckResult ValidateManifestFinal(string manifestType, bool isFinalImportManifest)
    {
        var passed = string.Equals(
                manifestType,
                TranslationImportConstants.ManifestType,
                StringComparison.Ordinal)
            && isFinalImportManifest;

        return TranslationValidationChecks.Hard(
            TranslationInvariants.CheckManifestFinal,
            $"manifestType={TranslationImportConstants.ManifestType}, isFinalImportManifest=true",
            $"manifestType={manifestType}, isFinalImportManifest={isFinalImportManifest.ToString().ToLowerInvariant()}",
            passed);
    }

    private static IReadOnlyList<TranslationCheckResult> ValidateManifestCounts(
        int sourceCount,
        int simpleCount,
        int withFootnotesCount,
        int excludedCount,
        int languageCount,
        int contentCoverageCount,
        long approvedAyahMappingCount,
        IReadOnlyList<TranslationManifestSourceRecord> approvedSources,
        IReadOnlyList<ExcludedTranslationSourceDto> excludedSources,
        TranslationExpectedCounts? expectedCounts)
    {
        var observedApproved = approvedSources.Count;
        var observedSimple = approvedSources.Count(source => source.TranslationType == "simple");
        var observedWithFootnotes = approvedSources.Count(source => source.TranslationType == "with_footnotes");
        var observedExcluded = excludedSources.Count;
        var observedLanguages = approvedSources.Select(source => source.LanguageCode).Distinct().Count();
        var observedMappings = approvedSources.Sum(source => source.ContentCoverageCount);

        return
        [
            TranslationValidationChecks.Hard(
                TranslationInvariants.CheckSourceCount,
                expectedCounts?.ApprovedSources.ToString(CultureInfo.InvariantCulture)
                    ?? sourceCount.ToString(CultureInfo.InvariantCulture),
                observedApproved.ToString(CultureInfo.InvariantCulture),
                sourceCount == observedApproved
                    && (expectedCounts is null || observedApproved == expectedCounts.ApprovedSources)),
            TranslationValidationChecks.Hard(
                TranslationInvariants.CheckTypeCounts,
                expectedCounts is null
                    ? $"simple={simpleCount}, with_footnotes={withFootnotesCount}"
                    : $"simple={expectedCounts.SimpleSources}, with_footnotes={expectedCounts.WithFootnotesSources}",
                $"simple={observedSimple}, with_footnotes={observedWithFootnotes}",
                simpleCount == observedSimple
                    && withFootnotesCount == observedWithFootnotes
                    && (expectedCounts is null
                        || (observedSimple == expectedCounts.SimpleSources
                            && observedWithFootnotes == expectedCounts.WithFootnotesSources))),
            TranslationValidationChecks.Hard(
                TranslationInvariants.CheckExcludedCount,
                expectedCounts?.ExcludedSources.ToString(CultureInfo.InvariantCulture)
                    ?? excludedCount.ToString(CultureInfo.InvariantCulture),
                observedExcluded.ToString(CultureInfo.InvariantCulture),
                excludedCount == observedExcluded
                    && (expectedCounts is null || observedExcluded == expectedCounts.ExcludedSources)),
            TranslationValidationChecks.Hard(
                TranslationInvariants.CheckCoverageCount,
                expectedCounts is null
                    ? $"contentCoverageCount={contentCoverageCount}, approvedAyahMappingCount={approvedAyahMappingCount}, languageCount={languageCount}"
                    : $"contentCoverageCount={expectedCounts.AyahsPerSource}, approvedAyahMappingCount={expectedCounts.SourceAyahMappings}, languageCount={expectedCounts.Languages}",
                $"contentCoverageCount={contentCoverageCount}, approvedAyahMappingCount={observedMappings}, languageCount={observedLanguages}",
                contentCoverageCount == observedMappings / Math.Max(observedApproved, 1)
                    && approvedAyahMappingCount == observedMappings
                    && languageCount == observedLanguages
                    && approvedSources.All(source => source.ContentCoverageCount == contentCoverageCount)
                    && (expectedCounts is null
                        || (contentCoverageCount == expectedCounts.AyahsPerSource
                            && observedMappings == expectedCounts.SourceAyahMappings
                            && observedLanguages == expectedCounts.Languages)))
        ];
    }

    private static TranslationCheckResult ValidateApprovedSourceFileSet(
        string packagePath,
        IReadOnlyList<TranslationManifestSourceRecord> approvedSources)
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

        return TranslationValidationChecks.Hard(
            TranslationInvariants.CheckSourceSet,
            "sources/ exactly matches manifest approved package files",
            observed,
            passed);
    }

    private static string NormalizeRelativePath(string relativePath) =>
        relativePath.Replace('\\', '/');

    private static TranslationCheckResult ValidateChecksum(string fullPath, string expectedSha256)
    {
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath)));
        var passed = string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);

        return TranslationValidationChecks.Hard(
            TranslationInvariants.CheckSourceHash,
            expectedSha256,
            passed ? expectedSha256 : actual,
            passed);
    }

    private static TranslationCheckResult ValidateFileSize(string fullPath, long expectedSize)
    {
        var actual = new FileInfo(fullPath).Length;
        var passed = actual == expectedSize;

        return TranslationValidationChecks.Hard(
            TranslationInvariants.CheckSourceHash,
            expectedSize.ToString(CultureInfo.InvariantCulture),
            actual.ToString(CultureInfo.InvariantCulture),
            passed);
    }
}

public sealed record TranslationPackageManifest(
    string ManifestType,
    bool IsFinalImportManifest,
    int SourceCount,
    int SimpleSourceCount,
    int WithFootnotesSourceCount,
    int ExcludedSourceCount,
    int LanguageCount,
    int ContentCoverageCount,
    long ApprovedAyahMappingCount,
    IReadOnlyList<TranslationManifestSourceRecord> ApprovedSources,
    IReadOnlyList<ExcludedTranslationSourceDto> ExcludedSources,
    string ManifestJson);

public sealed record TranslationManifestSourceRecord(
    string SourceKey,
    string LanguageCode,
    string TranslationType,
    int ContentCoverageCount,
    string PackageFile,
    string Sha256,
    long FileSizeBytes)
{
    public string? FullPath { get; init; }
}

public sealed class TranslationFileDigests(IReadOnlyDictionary<string, TranslationFileDigest> digests)
{
    public IReadOnlyDictionary<string, TranslationFileDigest> Digests { get; } = digests;

    public bool Equals(TranslationFileDigests? other)
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

public sealed record TranslationFileDigest(long Size, string Sha256);
