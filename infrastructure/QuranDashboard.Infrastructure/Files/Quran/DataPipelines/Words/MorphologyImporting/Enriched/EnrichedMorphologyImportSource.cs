using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

// Enriched morphology import source (Feature 020). Reads the single Dashboard-ready artifact through the
// streaming EnrichedMorphologyReader, validates sha256/size/recordCount/segmentCount via the manifest,
// then builds value-based root/lemma/stem dimensions and projects the result onto the existing
// MorphologySourceData DTO consumed unchanged by EfBulkMorphologyWriter.
//
// This pathway does NOT consult QUL word-level location links, does NOT apply the legacy
// WordLemmaNormalization / SegmentStemCorrection / CuratedLemmaDisambiguation artifacts, and does NOT
// use the old MorphologyAssembler. The old MorphologyImportSource + correction path stays registered in
// parallel (selected via the CLI --enriched flag / DI selector) so parity diffing is possible before the
// cut-over cleanup in Phase 2.
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

        // Structural gate: refuse before opening the writer if the artifact's record/segment counts drift
        // from the manifest. The check streams the JSON once (no full materialization) and runs ahead of
        // the dimension build.
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

        // The legacy MorphologySourceData carries roots/lemmas/stems maps (location -> value) that the
        // old assembler populated from QUL files. The enriched pathway has no QUL location map; these
        // maps are NOT consumed by EfBulkMorphologyWriter for the enriched DTO (resolved dimension lists
        // are the source of truth). They are kept empty here so the DTO shape stays compatible without
        // fabricating location-keyed QUL data the pathway explicitly rejects.
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
                // The enriched pathway does not produce review/multiword tiers (every form is display-clean
                // from the artifact), so these lists are intentionally empty.
                Array.Empty<string>(),
                Array.Empty<string>(),
                build.EmptyFormLocations),
            // No segment-dimension issues: the enriched pathway resolves every STEM segment value-based
            // against its own buckwalter; there is no QUL fanout to fail closed on.
            Array.Empty<SegmentDimensionIssue>(),
            // No legacy word-lemma correction summary: the enriched pathway does not run
            // WordLemmaNormalization. The report writer only renders that section when non-null.
            CorrectionSummary: null,
            SourceKind: MorphologyImportSourceKind.Enriched,
            // Per-buckwalter analytical breakdown under each collapsed display lemma (Feature 020).
            LemmaAnalyses: build.ResolvedLemmaAnalyses);
    }

    public async Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct)
    {
        if (capturedDigest is null)
        {
            throw new InvalidOperationException(
                "Enriched source digest was not captured. Call LoadAsync first.");
        }

        // Re-resolve the manifest so the source-path semantics match LoadAsync (sourcePath is a folder).
        var manifest = await manifestReader.ReadAsync(sourcePath, ct);
        return await manifestReader.VerifyDigestUnchangedAsync(manifest.FullPath, capturedDigest, ct);
    }
}
