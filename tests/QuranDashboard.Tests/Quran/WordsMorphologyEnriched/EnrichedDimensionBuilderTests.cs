using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.Tests.Quran.WordsMorphologyEnriched;

public sealed class EnrichedDimensionBuilderTests
{
    private static EnrichedDimensionBuilder CreateBuilder() => new();

    [Fact]
    public void Root_identity_is_value_based_on_root_buckwalter_not_qul_location()
    {
        // Two words in DIFFERENT locations share the same Corpus rootBuckwalter; under value-based identity
        // they MUST collapse to one root dimension. The legacy QUL-link pathway keyed on whole-word root
        // text and could not guarantee this; the enriched pathway keys on rootBuckwalter directly.
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
        // Two STEM segments with the SAME formArabic (stem_text) but DIFFERENT stemBuckwalter must collapse
        // to ONE stem dimension row: quran_stems has no stem_buckwalter column, so stemBuckwalter cannot
        // mint a separately-distinguishable row. This is the signed-off no-schema stem rule.
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
        // stemBuckwalter, *MappingStatus, *QulCanonical, corpusPresent, quranWordIdVerifiedAgainstDashboard
        // are read into the source model but must NOT appear on the persisted DTO. The DTO has no members
        // for them, so this asserts the projection is clean by construction.
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
        // No mapping-status / Qul-canonical / verified-against-dashboard field exists on AlignedSegmentDto;
        // the type itself is the contract. Assert render-source is the only "audit" string carried.
        segment.RenderSource.Should().NotContain("MappingStatus");
        segment.RenderSource.Should().NotContain("QulCanonical");
    }

    [Fact]
    public void No_buckwalter_in_arabic_display_fields()
    {
        // Arabic display fields (FormArabicNormalized, root_text, lemma_text, stem_text) must never carry
        // Buckwalter transliteration. Buckwalter stays only in form_buckwalter/root_buckwalter/lemma_buckwalter
        // (internal/audit columns).
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
        // A two-STEM word (PREFIX + STEM-primary + STEM-secondary + SUFFIX) mirroring 8:6:12. Each STEM
        // segment resolves its own root/lemma/stem id from its own buckwalter; the word-level head ids
        // come from the primary (lowest-numbered) STEM.
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

        // The two STEM segments each carry their own dimension ids; the PREFIX segment stays null.
        var primaryStem = word.Segments.Single(segment => segment.SegmentNumber == 2);
        var secondaryStem = word.Segments.Single(segment => segment.SegmentNumber == 3);
        var prefix = word.Segments.Single(segment => segment.SegmentNumber == 1);

        primaryStem.RootId.Should().NotBeNull("primary STEM has rootBuckwalter rootV");
        primaryStem.LemmaId.Should().NotBeNull();
        primaryStem.StemId.Should().NotBeNull();
        secondaryStem.RootId.Should().BeNull("secondary STEM has no rootBuckwalter in this fixture");
        secondaryStem.StemId.Should().NotBeNull("secondary STEM still gets a stem_text dimension");
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
}
