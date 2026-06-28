using System.Reflection;
using System.Security.Cryptography;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

// Loads the embedded word-level lemma normalization artifact and its companion Buckwalter→Arabic
// mapping-evidence artifact, computes a SHA-256 over the raw artifact bytes, parses both into typed
// records, and validates + applies the artifact to an in-memory raw QUL lemma map.
//
// Phase 1 scope: load + schema-validate + apply in-memory. Not yet wired into MorphologyImportSource.
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

    // Loaded + parsed artifact with computed hash and entry counts. Validation (ValidateSchema) is
    // invoked eagerly so a malformed embedded artifact fails closed at load, before any apply.
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

    // Loaded mapping-evidence artifact only. Used directly by tests and by the validator when a
    // custom (test) artifact is supplied.
    public IReadOnlyList<WordLemmaMappingEvidenceEntry> LoadEvidence()
    {
        using var stream = OpenResource(EvidenceResourceName);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return ParseEvidence(memory.ToArray());
    }

    // Validates + applies the loaded artifact to a copy of the raw QUL word-level lemma map.
    // Never mutates `rawLemmas`. Fails closed on any expected-current mismatch or unapplied op.
    //
    // `readableWordLocations` is the set of readable quran_words locations; when non-null, every
    // correction location must exist in it (defense against an artifact that drifts away from the
    // readable Quran word set).
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

        // Stamp the summary with the artifact hash now that apply succeeded.
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

// Result of Load(): parsed artifact, parsed mapping-evidence, raw artifact SHA-256, and entry counts.
public sealed record WordLemmaNormalizationLoaded(
    WordLemmaNormalizationArtifact Artifact,
    IReadOnlyList<WordLemmaMappingEvidenceEntry> Evidence,
    string ArtifactSha256,
    WordLemmaNormalizationCounts Counts);
