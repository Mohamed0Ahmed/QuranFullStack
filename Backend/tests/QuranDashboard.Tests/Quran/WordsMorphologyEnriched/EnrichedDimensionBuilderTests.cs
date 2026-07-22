using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.Tests.Quran.WordsMorphologyEnriched;

public sealed class EnrichedDimensionBuilderTests
{
    private static EnrichedDimensionBuilder CreateBuilder() => new();

    [Fact]
    public void Root_identity_is_value_based_on_root_buckwalter_not_qul_location()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                "2:1:1", quranWordId: 10,
                formBuckwalter: "synA", formArabic: "صِيغَةٌ تَجْرِيبِيَّة أ",
                rootBuckwalter: "rootA", rootArabic: "جذر تجريبي أ",
                lemmaBuckwalter: "lemmaA", lemmaArabic: "لِمَةٌ تَجْرِيبِيَّة أ"),
            EnrichedMorphologyTestData.StemRecord(
                "2:1:2", quranWordId: 11,
                formBuckwalter: "synB", formArabic: "صِيغَةٌ تَجْرِيبِيَّة ب",
                rootBuckwalter: "rootA", rootArabic: "جذر تجريبي أ",
                lemmaBuckwalter: "lemmaB", lemmaArabic: "لِمَةٌ تَجْرِيبِيَّة ب"),
        };

        var result = CreateBuilder().Build(records);

        result.ResolvedRoots.Should().ContainSingle("both words share rootBuckwalter 'rootA'");
        var root = result.ResolvedRoots.Single();
        root.RootBuckwalter.Should().Be("rootA");
        root.RootText.Should().Be("جذر تجريبي أ", "rootArabic is stored as root_text");
        root.WordsCount.Should().Be(2);
        root.DistinctLemmasCount.Should().Be(2, "two distinct lemmaBuckwalter keys fan out under the root");
    }

    [Fact]
    public void Lemma_identity_is_value_based_on_lemma_buckwalter_and_links_co_occurring_root()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                "1:1:1", quranWordId: 1,
                formBuckwalter: "synA", formArabic: "صِيغَةٌ تَجْرِيبِيَّة أ",
                rootBuckwalter: "rootA", rootArabic: "جذر تجريبي أ",
                lemmaBuckwalter: "lemmaA", lemmaArabic: "لِمَةٌ تَجْرِيبِيَّة أ"),
        };

        var result = CreateBuilder().Build(records);

        var lemma = result.ResolvedLemmas.Single();
        lemma.LemmaBuckwalter.Should().Be("lemmaA");
        lemma.LemmaText.Should().Be("لِمَةٌ تَجْرِيبِيَّة أ", "primary lemmaArabic is stored as lemma_text");
        lemma.RootId.Should().Be(result.ResolvedRoots.Single().AssignedId,
            "lemma→root link comes from the co-occurring segment root, never a QUL location join");
    }

    [Fact]
    public void Stem_identity_uses_stem_text_not_stem_buckwalter()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                "1:1:1", quranWordId: 1,
                formBuckwalter: "buckA", formArabic: "جِذْعٌ مُشْتَرَك",
                rootBuckwalter: "rootA", rootArabic: "جذر تجريبي أ",
                lemmaBuckwalter: "lemmaA", lemmaArabic: "لِمَةٌ تَجْرِيبِيَّة أ"),
            EnrichedMorphologyTestData.StemRecord(
                "1:1:2", quranWordId: 2,
                formBuckwalter: "buckB", formArabic: "جِذْعٌ مُشْتَرَك",
                rootBuckwalter: "rootA", rootArabic: "جذر تجريبي أ",
                lemmaBuckwalter: "lemmaA", lemmaArabic: "لِمَةٌ تَجْرِيبِيَّة أ"),
        };

        var result = CreateBuilder().Build(records);

        result.ResolvedStems.Should().ContainSingle(
            "distinct stemBuckwalter cannot create separate rows when stem_text is identical");
        result.ResolvedStems.Single().StemText.Should().Be("جِذْعٌ مُشْتَرَك");
        result.ResolvedStems.Single().WordsCount.Should().Be(2);
    }

    [Fact]
    public void Stem_identity_ignores_quranic_small_yeh_but_preserves_segment_render()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                "2:22:13", quranWordId: 311,
                formBuckwalter: "hi", formArabic: "هِ",
                pos: "PRON",
                featuresRaw: "STEM|POS:PRON|3MS",
                rootBuckwalter: null, rootArabic: null,
                lemmaBuckwalter: null, lemmaArabic: null),
            EnrichedMorphologyTestData.StemRecord(
                "2:22:14", quranWordId: 312,
                formBuckwalter: "hi.", formArabic: "هِۦ",
                pos: "PRON",
                featuresRaw: "STEM|POS:PRON|3MS",
                rootBuckwalter: null, rootArabic: null,
                lemmaBuckwalter: null, lemmaArabic: null),
        };

        var result = CreateBuilder().Build(records);

        result.ResolvedStems.Should().ContainSingle();
        var stem = result.ResolvedStems.Single();
        stem.StemText.Should().Be("هِ");
        stem.WordsCount.Should().Be(2);

        var strippedSegment = result.Words.Single(word => word.Word.Location == "2:22:13").Word.Segments.Single();
        var smallYehSegment = result.Words.Single(word => word.Word.Location == "2:22:14").Word.Segments.Single();
        strippedSegment.FormArabicNormalized.Should().Be("هِ");
        smallYehSegment.FormArabicNormalized.Should().Be("هِۦ");
        smallYehSegment.StemId.Should().Be(strippedSegment.StemId,
            "stem_id uses normalized stem identity while segment render keeps U+06E6");
    }

    [Fact]
    public void Known_small_yeh_head_stems_collapse_to_existing_stripped_stem_identities()
    {
        var pairs = new (string Stripped, string WithSmallYeh, string Buckwalter)[]
        {
            ("هِ", "هِۦ", "hi."),
            ("يُحْىِ", "يُحْىِۦ", "yuHoYi."),
            ("أُحْىِ", "أُحْىِۦ", ">uHoYi."),
            ("هَٰذِهِ", "هَٰذِهِۦ", "ha`*ihi."),
            ("نُحْىِ", "نُحْىِۦ", "nuHoYi."),
        };
        var records = pairs.SelectMany((pair, index) => new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                $"1:1:{(index * 2) + 1}", quranWordId: (index * 2) + 1,
                formBuckwalter: pair.Buckwalter.TrimEnd('.'), formArabic: pair.Stripped,
                rootBuckwalter: null, rootArabic: null,
                lemmaBuckwalter: null, lemmaArabic: null),
            EnrichedMorphologyTestData.StemRecord(
                $"1:1:{(index * 2) + 2}", quranWordId: (index * 2) + 2,
                formBuckwalter: pair.Buckwalter, formArabic: pair.WithSmallYeh,
                rootBuckwalter: null, rootArabic: null,
                lemmaBuckwalter: null, lemmaArabic: null),
        }).ToArray();

        var result = CreateBuilder().Build(records);

        result.ResolvedStems.Should().HaveCount(5);
        result.ResolvedStems.Should().OnlyContain(stem => !stem.StemText.Contains('ۦ'));
        result.ResolvedStems.Select(stem => stem.StemText).Should().BeEquivalentTo(pairs.Select(pair => pair.Stripped));
        result.ResolvedStems.Should().OnlyContain(stem => stem.WordsCount == 2);
    }

    [Fact]
    public void Small_yeh_suffix_pronoun_remains_rendered_and_does_not_mint_a_stem()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.MultiSegmentRecord(
                "12:101:14",
                quranWordId: 33346,
                new[]
                {
                    EnrichedMorphologyTestData.Segment(
                        1, "STEM", "N", "waliY~i", "وَلِىِّ", "STEM|POS:N|LEM:waliY~|ROOT:wly|M|NOM",
                        rootBuckwalter: "wly", rootArabic: "و ل ي",
                        lemmaBuckwalter: "waliY~", lemmaArabic: "وَلِىّ"),
                    EnrichedMorphologyTestData.Segment(
                        2, "SUFFIX", "PRON", ".", "ۦ", "SUFFIX|PRON:1S"),
                }),
        };

        var result = CreateBuilder().Build(records);

        result.ResolvedStems.Should().ContainSingle();
        result.ResolvedStems.Single().StemText.Should().Be("وَلِىِّ");
        var word = result.Words.Single().Word;
        var suffix = word.Segments.Single(segment => segment.SegmentNumber == 2);
        suffix.FormBuckwalter.Should().Be(".");
        suffix.FormArabicNormalized.Should().Be("ۦ");
        suffix.Kind.Should().Be("SUFFIX");
        suffix.Pos.Should().Be("PRON");
        suffix.StemId.Should().BeNull("only STEM segments can resolve quran_stems ids");
    }

    [Fact]
    public void Verb_features_map_from_head_stem_features()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                "1:1:1", quranWordId: 1,
                formBuckwalter: "verbSyn", formArabic: "فِعْلٌ تَجْرِيبِيّ",
                pos: "V",
                featuresRaw: "STEM|POS:V|PERF|ACT",
                rootBuckwalter: "rootV", rootArabic: "جذر فعلي تجريبي",
                lemmaBuckwalter: "lemmaV", lemmaArabic: "لِمَةٌ فِعْلِيَّة تَجْرِيبِيَّة"),
        };

        var result = CreateBuilder().Build(records);
        var word = result.Words.Single().Word;

        word.IsVerb.Should().BeTrue();
        word.VerbTense.Should().Be("past", "PERF marker maps to past");
        word.VerbVoice.Should().Be("active", "no PASS marker means active");
        word.CaseFeature.Should().BeNull("verbs do not carry case");
    }

    [Fact]
    public void Noun_case_feature_maps_from_head_stem_features()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                "1:1:1", quranWordId: 1,
                formBuckwalter: "nounSyn", formArabic: "اِسْمٌ تَجْرِيبِيّ",
                pos: "N",
                featuresRaw: "STEM|POS:N|M|NOM",
                rootBuckwalter: "rootN", rootArabic: "جذر اسمي تجريبي",
                lemmaBuckwalter: "lemmaN", lemmaArabic: "لِمَةٌ اِسْمِيَّة تَجْرِيبِيَّة"),
        };

        var result = CreateBuilder().Build(records);
        var word = result.Words.Single().Word;

        word.IsVerb.Should().BeFalse();
        word.VerbTense.Should().BeNull();
        word.VerbVoice.Should().BeNull();
        word.CaseFeature.Should().Be("nominative", "NOM marker maps to nominative");
    }

    [Fact]
    public void Audit_only_fields_are_dropped_from_persisted_dto()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                "1:1:1", quranWordId: 1,
                formBuckwalter: "synA", formArabic: "صِيغَةٌ تَجْرِيبِيَّة أ",
                rootBuckwalter: "rootA", rootArabic: "جذر تجريبي أ",
                lemmaBuckwalter: "lemmaA", lemmaArabic: "لِمَةٌ تَجْرِيبِيَّة أ"),
        };

        var result = CreateBuilder().Build(records);

        var segment = result.Words.Single().Word.Segments.Single();
        segment.RenderTier.Should().Be(EnrichedDimensionBuilder.EnrichedRenderTier);
        segment.RenderSource.Should().Be(EnrichedDimensionBuilder.EnrichedRenderSource);
        segment.RenderSource.Should().NotContain("MappingStatus");
        segment.RenderSource.Should().NotContain("QulCanonical");
    }

    [Fact]
    public void No_buckwalter_in_arabic_display_fields()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                "1:1:1", quranWordId: 1,
                formBuckwalter: "synA", formArabic: "صِيغَةٌ تَجْرِيبِيَّة أ",
                rootBuckwalter: "rootA", rootArabic: "جذر تجريبي أ",
                lemmaBuckwalter: "lemmaA", lemmaArabic: "لِمَةٌ تَجْرِيبِيَّة أ"),
        };

        var result = CreateBuilder().Build(records);

        var segment = result.Words.Single().Word.Segments.Single();
        segment.FormArabicNormalized.Should().Be("صِيغَةٌ تَجْرِيبِيَّة أ");
        result.ResolvedRoots.Single().RootText.Should().Be("جذر تجريبي أ");
        result.ResolvedLemmas.Single().LemmaText.Should().Be("لِمَةٌ تَجْرِيبِيَّة أ");
        result.ResolvedStems.Single().StemText.Should().Be("صِيغَةٌ تَجْرِيبِيَّة أ");
    }

    [Fact]
    public void Unknown_pos_code_is_collected_not_swallowed()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                "1:1:1", quranWordId: 1,
                formBuckwalter: "x", formArabic: "صِيغَةٌ تَجْرِيبِيَّة",
                pos: "BOGUS",
                featuresRaw: "STEM|POS:BOGUS",
                rootBuckwalter: "rootA", rootArabic: "جذر تجريبي أ",
                lemmaBuckwalter: "lemmaA", lemmaArabic: "لِمَةٌ تَجْرِيبِيَّة أ"),
        };

        var result = CreateBuilder().Build(records);

        result.UnknownPosCodes.Should().Contain("BOGUS");
    }

    [Fact]
    public void Segment_dimension_ids_resolve_value_based_per_segment()
    {
        var segments = new[]
        {
            EnrichedMorphologyTestData.Segment(1, "PREFIX", "CONJ", "prefixSyn", "بادئة تجريبية", "PREFIX|TEST"),
            EnrichedMorphologyTestData.Segment(
                2, "STEM", "V", "verbSyn", "فِعْلٌ تَجْرِيبِيّ",
                "STEM|POS:V|PERF|ACT",
                rootBuckwalter: "rootV", rootArabic: "جذر فعلي تجريبي",
                lemmaBuckwalter: "lemmaV", lemmaArabic: "لِمَةٌ فِعْلِيَّة تَجْرِيبِيَّة"),
            EnrichedMorphologyTestData.Segment(
                3, "STEM", "PRON", "pronSyn", "ضَمِيرٌ تَجْرِيبِيّ",
                "STEM|POS:PRON",
                rootBuckwalter: null, rootArabic: null,
                lemmaBuckwalter: null, lemmaArabic: null),
        };
        var records = new[]
        {
            EnrichedMorphologyTestData.MultiSegmentRecord("8:6:12", quranWordId: 1234, segments),
        };

        var result = CreateBuilder().Build(records);

        var word = result.Words.Single().Word;
        word.Segments.Should().HaveCount(3);
        word.HeadPos.Should().Be("V", "primary STEM is the head");
        word.IsVerb.Should().BeTrue();

        var primaryStem = word.Segments.Single(segment => segment.SegmentNumber == 2);
        var secondaryStem = word.Segments.Single(segment => segment.SegmentNumber == 3);
        var prefix = word.Segments.Single(segment => segment.SegmentNumber == 1);

        primaryStem.RootId.Should().NotBeNull("primary STEM has rootBuckwalter rootV");
        primaryStem.LemmaId.Should().NotBeNull();
        primaryStem.StemId.Should().NotBeNull();
        secondaryStem.RootId.Should().BeNull("secondary STEM has no rootBuckwalter in this fixture");
        secondaryStem.LemmaId.Should().BeNull("secondary STEM has no lemmaBuckwalter in this fixture");
        secondaryStem.StemId.Should().BeNull(
            "secondary STEMs resolve lookup-only and never mint a fresh stem dimension (UNIQUE first_word_order)");
        prefix.RootId.Should().BeNull("PREFIX segments do not carry root dimensions");
        prefix.LemmaId.Should().BeNull();
        prefix.StemId.Should().BeNull();
    }

    [Fact]
    public void First_word_order_is_the_minimum_quran_word_id_not_iteration_order()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                "1:1:10", quranWordId: 9,
                formBuckwalter: "a", formArabic: "صِيغَةٌ تَجْرِيبِيَّة أ",
                rootBuckwalter: "rootA", rootArabic: "جذر تجريبي أ",
                lemmaBuckwalter: "lemmaA", lemmaArabic: "لِمَةٌ تَجْرِيبِيَّة أ"),
            EnrichedMorphologyTestData.StemRecord(
                "1:1:2", quranWordId: 1,
                formBuckwalter: "b", formArabic: "صِيغَةٌ تَجْرِيبِيَّة ب",
                rootBuckwalter: "rootA", rootArabic: "جذر تجريبي أ",
                lemmaBuckwalter: "lemmaA", lemmaArabic: "لِمَةٌ تَجْرِيبِيَّة أ"),
        };

        var result = CreateBuilder().Build(records);

        result.ResolvedRoots.Single().FirstWordOrderInMushaf.Should().Be(1,
            "first_word_order is the minimum quran_word_id observed, not iteration order");
    }

    [Fact]
    public void Multi_stem_word_never_mints_two_dimensions_sharing_a_first_word_order()
    {
        var segments = new[]
        {
            EnrichedMorphologyTestData.Segment(
                1, "STEM", "N", "minSyn", "مِنْ", "STEM|POS:N|GEN",
                rootBuckwalter: "min", rootArabic: "من",
                lemmaBuckwalter: "min", lemmaArabic: "مِنْ"),
            EnrichedMorphologyTestData.Segment(
                2, "STEM", "N", "maASyn", "مَا", "STEM|POS:N|GEN",
                rootBuckwalter: "maA", rootArabic: "ما",
                lemmaBuckwalter: "maA", lemmaArabic: "مَا"),
        };
        var records = new[]
        {
            EnrichedMorphologyTestData.MultiSegmentRecord("2:3:6", quranWordId: 52, segments),
        };

        var result = CreateBuilder().Build(records);

        result.ResolvedLemmas.Select(lemma => lemma.FirstWordOrderInMushaf).Should().OnlyHaveUniqueItems(
            "two STEM lemmas in one word must never share a FirstWordOrder (UNIQUE index)");
        result.ResolvedRoots.Select(root => root.FirstWordOrderInMushaf).Should().OnlyHaveUniqueItems(
            "two STEM roots in one word must never share a FirstWordOrder (UNIQUE index)");
        result.ResolvedStems.Select(stem => stem.FirstWordOrderInMushaf).Should().OnlyHaveUniqueItems(
            "two STEM stems in one word must never share a FirstWordOrder (UNIQUE index)");

        result.ResolvedLemmas.Should().ContainSingle("only the head STEM (segment 1, 'min') mints a lemma");
        result.ResolvedRoots.Should().ContainSingle("only the head STEM mints a root");
        result.ResolvedStems.Should().ContainSingle("only the head STEM mints a stem");

        var word = result.Words.Single().Word;
        var headStem = word.Segments.Single(segment => segment.SegmentNumber == 1);
        var secondaryStem = word.Segments.Single(segment => segment.SegmentNumber == 2);
        headStem.RootId.Should().NotBeNull();
        headStem.LemmaId.Should().NotBeNull();
        headStem.StemId.Should().NotBeNull();
        secondaryStem.RootId.Should().BeNull("secondary STEM dimensions resolve lookup-only, never minted");
        secondaryStem.LemmaId.Should().BeNull("secondary STEM dimensions resolve lookup-only, never minted");
        secondaryStem.StemId.Should().BeNull("secondary STEM dimensions resolve lookup-only, never minted");
        word.LemmaId.Should().Be(headStem.LemmaId, "word-level lemma comes from the head STEM");
        word.RootId.Should().Be(headStem.RootId, "word-level root comes from the head STEM");
        word.StemId.Should().Be(headStem.StemId, "word-level stem comes from the head STEM");
    }

    [Fact]
    public void Secondary_stem_dimension_reuses_an_already_minted_dimension()
    {
        var head = EnrichedMorphologyTestData.StemRecord(
            "1:1:1", quranWordId: 1,
            formBuckwalter: "maASyn", formArabic: "مَا",
            rootBuckwalter: "maA", rootArabic: "ما",
            lemmaBuckwalter: "maA", lemmaArabic: "مَا");
        var compound = EnrichedMorphologyTestData.MultiSegmentRecord(
            "2:3:6", quranWordId: 52,
            new[]
            {
                EnrichedMorphologyTestData.Segment(
                    1, "STEM", "N", "minSyn", "مِنْ", "STEM|POS:N|GEN",
                    rootBuckwalter: "min", rootArabic: "من",
                    lemmaBuckwalter: "min", lemmaArabic: "مِنْ"),
                EnrichedMorphologyTestData.Segment(
                    2, "STEM", "N", "maASyn", "مَا", "STEM|POS:N|GEN",
                    rootBuckwalter: "maA", rootArabic: "ما",
                    lemmaBuckwalter: "maA", lemmaArabic: "مَا"),
            });

        var result = CreateBuilder().Build(new[] { head, compound });

        var maALemma = result.ResolvedLemmas.Single(lemma => lemma.LemmaBuckwalter == "maA");
        var maARoot = result.ResolvedRoots.Single(root => root.RootBuckwalter == "maA");
        var maAStem = result.ResolvedStems.Single(stem => stem.StemText == "مَا");
        var compoundWord = result.Words.Single(word => word.QuranWordId == 52).Word;
        var secondaryStem = compoundWord.Segments.Single(segment => segment.SegmentNumber == 2);

        secondaryStem.LemmaId.Should().Be(maALemma.AssignedId,
            "the secondary STEM reuses the lemma already minted by the first word's head");
        secondaryStem.RootId.Should().Be(maARoot.AssignedId, "the secondary STEM reuses the already-minted root");
        secondaryStem.StemId.Should().Be(maAStem.AssignedId, "the secondary STEM reuses the already-minted stem");
        result.ResolvedLemmas.Select(lemma => lemma.FirstWordOrderInMushaf).Should().OnlyHaveUniqueItems();
        result.ResolvedRoots.Select(root => root.FirstWordOrderInMushaf).Should().OnlyHaveUniqueItems();
        result.ResolvedStems.Select(stem => stem.FirstWordOrderInMushaf).Should().OnlyHaveUniqueItems();
    }

    private static EnrichedMorphologyRecord[] AsaCollisionRecords() =>
        [
            EnrichedMorphologyTestData.StemRecord(
                "2:60:7", quranWordId: 949,
                formBuckwalter: "EaSaAsyn", formArabic: "عَصَا", pos: "N",
                rootBuckwalter: "ESw", rootArabic: "ع ص و",
                lemmaBuckwalter: "EaSaA2", lemmaArabic: "عَصَا"),
            EnrichedMorphologyTestData.StemRecord(
                "2:61:57", quranWordId: 1028,
                formBuckwalter: "EaSaAsyn2", formArabic: "عَصَاۦ", pos: "V",
                featuresRaw: "STEM|POS:V|PERF|ACT",
                rootBuckwalter: "ESy", rootArabic: "ع ص ي",
                lemmaBuckwalter: "EaSaA", lemmaArabic: "عَصَا"),
        ];

    [Fact]
    public void Colliding_lemma_text_variants_collapse_to_one_display_lemma_but_keep_distinct_analyses()
    {
        var result = CreateBuilder().Build(AsaCollisionRecords());

        result.ResolvedLemmas.Should().ContainSingle("both buckwalters share lemma_text عَصَا → one display lemma");
        result.ResolvedLemmas.Single().LemmaText.Should().Be("عَصَا");
        result.ResolvedLemmas.Single().WordsCount.Should().Be(2, "the collapsed lemma counts both occurrences");
        result.ResolvedLemmas.Select(lemma => lemma.LemmaText).Should().OnlyHaveUniqueItems();
        result.ResolvedLemmas.Select(lemma => lemma.FirstWordOrderInMushaf).Should().OnlyHaveUniqueItems(
            "one head lemma per word keeps first_word_order unique after collapse");

        result.ResolvedLemmaAnalyses.Should().HaveCount(2, "each distinct buckwalter keeps its own analysis");
        result.ResolvedLemmaAnalyses.Select(analysis => analysis.LemmaBuckwalter)
            .Should().BeEquivalentTo(["EaSaA2", "EaSaA"]);
        result.ResolvedLemmaAnalyses.Should().OnlyContain(
            analysis => analysis.LemmaId == result.ResolvedLemmas.Single().AssignedId,
            "every analysis links to the single collapsed display lemma");
        result.ResolvedLemmaAnalyses.Should().OnlyContain(analysis => analysis.WordsCount == 1);
    }

    [Fact]
    public void Different_root_collision_variants_are_not_analytically_merged()
    {
        var result = CreateBuilder().Build(AsaCollisionRecords());

        result.ResolvedRoots.Should().HaveCount(2, "two distinct rootBuckwalter → two roots (ع ص و, ع ص ي)");
        var analyses = result.ResolvedLemmaAnalyses;
        analyses.Should().HaveCount(2, "both عَصَا buckwalter variants keep an analysis row");
        analyses.Should().OnlyContain(analysis => analysis.RootId != null);
        analyses.Select(analysis => analysis.RootId).Should().OnlyHaveUniqueItems(
            "different-root variants keep distinct root_id in their analyses");
    }

    [Fact]
    public void Different_pos_collision_variants_preserve_head_pos_per_variant()
    {
        var records = new[]
        {
            EnrichedMorphologyTestData.StemRecord(
                "1:4:1", quranWordId: 14,
                formBuckwalter: "maAlikSyn", formArabic: "مَٰلِك", pos: "N",
                rootBuckwalter: "mlk", rootArabic: "م ل ك",
                lemmaBuckwalter: "ma`lik", lemmaArabic: "مَٰلِك"),
            EnrichedMorphologyTestData.StemRecord(
                "43:77:2", quranWordId: 68161,
                formBuckwalter: "maAlikSyn2", formArabic: "مَٰلِكُ", pos: "PN",
                rootBuckwalter: "mlk", rootArabic: "م ل ك",
                lemmaBuckwalter: "ma`lik2", lemmaArabic: "مَٰلِك"),
        };

        var result = CreateBuilder().Build(records);

        result.ResolvedLemmas.Should().ContainSingle();
        var byBuckwalter = result.ResolvedLemmaAnalyses.ToDictionary(analysis => analysis.LemmaBuckwalter);
        byBuckwalter["ma`lik"].HeadPos.Should().Be("N", "the common-noun variant keeps head POS N");
        byBuckwalter["ma`lik2"].HeadPos.Should().Be("PN", "the proper-noun variant keeps head POS PN");
        byBuckwalter["ma`lik"].FirstLocation.Should().Be("1:4:1");
        byBuckwalter["ma`lik2"].FirstLocation.Should().Be("43:77:2");
    }
}
