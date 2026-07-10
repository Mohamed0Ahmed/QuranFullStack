using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.Tests.Quran.WordsMorphologyEnriched;

public sealed class EnrichedMorphologyDryValidatorTests
{
    [Fact]
    public void Flag_failing_check_when_record_count_wrong()
    {
        var source = new MorphologySourceData(
            Words: Array.Empty<AlignedWordDto>(),
            Roots: new Dictionary<string, string>(),
            Lemmas: new Dictionary<string, string>(),
            Stems: new Dictionary<string, string>(),
            ResolvedRoots: Array.Empty<ResolvedRootDto>(),
            ResolvedLemmas: Array.Empty<ResolvedLemmaDto>(),
            ResolvedStems: Array.Empty<ResolvedStemDto>(),
            CharsetWarnings: Array.Empty<string>(),
            UnknownPosCodes: Array.Empty<string>(),
            RenderStats: new MorphologyRenderStats(0, 0, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()),
            SegmentDimensionIssues: Array.Empty<SegmentDimensionIssue>());

        var manifest = new EnrichedMorphologyManifest(
            "path", "sha", 1L, 0, 0,
            QuranWordIdVerifiedAgainstDashboard: true,
            Provenance: "corpus-bridge-enriched",
            SourceArtifact: null);

        var result = new EnrichedMorphologyDryValidator().Validate(source, manifest);

        result.AllPassed.Should().BeFalse();
        result.Checks.Should().Contain(check =>
            check.Id == "ENRICHED-RECORD-COUNT" && !check.Passed);
    }

    [Fact]
    public void Flags_when_provenance_missing()
    {
        var source = new MorphologySourceData(
            Words: Array.Empty<AlignedWordDto>(),
            Roots: new Dictionary<string, string>(),
            Lemmas: new Dictionary<string, string>(),
            Stems: new Dictionary<string, string>(),
            ResolvedRoots: Array.Empty<ResolvedRootDto>(),
            ResolvedLemmas: Array.Empty<ResolvedLemmaDto>(),
            ResolvedStems: Array.Empty<ResolvedStemDto>(),
            CharsetWarnings: Array.Empty<string>(),
            UnknownPosCodes: Array.Empty<string>(),
            RenderStats: new MorphologyRenderStats(0, 0, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()),
            SegmentDimensionIssues: Array.Empty<SegmentDimensionIssue>());

        var manifest = new EnrichedMorphologyManifest(
            "path", "sha", 1L, 0, 0,
            QuranWordIdVerifiedAgainstDashboard: false,
            Provenance: null,
            SourceArtifact: null);

        var result = new EnrichedMorphologyDryValidator().Validate(source, manifest);

        result.Checks.Should().Contain(check =>
            check.Id == "ENRICHED-NO-QUL-WORD-LEMMA-LINK" && !check.Passed);
    }
}
