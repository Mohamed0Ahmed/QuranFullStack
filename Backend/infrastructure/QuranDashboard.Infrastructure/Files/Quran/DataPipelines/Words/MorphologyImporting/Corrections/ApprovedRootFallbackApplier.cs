using System.Runtime.CompilerServices;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

public sealed class ApprovedRootFallbackApplier
{
    public ApprovedRootFallbackApplication Create(ApprovedRootFallbackLoaded loaded)
    {
        ArgumentNullException.ThrowIfNull(loaded);
        return new ApprovedRootFallbackApplication(loaded);
    }
}

public sealed class ApprovedRootFallbackApplication
{
    private readonly ApprovedRootFallbackLoaded loaded;
    private readonly HashSet<string> appliedLocations = new(StringComparer.Ordinal);

    public ApprovedRootFallbackApplication(ApprovedRootFallbackLoaded loaded)
    {
        this.loaded = loaded;
    }

    public async IAsyncEnumerable<EnrichedMorphologyRecord> ApplyAsync(
        IAsyncEnumerable<EnrichedMorphologyRecord> records,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var sourceRecord in records.WithCancellation(ct))
        {
            if (!loaded.EntriesByLocation.TryGetValue(sourceRecord.Location ?? string.Empty, out var entry))
            {
                yield return sourceRecord;
                continue;
            }

            yield return Apply(sourceRecord, entry);
        }
    }

    public ApprovedRootFallbackSummary Complete()
    {
        var missing = loaded.Artifact.Entries
            .Where(entry => !appliedLocations.Contains(entry.Location))
            .Select(entry => entry.Location)
            .ToList();

        if (missing.Count > 0 || appliedLocations.Count != loaded.Artifact.ExpectedAppliedCount)
        {
            throw new InvalidDataException(
                $"Approved-root fallback application failed: applied={appliedLocations.Count}, "
                + $"expected={loaded.Artifact.ExpectedAppliedCount}, missing={string.Join(",", missing)}.");
        }

        return new ApprovedRootFallbackSummary(
            loaded.ArtifactSha256,
            loaded.Artifact.Source,
            loaded.Artifact.ExpectedAppliedCount,
            appliedLocations.Count,
            CountStatus("strong"),
            CountStatus("linguistic"),
            CountStatus("lexical"));
    }

    private EnrichedMorphologyRecord Apply(
        EnrichedMorphologyRecord sourceRecord,
        ApprovedRootFallbackEntry entry)
    {
        if (!appliedLocations.Add(entry.Location))
        {
            throw new InvalidDataException(
                $"Enriched morphology source contains duplicate approved-root location '{entry.Location}'.");
        }

        if (sourceRecord.QuranWordId != entry.QuranWordId)
        {
            throw new InvalidDataException(
                $"Approved-root fallback '{entry.Location}' expected quranWordId={entry.QuranWordId}, "
                + $"observed={sourceRecord.QuranWordId?.ToString() ?? "null"}.");
        }

        var target = sourceRecord.Segments.SingleOrDefault(segment => segment.SegmentNumber == entry.SegmentNumber)
            ?? throw new InvalidDataException(
                $"Approved-root fallback '{entry.Location}' did not find segment {entry.SegmentNumber}.");

        if (!string.Equals(target.Kind, "STEM", StringComparison.Ordinal)
            || !string.Equals(target.LemmaBuckwalter, entry.ExpectedLemmaBuckwalter, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Approved-root fallback '{entry.Location}' no longer matches the expected STEM lemma.");
        }

        if (!string.IsNullOrWhiteSpace(target.RootBuckwalter)
            || !string.IsNullOrWhiteSpace(target.RootArabic))
        {
            throw new InvalidDataException(
                $"Approved-root fallback '{entry.Location}' refuses to overwrite an existing source root.");
        }

        var segments = sourceRecord.Segments
            .Select(segment => segment.SegmentNumber == entry.SegmentNumber
                ? segment with
                {
                    RootBuckwalter = entry.RootBuckwalter,
                    RootArabic = entry.RootArabic,
                }
                : segment)
            .ToList();

        return sourceRecord with { Segments = segments };
    }

    private int CountStatus(string status) =>
        loaded.Artifact.Entries.Count(entry => string.Equals(entry.ReviewStatus, status, StringComparison.Ordinal));
}
