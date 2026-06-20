using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.SimpleI3rabGeneration;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

// Regression coverage for the attached-pronoun (ضمير متصل) encoding gap: the corpus glues the
// person into the POS token for affixes (e.g. "SUFFIX|PRON:3MP"), unlike stems which carry the
// person as a standalone token ("STEM|POS:PRON|3MS"). The earlier fixture used the standalone form
// for suffixes, so the bare-"SUFFIX:PRON" miss against the catalogue went undetected.
public sealed class SuffixPronounSignatureTests
{
    // All 16 person variants the catalogue carries as SUFFIX:PRON:* keys, expressed in the real
    // corpus affix encoding "SUFFIX|PRON:<person>".
    public static TheoryData<string, string> SuffixPronounCases => new()
    {
        { "SUFFIX|PRON:3MP", "SUFFIX:PRON:3MP" },
        { "SUFFIX|PRON:2MP", "SUFFIX:PRON:2MP" },
        { "SUFFIX|PRON:3MS", "SUFFIX:PRON:3MS" },
        { "SUFFIX|PRON:1P", "SUFFIX:PRON:1P" },
        { "SUFFIX|PRON:2MS", "SUFFIX:PRON:2MS" },
        { "SUFFIX|PRON:1S", "SUFFIX:PRON:1S" },
        { "SUFFIX|PRON:3FS", "SUFFIX:PRON:3FS" },
        { "SUFFIX|PRON:3FP", "SUFFIX:PRON:3FP" },
        { "SUFFIX|PRON:3MD", "SUFFIX:PRON:3MD" },
        { "SUFFIX|PRON:3D", "SUFFIX:PRON:3D" },
        { "SUFFIX|PRON:2D", "SUFFIX:PRON:2D" },
        { "SUFFIX|PRON:2FS", "SUFFIX:PRON:2FS" },
        { "SUFFIX|PRON:2FP", "SUFFIX:PRON:2FP" },
        { "SUFFIX|PRON:2MD", "SUFFIX:PRON:2MD" },
        { "SUFFIX|PRON:3FD", "SUFFIX:PRON:3FD" },
        { "SUFFIX|PRON:2FD", "SUFFIX:PRON:2FD" }
    };

    // Stem pronouns must keep working: they carry the person as a standalone token.
    public static TheoryData<string, string> StemPronounCases => new()
    {
        { "STEM|POS:PRON|3MS", "STEM:PRON:3MS" },
        { "STEM|POS:PRON|3MP", "STEM:PRON:3MP" },
        { "STEM|POS:PRON|1P", "STEM:PRON:1P" },
        { "STEM|POS:PRON|1S", "STEM:PRON:1S" }
    };

    [Theory]
    [MemberData(nameof(SuffixPronounCases))]
    public void Build_maps_real_suffix_pronoun_encoding_to_person_qualified_signature(
        string featuresRaw,
        string expectedSignature)
    {
        var input = SuffixPronoun(featuresRaw);

        SegmentSignatureBuilder.Build(input).Should().Be(expectedSignature);
    }

    [Theory]
    [MemberData(nameof(SuffixPronounCases))]
    public void Generated_suffix_pronoun_signature_resolves_against_catalogue(
        string featuresRaw,
        string expectedSignature)
    {
        var catalog = new I3rabRuleCatalogSeed();

        var signature = SegmentSignatureBuilder.Build(SuffixPronoun(featuresRaw));

        signature.Should().Be(expectedSignature);
        catalog.TryGet(signature, out var row).Should().BeTrue(
            "the generated suffix-pronoun signature must match a seeded catalogue key");
        row.RuleFamily.Should().Be("PRON.SUF");
    }

    [Theory]
    [MemberData(nameof(StemPronounCases))]
    public void Build_keeps_standalone_stem_pronoun_behavior(
        string featuresRaw,
        string expectedSignature)
    {
        var input = new I3rabSegmentInput(1, 1, 1, "STEM", "PRON", featuresRaw, null, null, null, false, false);

        SegmentSignatureBuilder.Build(input).Should().Be(expectedSignature);
    }

    private static I3rabSegmentInput SuffixPronoun(string featuresRaw) =>
        new(1, 1, 2, "SUFFIX", "PRON", featuresRaw, null, null, null, false, false);
}
