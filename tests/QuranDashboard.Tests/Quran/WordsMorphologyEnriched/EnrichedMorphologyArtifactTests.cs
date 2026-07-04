using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.Tests.Quran.WordsMorphologyEnriched;

// End-to-end validation against the real staged Dashboard-ready artifact. These tests are SKIPPED when
// the 96 MB artifact is not present (it is gitignored and only staged locally for Feature 020). When the
// artifact IS present, they enforce every signed-off count + boundary + corrected-lemma gate from the
// implementation plan §7. No DB is touched: the assertions run on the in-memory MorphologySourceData
// produced by EnrichedMorphologyImportSource.LoadAsync.
public sealed class EnrichedMorphologyArtifactTests
{
    public static IEnumerable<object[]> BoundaryLocations => new[]
    {
        new object[] { "2:181:14" },
        new object[] { "13:37:20" },
    };

    [EnrichedArtifactFact]
    public async Task Artifact_record_count_is_exactly_77432()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        data.Words.Count.Should().Be(77_432, "the dashboard-ready artifact carries exactly 77,432 word records");
    }

    [EnrichedArtifactFact]
    public async Task Artifact_segment_count_is_exactly_128219()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var segmentCount = data.Words.Sum(word => word.Segments.Count);
        segmentCount.Should().Be(128_219, "the dashboard-ready artifact carries exactly 128,219 segments");
    }

    [EnrichedArtifactFact]
    public async Task Artifact_has_no_unknown_pos_codes()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        data.UnknownPosCodes.Should().BeEmpty("every enriched pos code must resolve against PosTagSeed");
    }

    [EnrichedArtifactFact]
    public async Task Artifact_has_no_duplicate_word_locations()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var duplicateLocations = data.Words
            .GroupBy(word => word.Location)
            .Where(group => group.Count() > 1)
            .ToList();

        duplicateLocations.Should().BeEmpty("every word location appears exactly once");
    }

    [EnrichedArtifactFact]
    public async Task Artifact_has_no_duplicate_word_segment_keys()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var duplicateKeys = data.Words
            .SelectMany(word => word.Segments.Select(segment => $"{word.Location}:{segment.SegmentNumber}"))
            .GroupBy(key => key)
            .Where(group => group.Count() > 1)
            .ToList();

        duplicateKeys.Should().BeEmpty("every (quranWordId, segmentNumber) pair is unique");
    }

    [EnrichedArtifactFact]
    public async Task Boundary_ayah_2_181_is_represented()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var wordCount = data.Words.Count(word => word.Location.StartsWith("2:181:", StringComparison.Ordinal));
        wordCount.Should().Be(14, "2:181 has 14 readable words per the dashboard-ready artifact");
    }

    [EnrichedArtifactFact]
    public async Task Boundary_ayah_2_282_is_represented()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var wordCount = data.Words.Count(word => word.Location.StartsWith("2:282:", StringComparison.Ordinal));
        wordCount.Should().Be(128);
    }

    [EnrichedArtifactFact]
    public async Task Boundary_ayah_8_6_is_represented()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var wordCount = data.Words.Count(word => word.Location.StartsWith("8:6:", StringComparison.Ordinal));
        wordCount.Should().Be(12);
    }

    [EnrichedArtifactFact]
    public async Task Boundary_ayah_13_37_is_represented()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var wordCount = data.Words.Count(word => word.Location.StartsWith("13:37:", StringComparison.Ordinal));
        wordCount.Should().Be(20);
    }

    [EnrichedArtifactTheory]
    [MemberData(nameof(BoundaryLocations))]
    public async Task Boundary_word_carries_real_segments(string location)
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var word = data.Words.Single(word => word.Location == location);
        word.Segments.Should().NotBeEmpty($"{location} must carry real segments (no pseudo-segments, no fallback)");
    }

    [EnrichedArtifactFact]
    public async Task Boundary_word_8_6_12_has_exactly_two_segments()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var word = data.Words.Single(word => word.Location == "8:6:12");
        word.Segments.Should().HaveCount(2, "8:6:12 is the formerly-missing word carrying 2 real segments");
    }

    [EnrichedArtifactFact]
    public async Task Corrected_lemma_41_44_16_is_not_ءَامَنَ()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var word = data.Words.Single(word => word.Location == "41:44:16");
        var lemma = data.ResolvedLemmas.Single(lemma => lemma.AssignedId == word.LemmaId);
        lemma.LemmaText.Should().NotBe("ءَامَنَ", "the QUL-shift defect is fixed by value-based Corpus identity");
        lemma.LemmaText.Should().Be("شِفَاء", "the Corpus-derived lemma for root ش ف ي");
        lemma.LemmaBuckwalter.Should().Be("$ifaA^'");
    }

    [EnrichedArtifactFact]
    public async Task Corrected_lemma_11_29_17_is_not_ءَامَنَ()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var word = data.Words.Single(word => word.Location == "11:29:17");
        var lemma = data.ResolvedLemmas.Single(lemma => lemma.AssignedId == word.LemmaId);
        lemma.LemmaText.Should().NotBe("ءَامَنَ");
        // Corpus-derived lemma for root ل ق ي.
        lemma.LemmaText.Should().Be("مُّلَٰقُوا");
        lemma.LemmaBuckwalter.Should().Be("m~ula`quwA");
    }

    [EnrichedArtifactFact]
    public async Task Corrected_lemma_2_102_41_is_not_فَرَّقُ()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var word = data.Words.Single(word => word.Location == "2:102:41");
        var lemma = data.ResolvedLemmas.Single(lemma => lemma.AssignedId == word.LemmaId);
        lemma.LemmaText.Should().NotBe("فَرَّقُ");
        lemma.LemmaText.Should().Be("مَرْء", "the Corpus-derived lemma for root م ر ا");
        lemma.LemmaBuckwalter.Should().Be("maro'");
    }

    [EnrichedArtifactFact]
    public async Task Corrected_lemma_2_144_20_is_not_كَانَ()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var word = data.Words.Single(word => word.Location == "2:144:20");
        var lemma = data.ResolvedLemmas.Single(lemma => lemma.AssignedId == word.LemmaId);
        lemma.LemmaText.Should().NotBe("كَانَ");
        lemma.LemmaText.Should().Be("شَطْر", "the Corpus-derived lemma for root ش ط ر");
        lemma.LemmaBuckwalter.Should().Be("$aTor");
    }

    [EnrichedArtifactFact]
    public async Task No_buckwalter_in_arabic_display_fields()
    {
        var data = await EnrichedMorphologyArtifactFixture.LoadAsync();

        var offendingSegmentForms = data.Words
            .SelectMany(word => word.Segments)
            .Where(segment => ContainsBuckwalterGlyph(segment.FormArabicNormalized))
            .ToList();
        offendingSegmentForms.Should().BeEmpty("form_arabic_normalized must be Arabic, not Buckwalter");

        var offendingRoots = data.ResolvedRoots
            .Where(root => ContainsBuckwalterGlyph(root.RootText))
            .ToList();
        offendingRoots.Should().BeEmpty("root_text must be Arabic, not Buckwalter");

        var offendingLemmas = data.ResolvedLemmas
            .Where(lemma => ContainsBuckwalterGlyph(lemma.LemmaText))
            .ToList();
        offendingLemmas.Should().BeEmpty("lemma_text must be Arabic, not Buckwalter");

        var offendingStems = data.ResolvedStems
            .Where(stem => ContainsBuckwalterGlyph(stem.StemText))
            .ToList();
        offendingStems.Should().BeEmpty("stem_text must be Arabic, not Buckwalter");
    }

    private static bool ContainsBuckwalterGlyph(string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Any(static ch => ch is >= 'A' and <= 'Z'
            || ch is >= 'a' and <= 'z'
            || "'$><&}{*~`^|".IndexOf(ch) >= 0);
}
