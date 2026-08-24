using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

public sealed class EnrichedMorphologyImportSource : IMorphologyImportSource
{
    private readonly EnrichedMorphologyManifestReader manifestReader;
    private readonly EnrichedMorphologyReader sourceReader;
    private readonly EnrichedDimensionBuilder dimensionBuilder;
    private readonly ApprovedRootFallbackReader? rootFallbackReader;
    private readonly ApprovedRootFallbackApplier? rootFallbackApplier;

    private EnrichedMorphologyFileDigest? capturedDigest;
    private string? capturedRootFallbackDigest;

    internal EnrichedMorphologyImportSource(
        EnrichedMorphologyManifestReader manifestReader,
        EnrichedMorphologyReader sourceReader,
        EnrichedDimensionBuilder dimensionBuilder)
    {
        this.manifestReader = manifestReader;
        this.sourceReader = sourceReader;
        this.dimensionBuilder = dimensionBuilder;
    }

    public EnrichedMorphologyImportSource(
        EnrichedMorphologyManifestReader manifestReader,
        EnrichedMorphologyReader sourceReader,
        EnrichedDimensionBuilder dimensionBuilder,
        ApprovedRootFallbackReader rootFallbackReader,
        ApprovedRootFallbackApplier rootFallbackApplier)
    {
        this.manifestReader = manifestReader;
        this.sourceReader = sourceReader;
        this.dimensionBuilder = dimensionBuilder;
        this.rootFallbackReader = rootFallbackReader;
        this.rootFallbackApplier = rootFallbackApplier;
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
        var records = sourceReader.ReadAsync(manifest.FullPath, ct);
        ApprovedRootFallbackApplication? fallbackApplication = null;
        if (manifest.RecordCount == MorphologyInvariants.ExpectedReadableWords
            && rootFallbackReader is not null
            && rootFallbackApplier is not null)
        {
            var fallback = rootFallbackReader.Load();
            capturedRootFallbackDigest = fallback.ArtifactSha256;
            fallbackApplication = rootFallbackApplier.Create(fallback);
            records = fallbackApplication.ApplyAsync(records, ct);
        }

        var build = await dimensionBuilder.BuildAsync(
            records,
            manifest.RecordCount,
            ct);
        var fallbackSummary = fallbackApplication?.Complete();

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
            LemmaAnalyses: build.ResolvedLemmaAnalyses,
            RootFallbackSummary: fallbackSummary);
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
        var sourceUnchanged = await manifestReader.VerifyDigestUnchangedAsync(manifest.FullPath, capturedDigest, ct);
        if (rootFallbackReader is null || capturedRootFallbackDigest is null)
        {
            return sourceUnchanged;
        }

        var fallbackUnchanged = string.Equals(
            rootFallbackReader.Load().ArtifactSha256,
            capturedRootFallbackDigest,
            StringComparison.Ordinal);
        return sourceUnchanged && fallbackUnchanged;
    }
}
