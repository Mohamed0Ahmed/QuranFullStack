using QuranDashboard.Application.Abstractions.Quran.Import;
using QuranDashboard.Application.Quran.Import.ImportQuranFoundation;
using QuranDashboard.Application.Quran.Import.Validation;

namespace QuranDashboard.Tests.Quran.Import;

public sealed class ImlaeiCleanKeyBindingTests
{
    private readonly QuranImportValidator validator = new();

    // The raw imlaei text must always survive into TextImlaeiSimple, while the clean
    // identity key is bound from the source's text_clean (or empty when the source omits it).
    [Theory]
    [InlineData("i2_clean", "i2_clean")]
    [InlineData(null, "")]
    public void Assemble_BindsImlaeiTextCleanToWordKey_AndKeepsRawImlaei(string? sourceClean, string expectedKey)
    {
        var assembled = new QuranFoundationAssembler().Assemble(BuildSyntheticSource(sourceClean));

        var word = assembled.Words.Single(word => word.Location == "1:1:2");
        word.TextImlaeiSimple.Should().Be("i2");
        word.WordKeyImlaeiSimple.Should().Be(expectedKey);
    }

    [Fact]
    public void Validate_WhenImlaeiRecordMissingTextClean_FailsImlaeiCleanKeyCheck()
    {
        var source = new QuranImportSourceData(
            Surahs: [],
            Ayahs: [],
            Glyph: [],
            Uthmani: [],
            UthmaniSimple: [],
            ImlaeiSimple:
            [
                new WordRecordDto(1, 1, 1, 1, "1:1:1", "raw1", "clean1"),
                new WordRecordDto(2, 1, 1, 2, "1:1:2", "raw2", TextClean: null)
            ],
            Layout: new LayoutDto(0, 0, new Dictionary<int, IReadOnlyList<LineDto>>()),
            ManifestVersion: "1");

        var result = validator.Validate(
            "/tmp/quran-source",
            "1",
            source,
            new AssembledQuranData([], [], [], [], []),
            forced: false);

        var check = result.Checks.Single(check => check.Id == ImportValidationCheckIds.ImlaeiCleanKey);
        check.Passed.Should().BeFalse();
        check.Observed.Should().Be("1");
    }

    private static QuranImportSourceData BuildSyntheticSource(string? word2Clean)
    {
        WordRecordDto Plain(int id, int word, string text) =>
            new(id, 1, 1, word, $"1:1:{word}", text);

        return new QuranImportSourceData(
            Surahs:
            [
                new SurahMetaDto(1, "SYNTHETIC_SURAH", "SYNTHETIC_SURAH", "SURAH_NAME_PLACEHOLDER", 5, "makkah", 7, false)
            ],
            Ayahs:
            [
                new AyahMetaDto(1, 1, 1, "1:1", 4, "AYAH_TEXT_PLACEHOLDER")
            ],
            Glyph:
            [
                Plain(1, 1, "g1"), Plain(2, 2, "g2"), Plain(3, 3, "g3"), Plain(4, 4, "g4"), Plain(5, 5, "g5")
            ],
            Uthmani:
            [
                Plain(1, 1, "u1"), Plain(2, 2, "u2"), Plain(3, 3, "u3"), Plain(4, 4, "u4"), Plain(5, 5, "u5")
            ],
            UthmaniSimple:
            [
                Plain(1, 1, "us1"), Plain(2, 2, "us2"), Plain(3, 3, "us3"), Plain(4, 4, "us4"), Plain(5, 5, "us5")
            ],
            ImlaeiSimple:
            [
                new WordRecordDto(1, 1, 1, 1, "1:1:1", "i1", "i1_clean"),
                new WordRecordDto(2, 1, 1, 2, "1:1:2", "i2", word2Clean),
                new WordRecordDto(3, 1, 1, 3, "1:1:3", "i3", "i3_clean"),
                new WordRecordDto(4, 1, 1, 4, "1:1:4", "i4", "i4_clean"),
                new WordRecordDto(5, 1, 1, 5, "1:1:5", "1", "1")
            ],
            Layout: new LayoutDto(
                1,
                2,
                new Dictionary<int, IReadOnlyList<LineDto>>
                {
                    [1] =
                    [
                        new LineDto(1, 1, "surah_name", true, 1, null, null),
                        new LineDto(1, 2, "ayah", true, null, 1, 5)
                    ]
                }));
    }
}
