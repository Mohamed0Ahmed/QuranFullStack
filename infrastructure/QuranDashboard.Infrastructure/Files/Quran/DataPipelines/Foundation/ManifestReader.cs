namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;

public sealed class ManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> RequiredKeys = new(StringComparer.Ordinal)
    {
        "qpc-glyph",
        "uthmani",
        "uthmani-simple",
        "imlaei-simple",
        "layout",
        "surah-meta",
        "ayah-meta"
    };

    public async Task<ImportManifest> ReadAsync(string sourceRoot, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);

        var manifestPath = Path.Combine(sourceRoot, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Manifest file was not found: {manifestPath}", manifestPath);
        }

        await using var manifestStream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(manifestStream, cancellationToken: ct);
        var manifest = document.RootElement.Deserialize<ManifestDocument>(JsonOptions)
            ?? throw new InvalidDataException("manifest.json could not be parsed.");

        if (!string.Equals(manifest.Version, "1", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported manifest version '{manifest.Version}'.");
        }

        ValidateSourcesShape(manifest.Sources);

        var sources = new Dictionary<string, ManifestSource>(StringComparer.Ordinal);
        foreach (var source in manifest.Sources)
        {
            var fullPath = Path.Combine(sourceRoot, source.RelativePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Manifest source file was not found: {fullPath}", fullPath);
            }

            if (!string.IsNullOrWhiteSpace(source.Sha256))
            {
                ValidateChecksum(fullPath, source.Sha256!);
            }

            ValidateDeclaredCounts(source, fullPath, ct);

            sources.Add(source.Key, source with { FullPath = fullPath });
        }

        return new ImportManifest(manifest.Version, sources);
    }

    private static void ValidateSourcesShape(IReadOnlyList<ManifestSource> sources)
    {
        if (sources.Count != RequiredKeys.Count)
        {
            throw new InvalidDataException($"Manifest must contain exactly {RequiredKeys.Count} sources.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            if (!RequiredKeys.Contains(source.Key))
            {
                throw new InvalidDataException($"Unknown manifest source key '{source.Key}'.");
            }

            if (!seen.Add(source.Key))
            {
                throw new InvalidDataException($"Duplicate manifest source key '{source.Key}'.");
            }

            if (!string.Equals(source.Format, "json", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Manifest source '{source.Key}' must use json format.");
            }

            if (string.IsNullOrWhiteSpace(source.RelativePath))
            {
                throw new InvalidDataException($"Manifest source '{source.Key}' is missing a relative path.");
            }
        }

        if (!RequiredKeys.SetEquals(seen))
        {
            throw new InvalidDataException("Manifest is missing one or more required source keys.");
        }
    }

    private static void ValidateDeclaredCounts(ManifestSource source, string fullPath, CancellationToken ct)
    {
        using var stream = File.OpenRead(fullPath);
        using var document = JsonDocument.Parse(stream);

        switch (source.Key)
        {
            case "layout":
            {
                var pagesCount = document.RootElement.GetProperty("pagesCount").GetInt32();
                var pagesElement = document.RootElement.GetProperty("pages");
                var observedPages = 0;
                var observedLines = 0;

                foreach (var page in pagesElement.EnumerateObject())
                {
                    observedPages++;
                    observedLines += page.Value.GetArrayLength();
                }

                if (source.ExpectedPageCount is not null && source.ExpectedPageCount.Value != pagesCount)
                {
                    throw new InvalidDataException($"Manifest count mismatch for '{source.Key}': expectedPageCount={source.ExpectedPageCount.Value}, observed={pagesCount}.");
                }

                if (source.ExpectedPageCount is not null && source.ExpectedPageCount.Value != observedPages)
                {
                    throw new InvalidDataException($"Manifest count mismatch for '{source.Key}': expectedPageCount={source.ExpectedPageCount.Value}, observed={observedPages}.");
                }

                if (source.ExpectedLineCount is not null && source.ExpectedLineCount.Value != observedLines)
                {
                    throw new InvalidDataException($"Manifest count mismatch for '{source.Key}': expectedLineCount={source.ExpectedLineCount.Value}, observed={observedLines}.");
                }

                break;
            }
            default:
            {
                var observedCount = document.RootElement.EnumerateObject().Count();
                if (source.ExpectedRecordCount is not null && source.ExpectedRecordCount.Value != observedCount)
                {
                    throw new InvalidDataException($"Manifest count mismatch for '{source.Key}': expectedRecordCount={source.ExpectedRecordCount.Value}, observed={observedCount}.");
                }

                break;
            }
        }
    }

    private static void ValidateChecksum(string fullPath, string expectedSha256)
    {
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath)));
        if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Checksum mismatch for '{fullPath}'.");
        }
    }

    private sealed record ManifestDocument(
        string Version,
        List<ManifestSource> Sources);
}

public sealed record ManifestSource(
    string Key,
    string RelativePath,
    string Format,
    int? ExpectedRecordCount,
    int? ExpectedPageCount,
    int? ExpectedLineCount,
    string JoinKey,
    string? Sha256)
{
    [JsonIgnore]
    public string? FullPath { get; init; }
}

public sealed record ImportManifest(string Version, IReadOnlyDictionary<string, ManifestSource> Sources);
