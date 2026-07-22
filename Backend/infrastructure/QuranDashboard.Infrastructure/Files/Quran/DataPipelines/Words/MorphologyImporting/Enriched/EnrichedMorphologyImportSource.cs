using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

public sealed class EnrichedMorphologyImportSource : IMorphologyImportSource
{
    private readonly EnrichedMorphologyManifestReader manifestReader;
    private readonly EnrichedMorphologyReader sourceReader;
    private readonly EnrichedDimensionBuilder dimensionBuilder;

    private EnrichedMorphologyFileDigest? capturedDigest;

    public EnrichedMorphologyImportSource(
        EnrichedMorphologyManifestReader manifestReader,
        EnrichedMorphologyReader sourceReader,
        EnrichedDimensionBuilder dimensionBuilder)
    {
        this.manifestReader = manifestReader;
        this.sourceReader = sourceReader;
        this.dimensionBuilder = dimensionBuilder;
    }

    public async Task<MorphologySourceData> LoadAsync(string sourcePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var manifest = await manifestReader.ReadAsync(sourcePath, ct);

        await manifestReader.ValidateRecordAndSegmentCountsAsync(
            manifest.FullPath, manifest.RecordCount, manifest.SegmentCount, ct);

        capturedDigest = await manifestReader.CaptureDigestAsync(manifest.FullPath, ct);

        var build = await dimensionBuilder.BuildAsync(
            sourceReader.ReadAsync(manifest.FullPath, ct), manifest.RecordCount, ct);

        if (build.Words.Count != manifest.RecordCount)
        {
            throw new InvalidDataException(
                $"Enriched morphology record count mismatch after read: expected={manifest.RecordCount}, " +
                $"observed={build.Words.Count}.");
        }

        var emptyLocationMap = new Dictionary<string, string>(StringComparer.Ordinal);

        var alignedWords = build.Words.Select(projection => projection.Word).ToList();

        return new MorphologySourceData(
            alignedWords,
            emptyLocationMap,
            emptyLocationMap,
            emptyLocationMap,
            build.ResolvedRoots,
            build.ResolvedLemmas,
            build.ResolvedStems,
            build.CharsetWarnings,
            build.UnknownPosCodes,
            new MorphologyRenderStats(
                build.WholeWordAgreementMatches,
                alignedWords.Count,
                Array.Empty<string>(),
                Array.Empty<string>(),
                build.EmptyFormLocations),
            Array.Empty<SegmentDimensionIssue>(),
            CorrectionSummary: null,
            SourceKind: MorphologyImportSourceKind.Enriched,
            LemmaAnalyses: build.ResolvedLemmaAnalyses);
    }

    public async Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct)
    {
        if (capturedDigest is null)
        {
            throw new InvalidOperationException(
                "Enriched source digest was not captured. Call LoadAsync first.");
        }

        var manifest = await manifestReader.ReadAsync(sourcePath, ct);
        return await manifestReader.VerifyDigestUnchangedAsync(manifest.FullPath, capturedDigest, ct);
    }
}
