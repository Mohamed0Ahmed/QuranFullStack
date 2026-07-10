using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

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
    public void Null_segment_buckwalter_values_keep_segment_dimension_ids_null()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", "X1", "kataba"),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };

        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, empty, empty);

        result.ResolvedRoots.Should().BeEmpty();
        result.ResolvedLemmas.Should().BeEmpty();
        var word = result.Words.Single();
        word.RootId.Should().BeNull();
        word.LemmaId.Should().BeNull();
        word.Segments.Single().RootId.Should().BeNull();
        word.Segments.Single().LemmaId.Should().BeNull();
        result.SegmentDimensionIssues.Should().BeEmpty();
    }

    [Fact]
    public void Single_stem_segment_uses_head_lemma_id_when_segment_lemma_buckwalter_is_empty()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            new("1:1:1", "X1",
            [
                new AlignedCorpusSegment(1, "STEM", "N", "kataba", "NOM", null, null)
            ]),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        var lemmas = new Dictionary<string, string> { ["1:1:1"] = "SYNTHETIC_KATAB" };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, lemmas, empty);

        var word = result.Words.Single();
        word.LemmaId.Should().NotBeNull();
        word.Segments.Single().LemmaId.Should().Be(word.LemmaId);
        result.SegmentDimensionIssues.Should().BeEmpty();
    }

    [Fact]
    public void Single_stem_segment_uses_head_lemma_id_even_when_buckwalter_is_globally_ambiguous()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", "X1", "kataba", lemma: "dup"),
            StemWord("1:1:2", "X2", "kitAbi", lemma: "dup"),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2 };
        var lemmas = new Dictionary<string, string>
        {
            ["1:1:1"] = "SYNTHETIC_LEMMA_A",
            ["1:1:2"] = "SYNTHETIC_LEMMA_B"
        };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, lemmas, empty);

        result.ResolvedLemmas.Where(lemma => lemma.LemmaBuckwalter == "dup").Should().HaveCount(2);
        result.Words.Should().OnlyContain(word =>
            word.Segments.Single().LemmaId == word.LemmaId,
            "single-STEM words intentionally trust the already-resolved head lemma");
        result.SegmentDimensionIssues.Should().BeEmpty();
    }

    [Fact]
    public void Multi_stem_segments_resolve_head_and_non_head_lemmas_independently()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            new("1:1:1", "X1",
            [
                new AlignedCorpusSegment(1, "STEM", "SUB", ">ano", "STEM|POS:SUB", null, ">an"),
                new AlignedCorpusSegment(2, "STEM", "NEG", "laA", "STEM|POS:NEG", null, "laA")
            ]),
            StemWord("1:1:2", "X2", "laA", lemma: "laA"),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2 };
        var lemmas = new Dictionary<string, string>
        {
            ["1:1:1"] = "SYNTHETIC_HEAD",
            ["1:1:2"] = "SYNTHETIC_LAA"
        };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, lemmas, empty);

        var multiStemWord = result.Words.Single(word => word.Location == "1:1:1");
        var laaLemma = result.ResolvedLemmas.Single(lemma => lemma.LemmaText == "SYNTHETIC_LAA");
        multiStemWord.Segments[0].LemmaId.Should().Be(multiStemWord.LemmaId);
        multiStemWord.Segments[1].LemmaId.Should().Be(laaLemma.AssignedId);
        result.SegmentDimensionIssues.Should().BeEmpty();
    }

    [Fact]
    public void Non_stem_segment_never_receives_lemma_id_even_with_unexpected_lemma_source()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            new("1:1:1", "X1",
            [
                new AlignedCorpusSegment(1, "PREFIX", "P", "bi", "PREFIX", null, "unexpected"),
                new AlignedCorpusSegment(2, "STEM", "N", "kataba", "NOM", null, "katab")
            ]),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        var lemmas = new Dictionary<string, string> { ["1:1:1"] = "SYNTHETIC_KATAB" };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, lemmas, empty);

        var prefix = result.Words.Single().Segments.Single(segment => segment.Kind == "PREFIX");
        prefix.LemmaId.Should().BeNull();
        result.SegmentDimensionIssues.Should().BeEmpty();
    }

    [Fact]
    public void Segment_root_id_resolves_by_root_buckwalter()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", "X1", "kataba", root: "ktb"),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        var roots = new Dictionary<string, string> { ["1:1:1"] = "SYNTHETIC_ROOT" };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, roots, empty, empty);

        var root = result.ResolvedRoots.Single();
        result.Words.Single().Segments.Single().RootId.Should().Be(root.AssignedId);
        result.SegmentDimensionIssues.Should().BeEmpty();
    }

    [Fact]
    public void Unresolved_multi_stem_lemma_is_reported_without_guessing()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            new("1:1:1", "X1",
            [
                new AlignedCorpusSegment(1, "STEM", "SUB", ">ano", "STEM|POS:SUB", null, ">an"),
                new AlignedCorpusSegment(2, "STEM", "NEG", "laA", "STEM|POS:NEG", null, "missing")
            ]),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        var lemmas = new Dictionary<string, string> { ["1:1:1"] = "SYNTHETIC_HEAD" };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, lemmas, empty);

        result.Words.Single().Segments[1].LemmaId.Should().BeNull();
        result.SegmentDimensionIssues.Should().ContainSingle(issue =>
            issue.CheckId == MorphologyInvariants.CheckSegLemmaMultiStemResolves
            && issue.SegmentLocation == "1:1:1:2");
    }

    [Fact]
    public void Unsafe_duplicate_multi_stem_lemma_is_reported_without_lowest_id_tiebreak()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", "X1", "kataba", lemma: "dup"),
            StemWord("1:1:2", "X2", "kitAbi", lemma: "dup"),
            new("1:1:3", "X3",
            [
                new AlignedCorpusSegment(1, "STEM", "N", "somi", "NOM", null, "head"),
                new AlignedCorpusSegment(2, "STEM", "N", "raboni", "NOM", null, "dup")
            ])
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2, ["1:1:3"] = 3 };
        var lemmas = new Dictionary<string, string>
        {
            ["1:1:1"] = "SYNTHETIC_DUP_A",
            ["1:1:2"] = "SYNTHETIC_DUP_B",
            ["1:1:3"] = "SYNTHETIC_HEAD"
        };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, lemmas, empty);

        result.Words.Single(word => word.Location == "1:1:3").Segments[1].LemmaId.Should().BeNull();
        result.SegmentDimensionIssues.Should().ContainSingle(issue =>
            issue.CheckId == MorphologyInvariants.CheckSegLemmaNoFanout
            && issue.SegmentLocation == "1:1:3:2");
    }

    [Fact]
    public void Safe_duplicate_multi_stem_lemma_tiebreak_uses_matching_arabic_form()
    {
        var matchingLemmaText = new BuckwalterArabicMap().Transliterate("kataba").Arabic;
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", "X1", "kataba", lemma: "dup"),
            StemWord("1:1:2", "X2", "kitAbi", lemma: "dup"),
            new("1:1:3", "X3",
            [
                new AlignedCorpusSegment(1, "STEM", "N", "somi", "NOM", null, "head"),
                new AlignedCorpusSegment(2, "STEM", "N", "kataba", "NOM", null, "dup")
            ])
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2, ["1:1:3"] = 3 };
        var lemmas = new Dictionary<string, string>
        {
            ["1:1:1"] = matchingLemmaText,
            ["1:1:2"] = "SYNTHETIC_DUP_B",
            ["1:1:3"] = "SYNTHETIC_HEAD"
        };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, lemmas, empty);

        var matchingLemma = result.ResolvedLemmas.Single(lemma => lemma.LemmaText == matchingLemmaText);
        result.Words.Single(word => word.Location == "1:1:3").Segments[1].LemmaId.Should().Be(matchingLemma.AssignedId);
        result.SegmentDimensionIssues.Should().BeEmpty();
    }

    [Fact]
    public void Ambiguous_acc_anna_segment_resolves_via_curated_disambiguation_map()
    {
        const string annaLemmaText = "أَنّ";
        const string innaLemmaText = "إِنّ";
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", "X1", ">an~a", lemma: ">an~"),
            StemWord("1:1:2", "X2", ">an~a", lemma: ">an~"),
            new("1:1:3", "X3",
            [
                new AlignedCorpusSegment(1, "STEM", "N", "somi", "NOM", null, "head"),
                new AlignedCorpusSegment(2, "STEM", "ACC", ">an~a", "STEM|POS:ACC", null, ">an~")
            ])
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2, ["1:1:3"] = 3 };
        var lemmas = new Dictionary<string, string>
        {
            ["1:1:1"] = annaLemmaText,
            ["1:1:2"] = innaLemmaText,
            ["1:1:3"] = "SYNTHETIC_HEAD"
        };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, lemmas, empty);

        result.ResolvedLemmas.Where(lemma => lemma.LemmaBuckwalter == ">an~").Should().HaveCount(2);
        var annaLemma = result.ResolvedLemmas.Single(lemma => lemma.LemmaText == annaLemmaText);
        result.Words.Single(word => word.Location == "1:1:3").Segments[1].LemmaId
            .Should().Be(annaLemma.AssignedId);
        result.SegmentDimensionIssues.Should().BeEmpty();
    }

    [Fact]
    public void Unresolved_root_buckwalter_is_reported_without_inventing_id()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", "X1", "kataba", root: "missing"),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1 };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, empty, empty, empty);

        result.Words.Single().Segments.Single().RootId.Should().BeNull();
        result.SegmentDimensionIssues.Should().ContainSingle(issue =>
            issue.CheckId == MorphologyInvariants.CheckSegRootResolves
            && issue.SegmentLocation == "1:1:1:1");
    }

    [Fact]
    public void Ambiguous_root_buckwalter_is_reported_without_guessing()
    {
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", "X1", "kataba", root: "dup"),
            StemWord("1:1:2", "X2", "kitAbi", root: "dup"),
        };
        var ids = new Dictionary<string, int> { ["1:1:1"] = 1, ["1:1:2"] = 2 };
        var roots = new Dictionary<string, string>
        {
            ["1:1:1"] = "SYNTHETIC_ROOT_A",
            ["1:1:2"] = "SYNTHETIC_ROOT_B"
        };
        var empty = new Dictionary<string, string>();

        var result = CreateAssembler().Assemble(corpus, ids, roots, empty, empty);

        result.Words.SelectMany(word => word.Segments).Should().OnlyContain(segment => segment.RootId == null);
        result.SegmentDimensionIssues.Should().HaveCount(2);
        result.SegmentDimensionIssues.Should().OnlyContain(issue =>
            issue.CheckId == MorphologyInvariants.CheckSegRootResolves);
    }

    [Fact]
    public void Whole_word_agreement_counts_words_whose_render_matches_qpc_uthmani()
    {
        var matchUthmani = new BuckwalterArabicMap().Transliterate("kataba").Arabic;
        var corpus = new List<AlignedCorpusWord>
        {
            StemWord("1:1:1", matchUthmani, "kataba"),
            StemWord("1:1:2", "MISMATCH", "kitAbi"),
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
    [InlineData("ya`^", "clean")]
    [InlineData(">an[bi", "quranic_marks")]
    [InlineData(">an_#bi", "review")]
    [InlineData("<ilo yaAsiyna", "multiword")]
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

            StemWord("1:1:1", "X1", "<ilo yaAsiyna"),

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

    [InlineData("V", "STEM|POS:V|PERF|LEM:kataba|ROOT:ktb", "past", "active", null)]
    [InlineData("V", "STEM|POS:V|IMPF|LEM:Eabada|ROOT:Ebd|1P", "present", "active", null)]
    [InlineData("V", "STEM|POS:V|IMPV|LEM:hadaY|ROOT:hdy", "imperative", "active", null)]
    [InlineData("V", "STEM|POS:V|PERF|PASS|LEM:xuliqa|ROOT:xlq", "past", "passive", null)]
    [InlineData("N", "STEM|POS:N|LEM:{som|ROOT:smw|M|GEN", null, null, "genitive")]
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
