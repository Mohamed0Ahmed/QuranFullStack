using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

public sealed class EnrichedMorphologyManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<EnrichedMorphologyManifest> ReadAsync(string sourceRoot, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);

        var manifestPath = Path.Combine(sourceRoot, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException(
                $"Enriched morphology manifest was not found: {manifestPath}", manifestPath);
        }

        await using var manifestStream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);
        var manifestDoc = document.RootElement.Deserialize<EnrichedMorphologyManifestDocument>(JsonOptions)
            ?? throw new InvalidDataException("Enriched morphology manifest.json could not be parsed.");

        ValidateManifestFileSet(manifestDoc.Files);

        if (manifestDoc.Files.Count != 1)
        {
            throw new InvalidDataException(
                $"Enriched morphology manifest must declare exactly one data file; observed {manifestDoc.Files.Count}.");
        }

        var entry = manifestDoc.Files[0];
        var localPath = ResolveLocalPath(entry);
        var fullPath = Path.Combine(sourceRoot, localPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Enriched morphology source file not found: {fullPath}", fullPath);
        }

        if (string.IsNullOrWhiteSpace(entry.Sha256))
        {
            throw new InvalidDataException(
                $"Enriched morphology manifest entry for '{localPath}' is missing sha256.");
        }

        if (entry.SizeBytes is null)
        {
            throw new InvalidDataException(
                $"Enriched morphology manifest entry for '{localPath}' is missing sizeBytes.");
        }

        if (entry.RecordCount is null)
        {
            throw new InvalidDataException(
                $"Enriched morphology manifest entry for '{localPath}' is missing recordCount.");
        }

        if (entry.SegmentCount is null)
        {
            throw new InvalidDataException(
                $"Enriched morphology manifest entry for '{localPath}' is missing segmentCount.");
        }

        await ValidateChecksumAsync(fullPath, entry.Sha256, ct);
        ValidateFileSize(fullPath, entry.SizeBytes.Value);

        return new EnrichedMorphologyManifest(
            fullPath,
            entry.Sha256,
            entry.SizeBytes.Value,
            entry.RecordCount.Value,
            entry.SegmentCount.Value,
            manifestDoc.QuranWordIdVerifiedAgainstDashboard,
            manifestDoc.Provenance,
            manifestDoc.SourceArtifact);
    }

    public async Task<EnrichedMorphologyFileDigest> CaptureDigestAsync(
        string fullPath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Enriched morphology source file not found: {fullPath}", fullPath);
        }

        var fileInfo = new FileInfo(fullPath);
        await using var stream = File.OpenRead(fullPath);
        var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));

        return new EnrichedMorphologyFileDigest(fileInfo.Length, sha256);
    }

    public async Task<bool> VerifyDigestUnchangedAsync(
        string fullPath, EnrichedMorphologyFileDigest before, CancellationToken ct)
    {
        var after = await CaptureDigestAsync(fullPath, ct);
        return before.Equals(after);
    }

    public async Task<EnrichedMorphologyCountResult> ValidateRecordAndSegmentCountsAsync(
        string fullPath, int expectedRecordCount, int expectedSegmentCount, CancellationToken ct)
    {
        var recordCount = 0;
        var segmentCount = 0;

        await using var stream = File.OpenRead(fullPath);
        IAsyncEnumerable<EnrichedMorphologyRecord?> records;
        try
        {
            records = JsonSerializer.DeserializeAsyncEnumerable<EnrichedMorphologyRecord>(
                stream, JsonOptions, ct);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"Enriched morphology source '{fullPath}' could not be parsed.", ex);
        }

        try
        {
            await foreach (var record in records.WithCancellation(ct))
            {
                if (record is null)
                {
                    throw new InvalidDataException(
                        $"Enriched morphology source '{fullPath}' contained a null record entry.");
                }

                recordCount++;
                segmentCount += record.Segments?.Count ?? 0;
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"Enriched morphology source '{fullPath}' could not be parsed as a JSON array.", ex);
        }

        if (recordCount != expectedRecordCount)
        {
            throw new InvalidDataException(
                $"Enriched morphology record count mismatch for '{fullPath}': " +
                $"expected={expectedRecordCount}, observed={recordCount}.");
        }

        if (segmentCount != expectedSegmentCount)
        {
            throw new InvalidDataException(
                $"Enriched morphology segment count mismatch for '{fullPath}': " +
                $"expected={expectedSegmentCount}, observed={segmentCount}.");
        }

        return new EnrichedMorphologyCountResult(recordCount, segmentCount);
    }

    private static void ValidateManifestFileSet(IReadOnlyList<EnrichedMorphologyManifestFile> files)
    {
        if (files.Count == 0)
        {
            throw new InvalidDataException(
                "Enriched morphology manifest declares no data files.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var localPath = ResolveLocalPath(file);
            if (!seen.Add(localPath))
            {
                throw new InvalidDataException(
                    $"Duplicate enriched morphology manifest file entry: {localPath}");
            }
        }
    }

    private static string ResolveLocalPath(EnrichedMorphologyManifestFile file)
    {
        var localPath = !string.IsNullOrWhiteSpace(file.RelativePath)
            ? file.RelativePath
            : file.Path;

        if (string.IsNullOrWhiteSpace(localPath))
        {
            throw new InvalidDataException(
                "Enriched morphology manifest file entry is missing 'relativePath'.");
        }

        return localPath.Replace('\\', '/');
    }

    private static async Task ValidateChecksumAsync(
        string fullPath, string expectedSha256, CancellationToken ct)
    {
        var actual = await ManifestChecksum.ComputeSha256HexAsync(fullPath, ct);
        if (!ManifestChecksum.Matches(actual, expectedSha256))
        {
            throw new InvalidDataException($"Checksum mismatch for '{fullPath}'.");
        }
    }

    private static void ValidateFileSize(string fullPath, long expectedSize)
    {
        var actual = new FileInfo(fullPath).Length;
        if (actual != expectedSize)
        {
            throw new InvalidDataException(
                $"File size mismatch for '{fullPath}': expected={expectedSize}, observed={actual}.");
        }
    }
}

public sealed record EnrichedMorphologyManifest(
    string FullPath,
    string Sha256,
    long SizeBytes,
    int RecordCount,
    int SegmentCount,
    bool QuranWordIdVerifiedAgainstDashboard,
    string? Provenance,
    EnrichedMorphologySourceArtifact? SourceArtifact);

public sealed record EnrichedMorphologySourceArtifact(
    string? Name,
    string? Generator,
    string? GeneratorReport,
    string? GeneratorVerdict,
    string? SourceAuditRepo);

public sealed record EnrichedMorphologyFileDigest(long Size, string Sha256)
{
    public bool Equals(EnrichedMorphologyFileDigest? other) =>
        other is not null && other.Size == Size
        && string.Equals(other.Sha256, Sha256, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => HashCode.Combine(Size, Sha256.ToUpperInvariant());
}

public sealed record EnrichedMorphologyCountResult(int RecordCount, int SegmentCount);

internal sealed record EnrichedMorphologyManifestDocument(
    List<EnrichedMorphologyManifestFile> Files,
    string? Provenance,
    bool QuranWordIdVerifiedAgainstDashboard,
    EnrichedMorphologySourceArtifact? SourceArtifact);

internal sealed class EnrichedMorphologyManifestFile
{
    public string? Role { get; init; }
    public string? RelativePath { get; init; }

    public string? Path { get; init; }
    public string? Sha256 { get; init; }
    public long? SizeBytes { get; init; }
    public int? RecordCount { get; init; }
    public int? SegmentCount { get; init; }
}
