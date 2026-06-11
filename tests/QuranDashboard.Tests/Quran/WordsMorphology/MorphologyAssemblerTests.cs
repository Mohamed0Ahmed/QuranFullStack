using QuranDashboard.Infrastructure.Files.Quran.Morphology;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

/// <summary>
/// Pure unit tests for <see cref="MorphologyAssembler"/> dimension resolution and segment
/// rendering — no database. These exercise edge cases the integration fixtures cannot reach
/// (multi-digit locations where ordinal order differs from mushaf/id order, exact occurrence
/// counts, lemma→root co-occurrence links, and non-clean render tiers).
/// </summary>
public sealed class MorphologyAssemblerTests
{
    private static MorphologyAssembler CreateAssembler() =>
        new(new SegmentArabicRenderer(new BuckwalterArabicMap()));

    private static AlignedCorpusWord StemWord(
        string location, string qpcUthmani, string form,
        string features = "NOM", string? root = null, string? lemma = null) =>
        new(location, qpcUthmani,
        [
            new AlignedCorpusSegment(1, "STEM", "N", form, features, root, lemma)
        ]);

    [Fact]
    public void Words_count_equals_actual_occurrences_not_plus_one()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", "X1", "kataba", root: "ktb", lemma: "katab"),
            StemWord("1:1:2", "X2", "kitAbi", root: "ktb", lemma: "kitAb"),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2 };
        var roots = new Dictionary<string, string> { ["1:1:1"] = "ROOT_KTB", ["1:1:2"] = "ROOT_KTB" };
        var lemmas = new Dictionary<string, string> { ["1:1:1"] = "LEMMA_KATAB", ["1:1:2"] = "LEMMA_KITAB" };
        var stems = new Dictionary<string, string> { ["1:1:1"] = "STEM_A", ["1:1:2"] = "STEM_B" };

        var result = CreateAssembler().Assemble(corpus, ids, roots, lemmas, stems);

        result.ResolvedRoots.Single().WordsCount.Should().Be(2, "the root occurs in exactly two words");
        result.ResolvedLemmas.Should().OnlyContain(l => l.WordsCount == 1, "each lemma occurs once");
    }

    [Fact]
    public void First_word_order_is_the_minimum_mushaf_id_not_iteration_order()
    {
        // Ordinal location order ("1:1:10" < "1:1:2") differs from mushaf/id order (9 > 1).
        // first_word_order_in_mushaf must be the minimum id (1), independent of iteration order.
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:10", "X10", "kataba", root: "ktb"),
            StemWord("1:1:2", "X2", "kitAbi", root: "ktb"),
        };
        var ids = new Dictionary<string, int> { ["1:1:10"] = 9, ["1:1:2"] = 1 };
        var roots = new Dictionary<string, string> { ["1:1:10"] = "ROOT_SHARED", ["1:1:2"] = "ROOT_SHARED" };
        var lemmas = new Dictionary<string, string>();
        var stems = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, roots, lemmas, stems);

        result.ResolvedRoots.Single().FirstWordOrderInMushaf.Should().Be(1);
    }

    [Fact]
    public void Lemma_root_id_links_to_co_occurring_root()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", "X1", "kataba", root: "ktb", lemma: "katab"),
            StemWord("1:1:2", "X2", "kitAbi", root: "ktb", lemma: "kitAb"),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2 };
        var roots = new Dictionary<string, string> { ["1:1:1"] = "ROOT_KTB", ["1:1:2"] = "ROOT_KTB" };
        var lemmas = new Dictionary<string, string> { ["1:1:1"] = "LEMMA_KATAB", ["1:1:2"] = "LEMMA_KITAB" };
        var stems = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, roots, lemmas, stems);

        var rootId = result.ResolvedRoots.Single().AssignedId;
        result.ResolvedLemmas.Should().OnlyContain(
            l => l.RootId == rootId, "both lemmas co-occur with the shared root");
        result.ResolvedRoots.Single().DistinctLemmasCount.Should().Be(2);
    }

    [Fact]
    public void Buckwalter_only_word_has_no_dimension_link_but_keeps_segment_buckwalter()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", "X1", "kataba", root: "ktb", lemma: "katab"),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        // QUL has no Arabic root/lemma/stem for this location.
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, empty, empty);

        result.ResolvedRoots.Should().BeEmpty();
        result.ResolvedLemmas.Should().BeEmpty();
        var word = result.Words.Single();
        word.RootId.Should().BeNull();
        word.LemmaId.Should().BeNull();
        word.Segments.Single().RootBuckwalter.Should().Be("ktb");
        word.Segments.Single().LemmaBuckwalter.Should().Be("katab");
    }

    [Fact]
    public void Whole_word_agreement_counts_words_whose_render_matches_qpc_uthmani()
    {
        var matchUthmani = new BuckwalterArabicMap().Transliterate("kataba").Arabic;
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", matchUthmani, "kataba"), // render equals qpcUthmani → match
            StemWord("1:1:2", "MISMATCH", "kitAbi"),   // render differs → no match
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2 };
        var empty = new Dictionary<string, string>();

        var stats = CreateAssembler().Assemble(corpus, ids, empty, empty, empty).RenderStats;

        stats.WholeWordAgreementTotal.Should().Be(2);
        stats.WholeWordAgreementMatches.Should().Be(1);
    }

    [Fact]
    public void Render_stats_collect_review_multiword_and_empty_lists()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            new("1:1:1", "X1", [new AlignedCorpusSegment(1, "STEM", "N", ">an_#bi", "NOM", null, null)]),
            new("1:1:2", "X2", [new AlignedCorpusSegment(1, "STEM", "N", "<ilo yaAsiyna", "NOM", null, null)]),
            new("1:1:3", "X3",
            [
                new AlignedCorpusSegment(1, "STEM", "N", "kataba", "NOM", null, null),
                new AlignedCorpusSegment(2, "SUFFIX", "PRON", "", "PRON", null, null),
            ]),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2, ["1:1:3"] = 3 };
        var empty = new Dictionary<string, string>();

        var stats = CreateAssembler().Assemble(corpus, ids, empty, empty, empty).RenderStats;

        stats.ReviewTierForms.Should().Contain(">an_#bi");
        stats.MultiwordForms.Should().Contain("<ilo yaAsiyna");
        stats.EmptyFormLocations.Should().ContainSingle().Which.Should().Be("1:1:3:2");
    }

    [Theory]
    [InlineData("kataba", "clean")]
    [InlineData("ya`^", "clean")]      // dagger alef + maddah are still clean (T1b)
    [InlineData(">an[bi", "quranic_marks")] // contains '[' annotation mark
    [InlineData(">an_#bi", "review")]  // contains tatweel '_' / kashida-hamza '#'
    [InlineData("<ilo yaAsiyna", "multiword")] // contains a space
    public void Segment_render_tier_is_classified_correctly(string form, string expectedTier)
    {
        var corpus = new List<AlignedCorpusWord> { StemWord("1:1:1", "X1", form) };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, empty, empty);

        result.Words.Single().Segments.Single().RenderTier.Should().Be(expectedTier);
    }

    [Fact]
    public void Multiword_form_space_is_not_flagged_but_out_of_map_chars_still_are()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            // "<ilo yaAsiyna" (إل ياسين) is the single multiword form; the space is its word
            // separator (renderer classifies it as the 'multiword' tier), not an unmapped glyph.
            StemWord("1:1:1", "X1", "<ilo yaAsiyna"),
            // '€' is genuinely outside the Buckwalter map and must still be reported.
            StemWord("1:1:2", "X2", "ba€b"),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2 };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, empty, empty);

        result.CharsetWarnings.Should().NotContain(
            w => w.Contains("yaAsiyna"),
            "a space inside a multiword-tier form is the legitimate separator, not an unmapped character");
        result.CharsetWarnings.Should().Contain(
            w => w.Contains("€"),
            "characters genuinely outside the Buckwalter map must still be reported");
    }

    [Theory]
    // The real QAC corpus encodes STEM features as pipe-delimited, prefixed tokens
    // (e.g. "POS:V", "LEM:..", "ROOT:..") rather than the space-separated bare tokens the
    // synthetic fixtures use. Verb tense/voice and grammatical case must still resolve.
    [InlineData("V", "STEM|POS:V|PERF|LEM:kataba|ROOT:ktb", "past", "active", null)]
    [InlineData("V", "STEM|POS:V|IMPF|LEM:Eabada|ROOT:Ebd|1P", "present", "active", null)] // observed 1:5:2
    [InlineData("V", "STEM|POS:V|IMPV|LEM:hadaY|ROOT:hdy", "imperative", "active", null)]
    [InlineData("V", "STEM|POS:V|PERF|PASS|LEM:xuliqa|ROOT:xlq", "past", "passive", null)]
    [InlineData("N", "STEM|POS:N|LEM:{som|ROOT:smw|M|GEN", null, null, "genitive")]         // observed 1:1:1:2
    [InlineData("N", "STEM|POS:N|ROOT:rbb|NOM", null, null, "nominative")]
    [InlineData("N", "STEM|POS:N|ROOT:Hkm|ACC", null, null, "accusative")]
    public void Verb_and_case_features_parse_from_pipe_delimited_corpus_format(
        string pos, string features, string? expectedTense, string? expectedVoice, string? expectedCase)
    {
        var corpus = new List<AlignedCorpusWord>
        {
            new("1:1:1", "X1", [new AlignedCorpusSegment(1, "STEM", pos, "kataba", features, null, null)]),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        var empty = new Dictionary<string, string>();

        var word = CreateAssembler().Assemble(corpus, ids, empty, empty, empty).Words.Single();

        word.VerbTense.Should().Be(expectedTense);
        word.VerbVoice.Should().Be(expectedVoice);
        word.CaseFeature.Should().Be(expectedCase);
    }
}
