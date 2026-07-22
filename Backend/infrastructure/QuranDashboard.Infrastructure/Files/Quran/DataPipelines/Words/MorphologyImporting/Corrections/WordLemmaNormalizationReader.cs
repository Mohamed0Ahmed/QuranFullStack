using System.Reflection;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

public sealed class WordLemmaNormalizationReader : IWordLemmaNormalizationReader
{
    internal const string ArtifactResourceName =
        "QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections.word-lemma-normalization.json";

    internal const string EvidenceResourceName =
        "QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections.word-lemma-mapping-evidence.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Validate eagerly so a malformed embedded artifact fails closed at load, before any apply.
    public WordLemmaNormalizationLoaded Load()
    {
        using var stream = OpenResource(ArtifactResourceName);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var rawBytes = memory.ToArray();

        var sha256 = Convert.ToHexStringLower(SHA256.HashData(rawBytes));
        var artifact = ParseArtifact(rawBytes);
        var counts = ComputeCounts(artifact);

        var loaded = new WordLemmaNormalizationLoaded(artifact, LoadEvidence(), sha256, counts);
        WordLemmaNormalizationValidator.ValidateSchema(loaded);
        return loaded;
    }

    public IReadOnlyList<WordLemmaMappingEvidenceEntry> LoadEvidence()
    {
        using var stream = OpenResource(EvidenceResourceName);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return ParseEvidence(memory.ToArray());
    }

    // Never mutates `rawLemmas`; fails closed on mismatch. readableWordLocations (when non-null) gates
    // every correction location against the readable Quran word set (rejects an artifact that drifts).
    public WordLemmaNormalizationResult Apply(
        IReadOnlyDictionary<string, string> rawLemmas,
        WordLemmaNormalizationLoaded loaded,
        IReadOnlySet<string>? readableWordLocations = null,
        string? rawLemmasSha256 = null)
    {
        ArgumentNullException.ThrowIfNull(rawLemmas);
        ArgumentNullException.ThrowIfNull(loaded);

        WordLemmaNormalizationValidator.ValidateSchema(loaded);

        var result = WordLemmaNormalizationApplier.Apply(
            rawLemmas, loaded.Artifact, readableWordLocations, rawLemmasSha256);

        return result with
        {
            Summary = result.Summary with { ArtifactSha256 = loaded.ArtifactSha256 },
        };
    }

    private static Stream OpenResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(
                $"Embedded word-lemma normalization resource '{name}' was not found.");
    }

    private static WordLemmaNormalizationArtifact ParseArtifact(byte[] rawBytes)
    {
        WordLemmaNormalizationArtifact artifact;
        using (var jsonStream = new MemoryStream(rawBytes, writable: false))
        {
            artifact = JsonSerializer.Deserialize<WordLemmaNormalizationArtifact>(jsonStream, JsonOptions)
                ?? throw new InvalidDataException("word-lemma-normalization.json could not be parsed.");
        }

        return artifact;
    }

    private static IReadOnlyList<WordLemmaMappingEvidenceEntry> ParseEvidence(byte[] rawBytes)
    {
        using var jsonStream = new MemoryStream(rawBytes, writable: false);
        var evidence = JsonSerializer.Deserialize<List<WordLemmaMappingEvidenceEntry>>(jsonStream, JsonOptions)
            ?? throw new InvalidDataException("word-lemma-mapping-evidence.json could not be parsed.");

        return evidence;
    }

    internal static WordLemmaNormalizationCounts ComputeCounts(WordLemmaNormalizationArtifact artifact)
    {
        var add = 0;
        var remove = 0;
        var replace = 0;
        var keep = 0;
        var exception = 0;
        var approved = 0;
        var acceptedException = 0;
        var candidateOrNeedsReview = 0;

        foreach (var entry in artifact.Entries)
        {
            switch (entry.OperationKind)
            {
                case WordLemmaNormalizationOperationKind.Add: add++; break;
                case WordLemmaNormalizationOperationKind.Remove: remove++; break;
                case WordLemmaNormalizationOperationKind.Replace: replace++; break;
                case WordLemmaNormalizationOperationKind.Keep: keep++; break;
                case WordLemmaNormalizationOperationKind.Exception: exception++; break;
            }

            switch (entry.DecisionStatus)
            {
                case WordLemmaNormalizationDecisionStatus.Approved: approved++; break;
                case WordLemmaNormalizationDecisionStatus.AcceptedException: acceptedException++; break;
                case WordLemmaNormalizationDecisionStatus.Candidate:
                case WordLemmaNormalizationDecisionStatus.NeedsReview:
                    candidateOrNeedsReview++;
                    break;
                default:
                    candidateOrNeedsReview++;
                    break;
            }
        }

        return new WordLemmaNormalizationCounts(
            artifact.Entries.Count, add, remove, replace, keep, exception,
            approved, acceptedException, candidateOrNeedsReview);
    }
}

public sealed record WordLemmaNormalizationLoaded(
    WordLemmaNormalizationArtifact Artifact,
    IReadOnlyList<WordLemmaMappingEvidenceEntry> Evidence,
    string ArtifactSha256,
    WordLemmaNormalizationCounts Counts);
