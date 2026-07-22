using System.Text.Json;

namespace QuranDashboard.DataImporter.Import.Safety;

public sealed class CanonicalQuranSourceVerifier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IReadOnlyDictionary<string, CanonicalSourceIdentity> registry;

    public CanonicalQuranSourceVerifier(IReadOnlyDictionary<string, CanonicalSourceIdentity>? registry = null)
    {
        this.registry = registry ?? CanonicalSourceRegistry.Default;
    }

    public SourceIdentityResult Verify(string sourceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);

        var manifestPath = Path.Combine(sourceDirectory, CanonicalSourceRegistry.ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            return SourceIdentityResult.Reject(
                SourceIdentityStatus.MissingManifest,
                $"No '{CanonicalSourceRegistry.ManifestFileName}' identity manifest was found under '{sourceDirectory}'.");
        }

        SourceManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<SourceManifest>(File.ReadAllText(manifestPath), JsonOptions);
        }
        catch (JsonException exception)
        {
            return SourceIdentityResult.Reject(
                SourceIdentityStatus.MissingManifest,
                $"The identity manifest under '{sourceDirectory}' could not be parsed: {exception.Message}");
        }

        if (manifest is null || string.IsNullOrWhiteSpace(manifest.SourceName))
        {
            return SourceIdentityResult.Reject(
                SourceIdentityStatus.MissingManifest,
                $"The identity manifest under '{sourceDirectory}' is empty or missing 'sourceName'.");
        }

        if (!registry.TryGetValue(manifest.SourceName, out var pinned))
        {
            return SourceIdentityResult.Reject(
                SourceIdentityStatus.Forbidden,
                $"Source '{manifest.SourceName}' is not a pinned canonical Quran source and is refused.");
        }

        if (!string.Equals(manifest.CanonicalId, pinned.CanonicalId, StringComparison.Ordinal))
        {
            return SourceIdentityResult.Reject(
                SourceIdentityStatus.WrongIdentity,
                $"Source '{manifest.SourceName}' declares canonical id '{manifest.CanonicalId}', "
                + $"which does not match the pinned '{pinned.CanonicalId}'.");
        }

        var stableIdError = ValidateStableIds(manifest.StableIds);
        if (stableIdError is not null)
        {
            return SourceIdentityResult.Reject(SourceIdentityStatus.UnstableIds, stableIdError);
        }

        return SourceIdentityResult.Accept(
            $"Source '{manifest.SourceName}' verified against pinned identity '{pinned.CanonicalId}'.");
    }

    private static string? ValidateStableIds(IReadOnlyList<long>? stableIds)
    {
        if (stableIds is null || stableIds.Count == 0)
        {
            return "The identity manifest declares no stable IDs to verify.";
        }

        var seen = new HashSet<long>();
        long previous = long.MinValue;
        foreach (var id in stableIds)
        {
            if (id <= 0)
            {
                return $"Stable IDs must be positive; found '{id}'.";
            }

            if (!seen.Add(id))
            {
                return $"Stable IDs must be unique; '{id}' is duplicated.";
            }

            if (id <= previous)
            {
                return $"Stable IDs must be strictly increasing; '{id}' follows '{previous}'.";
            }

            previous = id;
        }

        return null;
    }

    private sealed record SourceManifest(
        string? SourceName,
        string? CanonicalId,
        string? Revision,
        string? StableIdScheme,
        IReadOnlyList<long>? StableIds);
}
