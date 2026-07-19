using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Mutashabihat;

public sealed class MutashabihatManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> AllowedSourceFiles =
    [
        "manifest.json",
        "README.md",
        "mutashabihat-ul-quran/phrases.json",
        "similar-ayahs/matching-ayah.json"
    ];

    private static readonly HashSet<string> ManifestRequiredPaths =
    [
        "mutashabihat-ul-quran/phrases.json",
        "similar-ayahs/matching-ayah.json"
    ];

    private static readonly HashSet<string> AllowedSubdirectories =
        new(StringComparer.Ordinal)
        {
            "mutashabihat-ul-quran",
            "similar-ayahs"
        };

    public async Task<MutashabihatManifest> ReadAsync(string sourceRoot, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);

        var manifestPath = Path.Combine(sourceRoot, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new MutashabihatSourceException($"Manifest file was not found: {manifestPath}");
        }

        await using var manifestStream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);
        var manifest = document.RootElement.Deserialize<MutashabihatManifestDocument>(JsonOptions)
            ?? throw new MutashabihatSourceException("manifest.json could not be parsed.");

        ValidateSourceFolderContents(sourceRoot);
        ValidateManifestFileSet(manifest.Files);

        var files = new Dictionary<string, MutashabihatManifestFile>(StringComparer.Ordinal);
        foreach (var file in manifest.Files)
        {
            var localPath = file.ResolveLocalPath();
            var fullPath = Path.Combine(sourceRoot, localPath);
            if (!File.Exists(fullPath))
            {
                throw new MutashabihatSourceException($"Source file not found: {fullPath}");
            }

            if (string.IsNullOrWhiteSpace(file.Sha256))
            {
                throw new MutashabihatSourceException(
                    $"Manifest entry for '{localPath}' is missing sha256.");
            }

            if (file.FileSizeBytes is null)
            {
                throw new MutashabihatSourceException(
                    $"Manifest entry for '{localPath}' is missing fileSizeBytes.");
            }

            ValidateChecksum(fullPath, file.Sha256);
            ValidateFileSize(fullPath, file.FileSizeBytes.Value);

            if (file.ExpectedRecordCount is not null)
            {
                await ValidateRecordCountAsync(fullPath, file.ExpectedRecordCount.Value, ct);
            }

            files.Add(localPath, file with { FullPath = fullPath });
        }

        return new MutashabihatManifest(files);
    }

    public async Task<MutashabihatFileDigests> CaptureDigestsAsync(
        string sourceRoot, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);

        var digests = new Dictionary<string, MutashabihatFileDigest>(StringComparer.Ordinal);
        foreach (var relativePath in AllowedSourceFiles)
        {
            if (string.Equals(relativePath, "manifest.json", StringComparison.Ordinal))
            {
                continue;
            }

            var fullPath = Path.Combine(sourceRoot, relativePath);
            if (!File.Exists(fullPath))
            {
                throw new MutashabihatSourceException($"Source file not found: {fullPath}");
            }

            var fileInfo = new FileInfo(fullPath);
            await using var stream = File.OpenRead(fullPath);
            var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));

            digests[relativePath] = new MutashabihatFileDigest(fileInfo.Length, sha256);
        }

        return new MutashabihatFileDigests(digests);
    }

    public async Task<bool> VerifyDigestsUnchangedAsync(
        string sourceRoot, MutashabihatFileDigests before, CancellationToken ct)
    {
        var after = await CaptureDigestsAsync(sourceRoot, ct);
        return before.Equals(after);
    }

    private static void ValidateManifestFileSet(IReadOnlyList<MutashabihatManifestFile> files)
    {
        var errors = new List<string>();
        var declaredPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            string localPath;
            try
            {
                localPath = file.ResolveLocalPath();
            }
            catch (MutashabihatSourceException ex)
            {
                errors.Add(ex.Message);
                continue;
            }

            if (!declaredPaths.Add(localPath))
            {
                errors.Add($"Duplicate manifest file entry: {localPath}");
            }
        }

        var missing = ManifestRequiredPaths.Except(declaredPaths).ToList();
        if (missing.Count > 0)
        {
            errors.Add($"Missing manifest file entries: {string.Join(", ", missing)}");
        }

        var extra = declaredPaths.Except(ManifestRequiredPaths).ToList();
        if (extra.Count > 0)
        {
            errors.Add($"Unexpected manifest file entries: {string.Join(", ", extra)}");
        }

        if (errors.Count > 0)
        {
            throw new MutashabihatSourceException(
                $"Manifest file set validation failed. {string.Join("; ", errors)}");
        }
    }

    private static void ValidateSourceFolderContents(string sourceRoot)
    {
        var onDiskFiles = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in Directory.EnumerateFiles(
            sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = NormalizeRelativePath(Path.GetRelativePath(sourceRoot, entry));
            onDiskFiles.Add(relativePath);
        }

        var errors = new List<string>();

        foreach (var directory in Directory.EnumerateDirectories(
            sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativeDirectory = NormalizeRelativePath(Path.GetRelativePath(sourceRoot, directory));
            if (!AllowedSubdirectories.Contains(relativeDirectory))
            {
                errors.Add($"Unexpected directory in source folder: {relativeDirectory}");
            }
        }

        var missing = AllowedSourceFiles.Except(onDiskFiles).ToList();
        if (missing.Count > 0)
        {
            errors.Add($"Missing required files: {string.Join(", ", missing)}");
        }

        var extra = onDiskFiles.Except(AllowedSourceFiles).ToList();
        if (extra.Count > 0)
        {
            errors.Add($"Unexpected files in source folder: {string.Join(", ", extra)}");
        }

        if (errors.Count > 0)
        {
            throw new MutashabihatSourceException(
                $"Source folder validation failed. {string.Join("; ", errors)}");
        }
    }

    private static string NormalizeRelativePath(string relativePath) =>
        relativePath.Replace('\\', '/');

    private static void ValidateChecksum(string fullPath, string expectedSha256)
    {
        var actual = ManifestChecksum.ComputeSha256Hex(fullPath);
        if (!ManifestChecksum.Matches(actual, expectedSha256))
        {
            throw new MutashabihatSourceException($"Checksum mismatch for '{fullPath}'.");
        }
    }

    private static void ValidateFileSize(string fullPath, long expectedSize)
    {
        var actual = new FileInfo(fullPath).Length;
        if (actual != expectedSize)
        {
            throw new MutashabihatSourceException(
                $"File size mismatch for '{fullPath}': expected={expectedSize}, observed={actual}.");
        }
    }

    private static async Task ValidateRecordCountAsync(
        string fullPath, int expectedCount, CancellationToken ct)
    {
        await using var stream = File.OpenRead(fullPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new MutashabihatSourceException($"Expected JSON object root in '{fullPath}'.");
        }

        var observedCount = document.RootElement.EnumerateObject().Count();
        if (observedCount != expectedCount)
        {
            throw new MutashabihatSourceException(
                $"Record count mismatch for '{fullPath}': expected={expectedCount}, observed={observedCount}.");
        }
    }
}

public sealed record MutashabihatManifest(
    IReadOnlyDictionary<string, MutashabihatManifestFile> Files);

public sealed record MutashabihatManifestFile(
    string Role,
    string Path,
    string? OriginPath,
    int? ExpectedRecordCount,
    long? FileSizeBytes,
    string? Sha256,
    string? Notes)
{
    [JsonPropertyName("relativePath")]
    public string? RelativePath { get; init; }

    public string? FullPath { get; init; }

    public string ResolveLocalPath()
    {
        var localPath = !string.IsNullOrWhiteSpace(Path)
            ? Path
            : RelativePath;

        if (string.IsNullOrWhiteSpace(localPath))
        {
            throw new MutashabihatSourceException("Manifest file entry is missing 'path'.");
        }

        return localPath.Replace('\\', '/');
    }
}

public sealed class MutashabihatFileDigests(IReadOnlyDictionary<string, MutashabihatFileDigest> digests)
{
    public IReadOnlyDictionary<string, MutashabihatFileDigest> Digests { get; } = digests;

    public bool Equals(MutashabihatFileDigests? other)
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

public sealed record MutashabihatFileDigest(long Size, string Sha256);

internal sealed record MutashabihatManifestDocument(
    List<MutashabihatManifestFile> Files);
