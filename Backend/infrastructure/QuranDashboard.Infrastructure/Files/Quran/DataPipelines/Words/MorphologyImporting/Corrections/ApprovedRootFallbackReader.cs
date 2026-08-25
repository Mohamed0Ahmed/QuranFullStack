using System.Reflection;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

public sealed class ApprovedRootFallbackReader
{
    internal const string ArtifactResourceName =
        "QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections.approved-root-fallbacks.json";

    private static readonly HashSet<string> AllowedReviewStatuses =
        ["strong", "linguistic", "lexical"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ApprovedRootFallbackLoaded Load()
    {
        using var stream = OpenResource();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var rawBytes = memory.ToArray();
        var artifact = Parse(rawBytes);
        Validate(artifact);

        return new ApprovedRootFallbackLoaded(
            artifact,
            Convert.ToHexStringLower(SHA256.HashData(rawBytes)),
            artifact.Entries.ToDictionary(entry => entry.Location, StringComparer.Ordinal));
    }

    private static Stream OpenResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(ArtifactResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded approved-root fallback resource '{ArtifactResourceName}' was not found.");
    }

    private static ApprovedRootFallbackArtifact Parse(byte[] rawBytes)
    {
        using var stream = new MemoryStream(rawBytes, writable: false);
        return JsonSerializer.Deserialize<ApprovedRootFallbackArtifact>(stream, JsonOptions)
            ?? throw new InvalidDataException("approved-root-fallbacks.json could not be parsed.");
    }

    private static void Validate(ApprovedRootFallbackArtifact artifact)
    {
        if (artifact.SchemaVersion != 1)
        {
            throw new InvalidDataException(
                $"approved-root-fallbacks.json has unsupported schemaVersion '{artifact.SchemaVersion}'.");
        }

        if (!string.Equals(artifact.Source, "QUL word-root.json", StringComparison.Ordinal))
        {
            throw new InvalidDataException("approved-root-fallbacks.json has an unexpected source.");
        }

        if (artifact.ExpectedAppliedCount != MorphologyInvariants.ExpectedApprovedRootFallbacks
            || artifact.Entries.Count != artifact.ExpectedAppliedCount)
        {
            throw new InvalidDataException(
                $"approved-root-fallbacks.json must contain exactly {MorphologyInvariants.ExpectedApprovedRootFallbacks} entries.");
        }

        var locations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in artifact.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Location)
                || entry.QuranWordId <= 0
                || entry.SegmentNumber <= 0
                || string.IsNullOrWhiteSpace(entry.ExpectedLemmaBuckwalter)
                || string.IsNullOrWhiteSpace(entry.RootBuckwalter)
                || string.IsNullOrWhiteSpace(entry.RootArabic)
                || !AllowedReviewStatuses.Contains(entry.ReviewStatus))
            {
                throw new InvalidDataException(
                    $"approved-root fallback entry '{entry.Location}' is incomplete or invalid.");
            }

            if (!locations.Add(entry.Location))
            {
                throw new InvalidDataException(
                    $"approved-root-fallbacks.json contains duplicate location '{entry.Location}'.");
            }
        }
    }
}
