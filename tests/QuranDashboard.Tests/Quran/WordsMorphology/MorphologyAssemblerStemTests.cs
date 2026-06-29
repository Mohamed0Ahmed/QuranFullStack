using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

// Per-segment stem_id resolution (Feature 018). Non-STEM => null; single-STEM and two-STEM primary
// segments reuse the word head stem; the two-STEM secondary segment uses the curated artifact (clean
// stem by text) or stays null when not covered / intentionally unresolved.
public sealed class MorphologyAssemblerStemTests
{
    private static MorphologyAssembler CreateAssembler() =>
        new(new SegmentArabicRenderer(new BuckwalterArabicMap()));

    private static readonly Dictionary<string, string> Empty = new();

    [Fact]
    public void Single_stem_segment_uses_word_head_stem_id()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            new("1:1:1", "X1", [new AlignedCorpusSegment(1, "STEM", "N", "kataba", "NOM", null, null)]),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        var stems = new Dictionary<string, string> { ["1:1:1"] = "STEM_A" };

        var result = CreateAssembler().Assemble(corpus, ids, Empty, Empty, stems);

        var word = result.Words.Single();
        word.StemId.Should().NotBeNull();
        word.Segments.Single().StemId.Should().Be(word.StemId);
    }

    [Fact]
    public void Non_stem_segment_never_receives_stem_id()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            new("1:1:1", "X1",
            [
                new AlignedCorpusSegment(1, "PREFIX", "P", "bi", "PREFIX", null, null),
                new AlignedCorpusSegment(2, "STEM", "N", "kataba", "NOM", null, null),
            ]),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        var stems = new Dictionary<string, string> { ["1:1:1"] = "STEM_A" };

        var result = CreateAssembler().Assemble(corpus, ids, Empty, Empty, stems);

        var word = result.Words.Single();
        word.Segments.Single(s => s.Kind == "PREFIX").StemId.Should().BeNull();
        word.Segments.Single(s => s.Kind == "STEM").StemId.Should().Be(word.StemId);
    }

    [Fact]
    public void Two_stem_primary_uses_head_and_secondary_maps_to_curated_clean_stem()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            new("1:1:1", "X1",
            [
                new AlignedCorpusSegment(1, "STEM", "P", "mi", "STEM|POS:P", null, null),
                new AlignedCorpusSegment(2, "STEM", "REL", "m~aA", "STEM|POS:REL", null, null),
            ]),
            new("1:1:2", "X2", [new AlignedCorpusSegment(1, "STEM", "REL", "maA", "STEM|POS:REL", null, null)]),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2 };
        // word head stems: compound مِمَّا head = "MI"; standalone "MAA" supplies the clean target row.
        var stems = new Dictionary<string, string> { ["1:1:1"] = "MI", ["1:1:2"] = "MAA" };
        var corrections = new Dictionary<string, string> { ["1:1:1:2"] = "MAA" };

        var result = CreateAssembler().Assemble(corpus, ids, Empty, Empty, stems, corrections);

        var miStemId = result.ResolvedStems.Single(s => s.StemText == "MI").AssignedId;
        var maaStemId = result.ResolvedStems.Single(s => s.StemText == "MAA").AssignedId;
        var word = result.Words.Single(w => w.Location == "1:1:1");

        word.StemId.Should().Be(miStemId);
        word.Segments[0].StemId.Should().Be(miStemId, "the primary STEM reuses the word head stem");
        word.Segments[1].StemId.Should().Be(maaStemId, "the secondary STEM maps to the curated clean stem, not the head");
    }

    [Fact]
    public void Two_stem_secondary_without_curation_stays_null()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            new("1:1:1", "X1",
            [
                new AlignedCorpusSegment(1, "STEM", "P", "mi", "STEM|POS:P", null, null),
                new AlignedCorpusSegment(2, "STEM", "REL", "m~aA", "STEM|POS:REL", null, null),
            ]),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        var stems = new Dictionary<string, string> { ["1:1:1"] = "MI" };

        var result = CreateAssembler().Assemble(corpus, ids, Empty, Empty, stems);

        var word = result.Words.Single();
        word.Segments[0].StemId.Should().Be(word.StemId);
        word.Segments[1].StemId.Should().BeNull("an uncovered secondary stays null; coverage is enforced by the hard checks");
    }

    [Fact]
    public void Two_stem_secondary_with_unknown_clean_stem_text_stays_null()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            new("1:1:1", "X1",
            [
                new AlignedCorpusSegment(1, "STEM", "P", "mi", "STEM|POS:P", null, null),
                new AlignedCorpusSegment(2, "STEM", "REL", "m~aA", "STEM|POS:REL", null, null),
            ]),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        var stems = new Dictionary<string, string> { ["1:1:1"] = "MI" };
        var corrections = new Dictionary<string, string> { ["1:1:1:2"] = "NO_SUCH_STEM" };

        var result = CreateAssembler().Assemble(corpus, ids, Empty, Empty, stems, corrections);

        result.Words.Single().Segments[1].StemId.Should().BeNull();
    }
}
