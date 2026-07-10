using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.Tests.Quran.WordsMorphologyEnriched;

public sealed class EnrichedMorphologyReaderTests : IDisposable
{
    private readonly string tempDir;
    private readonly EnrichedMorphologyReader reader = new();

    public EnrichedMorphologyReaderTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "enriched-morph-reader-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
    }

    [Fact]
    public async Task Reads_records_and_segments_including_audit_only_fields()
    {
        var path = Path.Combine(tempDir, "slice.json");
        await File.WriteAllTextAsync(path, """
            [
              {
                "location": "1:1:1",
                "surah": 1,
                "ayah": 1,
                "wordNumber": 1,
                "quranWordId": 1,
                "quranWordIdVerifiedAgainstDashboard": true,
                "textUthmani": "نَصٌّ تَجْرِيبِيّ",
                "corpusPresent": true,
                "segments": [
                  {
                    "segmentNumber": 1,
                    "formBuckwalter": "bi",
                    "formArabic": "مُقَدِّمَة",
                    "kind": "PREFIX",
                    "featuresRaw": "PREFIX|bi+",
                    "lemmaArabicMappingStatus": "NOT_APPLICABLE"
                  },
                  {
                    "segmentNumber": 2,
                    "formBuckwalter": "somi",
                    "formArabic": "جِذْعٌ تَجْرِيبِيّ",
                    "kind": "STEM",
                    "pos": "N",
                    "featuresRaw": "STEM|POS:N|LEM:{som|ROOT:smw|M|GEN",
                    "lemmaBuckwalter": "{som",
                    "lemmaArabic": "لِمَةٌ تَجْرِيبِيَّة",
                    "lemmaArabicQulCanonical": "لِمَةٌ تَجْرِيبِيَّة",
                    "rootBuckwalter": "smw",
                    "rootArabic": "جذر تجريبي",
                    "stemBuckwalter": "somi",
                    "stemArabic": "جِذْعٌ تَجْرِيبِيّ",
                    "stemArabicQulCanonical": "جِذْعٌ تَجْرِيبِيّ",
                    "lemmaArabicMappingStatus": "CONFIRMED_IN_QUL_DICTIONARY"
                  }
                ]
              }
            ]
            """);

        var records = new List<EnrichedMorphologyRecord>();
        await foreach (var record in reader.ReadAsync(path, CancellationToken.None))
        {
            records.Add(record);
        }

        records.Should().ContainSingle();
        var only = records.Single();
        only.Location.Should().Be("1:1:1");
        only.QuranWordId.Should().Be(1);
        only.CorpusPresent.Should().BeTrue();
        only.QuranWordIdVerifiedAgainstDashboard.Should().BeTrue();
        only.Segments.Should().HaveCount(2);

        var stem = only.Segments.Single(segment => segment.Kind == "STEM");
        stem.FormBuckwalter.Should().Be("somi");
        stem.FormArabic.Should().Be("جِذْعٌ تَجْرِيبِيّ");
        stem.Pos.Should().Be("N");
        stem.RootBuckwalter.Should().Be("smw");
        stem.RootArabic.Should().Be("جذر تجريبي");
        stem.LemmaBuckwalter.Should().Be("{som");
        stem.LemmaArabic.Should().Be("لِمَةٌ تَجْرِيبِيَّة");
        stem.StemBuckwalter.Should().Be("somi", "stemBuckwalter is read for audit/diagnostics only");
    }

    [Fact]
    public async Task Throws_when_file_missing()
    {
        var act = async () =>
        {
            await foreach (var _ in reader.ReadAsync(Path.Combine(tempDir, "missing.json"), CancellationToken.None))
            {
            }
        };

        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}
