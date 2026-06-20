using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Tafsirs;

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

    public async Task<TafsirPackageManifest> ReadAsync(string packagePath, CancellationToken ct) =>
        await ReadAsync(packagePath, ct, expectedCounts: null);

    public async Task<TafsirPackageManifest> ReadAsync(
        string packagePath,
        CancellationToken ct,
        TafsirExpectedCounts? expectedCounts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        if (!Directory.Exists(packagePath))
        {
            throw new TafsirSourceException($"Tafsir source package directory was not found: {packagePath}");
        }

        var checks = new List<TafsirCheckResult>();
        checks.Add(ValidatePackageShape(packagePath));
        TafsirValidationChecks.EnsureAllHardChecksPassed(checks);

        var manifestPath = Path.Combine(packagePath, "manifest.json");
        await using var manifestStream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);
        var root = document.RootElement;

        var manifestType = root.GetProperty("manifestType").GetString()
            ?? throw new TafsirSourceException("manifest.json is missing manifestType.");
        var isFinalImportManifest = root.GetProperty("isFinalImportManifest").GetBoolean();
        var sourceCount = root.GetProperty("sourceCount").GetInt32();

        checks.Add(ValidateManifestFinal(manifestType, isFinalImportManifest));
        TafsirValidationChecks.EnsureAllHardChecksPassed(checks);

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
                checks.Add(TafsirValidationChecks.Hard(
                    TafsirInvariants.CheckSourceSet,
                    record.PackageFile,
                    "missing",
                    false));
                TafsirValidationChecks.EnsureAllHardChecksPassed(checks);
            }

            checks.Add(ValidateChecksum(fullPath, record.Sha256));
            checks.Add(ValidateFileSize(fullPath, record.FileSizeBytes));
            TafsirValidationChecks.EnsureAllHardChecksPassed(checks);

            approvedSources.Add(record with { FullPath = fullPath });
        }

        checks.Add(ValidateApprovedSourceFileSet(packagePath, approvedSources));
        TafsirValidationChecks.EnsureAllHardChecksPassed(checks);

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

        checks.AddRange(ValidateManifestCounts(
            sourceCount,
            approvedCount,
            excludedCount,
            arabicCount,
            nonArabicCount,
            approvedSources,
            excludedSources,
            expectedCounts));
        TafsirValidationChecks.EnsureAllHardChecksPassed(checks);

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

    private static TafsirCheckResult ValidatePackageShape(string packagePath)
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

        return TafsirValidationChecks.Hard(
            TafsirInvariants.CheckPackageShape,
            "README.md, manifest.json, package-report.md, sources/",
            errors.Count == 0 ? "present" : $"missing: {string.Join(", ", errors)}",
            errors.Count == 0);
    }

    private static TafsirCheckResult ValidateManifestFinal(string manifestType, bool isFinalImportManifest)
    {
        var passed = string.Equals(
                manifestType,
                TafsirImportConstants.ManifestType,
                StringComparison.Ordinal)
            && isFinalImportManifest;

        return TafsirValidationChecks.Hard(
            TafsirInvariants.CheckManifestFinal,
            $"manifestType={TafsirImportConstants.ManifestType}, isFinalImportManifest=true",
            $"manifestType={manifestType}, isFinalImportManifest={isFinalImportManifest.ToString().ToLowerInvariant()}",
            passed);
    }

    private static IReadOnlyList<TafsirCheckResult> ValidateManifestCounts(
        int sourceCount,
        int approvedCount,
        int excludedCount,
        int arabicCount,
        int nonArabicCount,
        IReadOnlyList<TafsirManifestSourceRecord> approvedSources,
        IReadOnlyList<ExcludedTafsirSourceDto> excludedSources,
        TafsirExpectedCounts? expectedCounts)
    {
        var observedApproved = approvedSources.Count;
        var observedExcluded = excludedSources.Count;
        var observedArabic = approvedSources.Count(source => source.LanguageCode == "ar");
        var observedNonArabic = observedApproved - observedArabic;

        return
        [
            TafsirValidationChecks.Hard(
                TafsirInvariants.CheckSourceCount,
                expectedCounts?.ApprovedSources.ToString(CultureInfo.InvariantCulture)
                    ?? approvedCount.ToString(CultureInfo.InvariantCulture),
                observedApproved.ToString(CultureInfo.InvariantCulture),
                sourceCount == observedApproved
                    && approvedCount == observedApproved
                    && (expectedCounts is null
                        || observedApproved == expectedCounts.ApprovedSources)),
            TafsirValidationChecks.Hard(
                TafsirInvariants.CheckExcludedCount,
                expectedCounts?.ExcludedSources.ToString(CultureInfo.InvariantCulture)
                    ?? excludedCount.ToString(CultureInfo.InvariantCulture),
                observedExcluded.ToString(CultureInfo.InvariantCulture),
                excludedCount == observedExcluded
                    && (expectedCounts is null
                        || observedExcluded == expectedCounts.ExcludedSources)),
            TafsirValidationChecks.Hard(
                TafsirInvariants.CheckArabicSourceCount,
                expectedCounts?.ArabicSources.ToString(CultureInfo.InvariantCulture)
                    ?? arabicCount.ToString(CultureInfo.InvariantCulture),
                observedArabic.ToString(CultureInfo.InvariantCulture),
                arabicCount == observedArabic
                    && (expectedCounts is null
                        || observedArabic == expectedCounts.ArabicSources)),
            TafsirValidationChecks.Hard(
                TafsirInvariants.CheckNonArabicSourceCount,
                expectedCounts?.NonArabicSources.ToString(CultureInfo.InvariantCulture)
                    ?? nonArabicCount.ToString(CultureInfo.InvariantCulture),
                observedNonArabic.ToString(CultureInfo.InvariantCulture),
                nonArabicCount == observedNonArabic
                    && (expectedCounts is null
                        || observedNonArabic == expectedCounts.NonArabicSources))
        ];
    }

    private static TafsirCheckResult ValidateApprovedSourceFileSet(
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

        return TafsirValidationChecks.Hard(
            TafsirInvariants.CheckSourceSet,
            "sources/ exactly matches manifest approved package files",
            observed,
            passed);
    }

    private static string NormalizeRelativePath(string relativePath) =>
        relativePath.Replace('\\', '/');

    private static TafsirCheckResult ValidateChecksum(string fullPath, string expectedSha256)
    {
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath)));
        var passed = string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);

        return TafsirValidationChecks.Hard(
            TafsirInvariants.CheckSourceHash,
            expectedSha256,
            passed ? expectedSha256 : actual,
            passed);
    }

    private static TafsirCheckResult ValidateFileSize(string fullPath, long expectedSize)
    {
        var actual = new FileInfo(fullPath).Length;
        var passed = actual == expectedSize;

        return TafsirValidationChecks.Hard(
            TafsirInvariants.CheckSourceHash,
            expectedSize.ToString(CultureInfo.InvariantCulture),
            actual.ToString(CultureInfo.InvariantCulture),
            passed);
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
