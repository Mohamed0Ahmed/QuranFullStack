using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Files.Quran.Morphology.Irab;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

public sealed class SegmentSignatureBuilderTests
{
    public static TheoryData<I3rabSegmentInput, string> SignatureCases => new()
    {
        {
            new I3rabSegmentInput(1, 1, 1, "STEM", "N", "GEN", "genitive", null, null, false, false),
            "STEM:N:GEN"
        },
        {
            new I3rabSegmentInput(2, 1, 1, "STEM", "V", "PERF|ACT|3MS", null, "past", "active", false, false),
            "STEM:V:PERF:ACT:3MS"
        },
        {
            new I3rabSegmentInput(3, 1, 2, "SUFFIX", "PRON", "3MP", null, null, null, false, false),
            "SUFFIX:PRON:3MP"
        },
        {
            new I3rabSegmentInput(4, 1, 1, "STEM", "PN", "GEN", "genitive", null, null, true, false),
            "STEM:PN:ALLAH:GEN"
        },
        {
            new I3rabSegmentInput(6, 1, 1, "STEM", "PN", "NOM", "nominative", null, null, true, false),
            "STEM:PN:ALLAH:NOM"
        },
        {
            new I3rabSegmentInput(7, 1, 1, "STEM", "PN", "ACC", "accusative", null, null, true, false),
            "STEM:PN:ALLAH:ACC"
        },
        {
            new I3rabSegmentInput(5, 1, 1, "STEM", "N", "GEN|1S", "genitive", null, null, false, false),
            "STEM:N:GEN:1S"
        }
    };

    [Theory]
    [MemberData(nameof(SignatureCases))]
    public void Build_maps_known_morphology_inputs_to_expected_signature_keys(
        I3rabSegmentInput input,
        string expectedSignature)
    {
        SegmentSignatureBuilder.Build(input).Should().Be(expectedSignature);
    }
}
