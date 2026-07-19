using System.Reflection;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

public sealed class SegmentStemCorrectionReader : ISegmentStemCorrectionReader
{
    internal const string ArtifactResourceName =
        "QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections.segment-stem-corrected-arabic.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public SegmentStemCorrectionLoaded Load()
    {
        using var stream = OpenResource(ArtifactResourceName);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var rawBytes = memory.ToArray();

        var sha256 = Convert.ToHexStringLower(SHA256.HashData(rawBytes));
        var artifact = ParseArtifact(rawBytes);

        ValidateSchema(artifact);

        var approvedByLocation = new Dictionary<string, string>(StringComparer.Ordinal);
        var unresolved = new HashSet<string>(StringComparer.Ordinal);
        var approvedCount = 0;
        var unresolvedCount = 0;

        foreach (var entry in artifact.Mappings)
        {
            if (string.Equals(entry.ReviewDecision, SegmentStemCorrectionArtifact.ApprovedDecision, StringComparison.Ordinal))
            {
                approvedByLocation[entry.SegmentLocation] = entry.ReviewedStemText!;
                approvedCount++;
            }
            else
            {
                unresolved.Add(entry.SegmentLocation);
                unresolvedCount++;
            }
        }

        var counts = new SegmentStemCorrectionCounts(artifact.Mappings.Count, approvedCount, unresolvedCount);
        return new SegmentStemCorrectionLoaded(artifact, sha256, counts, approvedByLocation, unresolved);
    }

    private static void ValidateSchema(SegmentStemCorrectionArtifact artifact)
    {
        if (artifact.Mappings.Count == 0)
        {
            throw new InvalidDataException("segment-stem-corrected-arabic.json contains no mappings.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in artifact.Mappings)
        {
            if (string.IsNullOrWhiteSpace(entry.SegmentLocation))
            {
                throw new InvalidDataException("segment-stem correction entry is missing segment_location.");
            }

            if (!seen.Add(entry.SegmentLocation))
            {
                throw new InvalidDataException(
                    $"segment-stem correction has a duplicate segment_location '{entry.SegmentLocation}'.");
            }

            switch (entry.ReviewDecision)
            {
                case SegmentStemCorrectionArtifact.ApprovedDecision:
                    if (string.IsNullOrWhiteSpace(entry.ReviewedStemText))
                    {
                        throw new InvalidDataException(
                            $"approved segment-stem correction '{entry.SegmentLocation}' has no reviewed_stem_text.");
                    }

                    break;

                case SegmentStemCorrectionArtifact.UnresolvedDecision:
                    if (entry.ReviewedStemId is not null || !string.IsNullOrWhiteSpace(entry.ReviewedStemText))
                    {
                        throw new InvalidDataException(
                            $"unresolved segment-stem correction '{entry.SegmentLocation}' must have null reviewed stem.");
                    }

                    if (string.IsNullOrWhiteSpace(entry.Reason))
                    {
                        throw new InvalidDataException(
                            $"unresolved segment-stem correction '{entry.SegmentLocation}' must state a reason.");
                    }

                    break;

                default:
                    throw new InvalidDataException(
                        $"segment-stem correction '{entry.SegmentLocation}' has unknown review_decision '{entry.ReviewDecision}'.");
            }
        }
    }

    private static Stream OpenResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(
                $"Embedded segment-stem correction resource '{name}' was not found.");
    }

    private static SegmentStemCorrectionArtifact ParseArtifact(byte[] rawBytes)
    {
        using var jsonStream = new MemoryStream(rawBytes, writable: false);
        return JsonSerializer.Deserialize<SegmentStemCorrectionArtifact>(jsonStream, JsonOptions)
            ?? throw new InvalidDataException("segment-stem-corrected-arabic.json could not be parsed.");
    }
}
