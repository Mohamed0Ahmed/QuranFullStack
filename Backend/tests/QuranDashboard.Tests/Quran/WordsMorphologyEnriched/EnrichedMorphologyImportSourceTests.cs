using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.Tests.Quran.WordsMorphologyEnriched;

public sealed class EnrichedMorphologyImportSourceTests : IDisposable
{
    private readonly string tempRoot;
    private readonly EnrichedMorphologyManifestReader manifestReader = new();
    private readonly EnrichedMorphologyReader sourceReader = new();
    private readonly EnrichedDimensionBuilder dimensionBuilder = new();

    public EnrichedMorphologyImportSourceTests()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "enriched-morph-source-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Fact]
    public async Task LoadAsync_produces_morphology_source_data_with_value_based_dimensions()
    {
        await WriteArtifactAsync("""
            [
              {
                "location": "1:1:1", "quranWordId": 1, "textUthmani": "صِيغَةٌ تَجْرِيبِيَّة أ", "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  {
                    "segmentNumber": 1, "kind": "STEM", "pos": "V",
                    "formBuckwalter": "synA", "formArabic": "صِيغَةٌ تَجْرِيبِيَّة أ",
                    "featuresRaw": "STEM|POS:V|PERF|ACT",
                    "rootBuckwalter": "rootA", "rootArabic": "جذر تجريبي أ",
                    "lemmaBuckwalter": "lemmaA", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة أ",
                    "stemBuckwalter": "synA", "stemArabic": "صِيغَةٌ تَجْرِيبِيَّة أ"
                  }
                ]
              },
              {
                "location": "1:1:2", "quranWordId": 2, "textUthmani": "صِيغَةٌ تَجْرِيبِيَّة ب", "corpusPresent": true,
                "quranWordIdVerifiedAgainstDashboard": true,
                "segments": [
                  {
                    "segmentNumber": 1, "kind": "STEM", "pos": "N",
                    "formBuckwalter": "synB", "formArabic": "صِيغَةٌ تَجْرِيبِيَّة ب",
                    "featuresRaw": "STEM|POS:N|M|GEN",
                    "rootBuckwalter": "rootA", "rootArabic": "جذر تجريبي أ",
                    "lemmaBuckwalter": "lemmaB", "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة ب",
                    "stemBuckwalter": "synB", "stemArabic": "صِيغَةٌ تَجْرِيبِيَّة ب"
                  }
                ]
              }
            ]
            """);

        var source = new EnrichedMorphologyImportSource(manifestReader, sourceReader, dimensionBuilder);
        var data = await source.LoadAsync(tempRoot, CancellationToken.None);

        data.Words.Should().HaveCount(2);
        data.ResolvedRoots.Should().ContainSingle("both words share root 'rootA'");
        data.ResolvedLemmas.Should().HaveCount(2);
        data.ResolvedStems.Should().HaveCount(2);
        data.SourceKind.Should().Be(MorphologyImportSourceKind.Enriched);
        data.UnknownPosCodes.Should().BeEmpty("V and N are both in PosTagSeed");
        data.SegmentDimensionIssues.Should().BeEmpty("enriched pathway resolves value-based with no fanout");

        var firstWord = data.Words[0];
        firstWord.IsVerb.Should().BeTrue();
        firstWord.HeadPos.Should().Be("V");
        firstWord.Segments.Single().RootId.Should().Be(data.ResolvedRoots.Single().AssignedId);
    }

    [Fact]
    public async Task LoadAsync_refuses_on_record_count_mismatch()
    {
        await WriteArtifactAsync("[{\"location\":\"1:1:1\",\"quranWordId\":1,\"segments\":[]},{\"location\":\"1:1:2\",\"quranWordId\":2,\"segments\":[]}]", recordCount: 5, segmentCount: 0);

        var source = new EnrichedMorphologyImportSource(manifestReader, sourceReader, dimensionBuilder);

        var act = async () => await source.LoadAsync(tempRoot, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidDataException>())
            .WithMessage("*record count mismatch*");
    }

    [Fact]
    public async Task SourceUnchangedAsync_throws_when_load_not_called()
    {
        var source = new EnrichedMorphologyImportSource(manifestReader, sourceReader, dimensionBuilder);

        var act = async () => await source.SourceUnchangedAsync(tempRoot, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*digest was not captured*");
    }

    private async Task WriteArtifactAsync(string artifactJson, int? recordCount = null, int? segmentCount = null)
    {
        var artifactPath = Path.Combine(tempRoot, "corpus-based-enriched-morphology.dashboard-ready.json");
        await File.WriteAllTextAsync(artifactPath, artifactJson);
        var bytes = await File.ReadAllBytesAsync(artifactPath);
        var sha = Convert.ToHexString(SHA256.HashData(bytes));

        int resolvedRecordCount;
        int resolvedSegmentCount;
        if (recordCount.HasValue && segmentCount.HasValue)
        {
            resolvedRecordCount = recordCount.Value;
            resolvedSegmentCount = segmentCount.Value;
        }
        else
        {
            using var doc = JsonDocument.Parse(artifactJson);
            resolvedRecordCount = doc.RootElement.GetArrayLength();
            resolvedSegmentCount = doc.RootElement.EnumerateArray()
                .Sum(record => record.TryGetProperty("segments", out var segs) && segs.ValueKind == JsonValueKind.Array
                    ? segs.GetArrayLength() : 0);
        }

        var manifestJson = $$"""
            {
              "schema": "quran-enriched-morphology-v1",
              "provenance": "corpus-bridge-enriched",
              "quranWordIdVerifiedAgainstDashboard": true,
              "files": [
                {
                  "role": "enriched-morphology",
                  "relativePath": "corpus-based-enriched-morphology.dashboard-ready.json",
                  "sha256": "{{sha}}",
                  "sizeBytes": {{bytes.Length}},
                  "recordCount": {{resolvedRecordCount}},
                  "segmentCount": {{resolvedSegmentCount}}
                }
              ]
            }
            """;
        await File.WriteAllTextAsync(Path.Combine(tempRoot, "manifest.json"), manifestJson);
    }
}
