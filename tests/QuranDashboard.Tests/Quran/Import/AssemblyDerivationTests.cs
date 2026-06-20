using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;
using QuranDashboard.Domain.Quran.Words;

using QuranDashboard.Application.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Tests.Quran.Import;

public sealed class AssemblyDerivationTests
{
    [Fact]
    public void DeriveWordPlacements_AssignsPageLineAndOrderFromAyahRanges()
    {
        var layout = new LayoutDto(
            PagesCount: 1,
            LinesPerPage: 2,
            Pages: new Dictionary<int, IReadOnlyList<LineDto>>
            {
                [1] =
                [
                    new LineDto(1, 1, "surah_name", true, 1, null, null),
                    new LineDto(1, 2, "ayah", true, null, 1, 3)
                ]
            });

        var placements = QuranFoundationAssembler.DeriveWordPlacements(layout);

        placements.Should().HaveCount(3);
        placements[1].Should().Be(new QuranFoundationAssembler.WordPlacement(1, 2, 1));
        placements[2].Should().Be(new QuranFoundationAssembler.WordPlacement(1, 2, 2));
        placements[3].Should().Be(new QuranFoundationAssembler.WordPlacement(1, 2, 3));
    }

    [Fact]
    public void FlagAyahMarkers_FlagsOnlyLastDigitOnlyWordInEachAyah()
    {
        var words = new List<QuranWord>
        {
            CreateWord(1, 1, 1, 1, "word"),
            CreateWord(2, 1, 1, 2, "2"),
            CreateWord(3, 1, 2, 1, "next"),
            CreateWord(4, 1, 2, 2, "x")
        };

        QuranFoundationAssembler.FlagAyahMarkers(words);

        words.Single(word => word.Id == 1).IsAyahMarker.Should().BeFalse();
        words.Single(word => word.Id == 2).IsAyahMarker.Should().BeTrue();
        words.Single(word => word.Id == 3).IsAyahMarker.Should().BeFalse();
        words.Single(word => word.Id == 4).IsAyahMarker.Should().BeFalse();
    }

    [Fact]
    public void Assemble_BuildsExpectedPlacementAndMarkerForSyntheticAyah()
    {
        var source = new QuranImportSourceData(
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
                new WordRecordDto(1, 1, 1, 1, "1:1:1", "g1"),
                new WordRecordDto(2, 1, 1, 2, "1:1:2", "g2"),
                new WordRecordDto(3, 1, 1, 3, "1:1:3", "g3"),
                new WordRecordDto(4, 1, 1, 4, "1:1:4", "g4"),
                new WordRecordDto(5, 1, 1, 5, "1:1:5", "g5")
            ],
            Uthmani:
            [
                new WordRecordDto(1, 1, 1, 1, "1:1:1", "u1"),
                new WordRecordDto(2, 1, 1, 2, "1:1:2", "u2"),
                new WordRecordDto(3, 1, 1, 3, "1:1:3", "u3"),
                new WordRecordDto(4, 1, 1, 4, "1:1:4", "u4"),
                new WordRecordDto(5, 1, 1, 5, "1:1:5", "u5")
            ],
            UthmaniSimple:
            [
                new WordRecordDto(1, 1, 1, 1, "1:1:1", "us1"),
                new WordRecordDto(2, 1, 1, 2, "1:1:2", "us2"),
                new WordRecordDto(3, 1, 1, 3, "1:1:3", "us3"),
                new WordRecordDto(4, 1, 1, 4, "1:1:4", "us4"),
                new WordRecordDto(5, 1, 1, 5, "1:1:5", "us5")
            ],
            ImlaeiSimple:
            [
                new WordRecordDto(1, 1, 1, 1, "1:1:1", "i1"),
                new WordRecordDto(2, 1, 1, 2, "1:1:2", "i2"),
                new WordRecordDto(3, 1, 1, 3, "1:1:3", "i3"),
                new WordRecordDto(4, 1, 1, 4, "1:1:4", "i4"),
                new WordRecordDto(5, 1, 1, 5, "1:1:5", "1")
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

        var assembled = new QuranFoundationAssembler().Assemble(source);

        var marker = assembled.Words.Single(word => word.IsAyahMarker);
        marker.Location.Should().Be("1:1:5");
        marker.PageNumber.Should().Be(1);
        marker.LineNumber.Should().Be(2);
        marker.LineWordOrder.Should().Be(5);

        assembled.Ayahs.Single().WordsCountReal.Should().Be(4);
        assembled.Ayahs.Single().PageFrom.Should().Be(1);
        assembled.Ayahs.Single().PageTo.Should().Be(1);
    }

    private static QuranWord CreateWord(int id, short surah, short ayah, short wordNumber, string imlaeiText) =>
        new()
        {
            Id = id,
            Location = $"{surah}:{ayah}:{wordNumber}",
            SurahNumber = surah,
            AyahNumber = ayah,
            WordNumber = wordNumber,
            TextImlaeiSimple = imlaeiText
        };
}
