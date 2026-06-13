using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;
using QuranDashboard.Application.Quran.Mutashabihat.ImportMutashabihat;
using QuranDashboard.Infrastructure.Files.Quran.Mutashabihat;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

public sealed class MutashabihatAssemblerTests
{
    private static readonly Dictionary<string, int> AyahIds = new(StringComparer.Ordinal)
    {
        ["900:1"] = 1,
        ["900:2"] = 2,
        ["900:3"] = 3,
        ["900:4"] = 4,
        ["900:99"] = 99
    };

    [Fact]
    public void Assemble_resolves_verse_key_to_ayah_id()
    {
        var phrases = CreatePhrases(
            sourceGroupId: 1,
            sourceKey: "900:1",
            sourceFrom: 1,
            sourceTo: 2,
            occurrences: new Dictionary<string, IReadOnlyList<WordRange>>
            {
                ["900:1"] = [new WordRange(1, 2)],
                ["900:2"] = [new WordRange(1, 2)]
            });

        var result = Assemble(phrases);

        result.Groups.Single().RepresentativeAyahId.Should().Be(1);
        result.Groups.Single().Occurrences.Should().OnlyContain(
            occurrence => occurrence.AyahId == 1 || occurrence.AyahId == 2);
    }

    [Fact]
    public void Assemble_collapses_duplicate_identical_occurrence()
    {
        var phrases = CreatePhrases(
            sourceGroupId: 75,
            sourceKey: "900:1",
            sourceFrom: 17,
            sourceTo: 19,
            occurrences: new Dictionary<string, IReadOnlyList<WordRange>>
            {
                ["900:1"] = [new WordRange(17, 19), new WordRange(17, 19)],
                ["900:2"] = [new WordRange(17, 19)]
            },
            rawSourceCountsJson: """{"surahs":1,"ayahs":2,"count":3}""");

        var result = Assemble(phrases);

        var group = result.Groups.Single();
        group.OccurrenceCount.Should().Be(2, "3 raw entries collapse to 2 stored unique occurrences");
        group.Occurrences.Should().HaveCount(2);
        group.Occurrences.Count(occurrence => occurrence.AyahId == 1).Should().Be(1);
    }

    [Fact]
    public void Assemble_recomputes_counters_from_actual_occurrences_not_stale_source()
    {
        var phrases = CreatePhrases(
            sourceGroupId: 10,
            sourceKey: "900:1",
            sourceFrom: 1,
            sourceTo: 2,
            occurrences: new Dictionary<string, IReadOnlyList<WordRange>>
            {
                ["900:1"] = [new WordRange(1, 2)],
                ["900:2"] = [new WordRange(1, 2)]
            },
            rawSourceCountsJson: """{"surahs":9,"ayahs":99,"count":999}""");

        var result = Assemble(phrases);

        var group = result.Groups.Single();
        group.OccurrenceCount.Should().Be(2);
        group.DistinctAyahCount.Should().Be(2);
        group.DistinctSurahCount.Should().Be(1);
        group.RawSourceCountsJson.Should().Be("""{"surahs":9,"ayahs":99,"count":999}""");
    }

    [Fact]
    public void Assemble_flags_representative_occurrence_matching_source_phrase()
    {
        var phrases = CreatePhrases(
            sourceGroupId: 20,
            sourceKey: "900:1",
            sourceFrom: 1,
            sourceTo: 2,
            occurrences: new Dictionary<string, IReadOnlyList<WordRange>>
            {
                ["900:1"] = [new WordRange(1, 2), new WordRange(3, 4)],
                ["900:2"] = [new WordRange(1, 2)]
            });

        var result = Assemble(phrases);

        var group = result.Groups.Single();
        group.Occurrences.Count(occurrence => occurrence.IsRepresentative).Should().Be(1);
        group.Occurrences.Single(occurrence => occurrence.IsRepresentative).Should()
            .BeEquivalentTo(new { AyahId = 1, WordFrom = (short)1, WordTo = (short)2 });
    }

    [Fact]
    public void Assemble_keeps_group_with_absent_source_key_and_zero_representative_occurrences()
    {
        var phrases = CreatePhrases(
            sourceGroupId: 1782,
            sourceKey: "900:99",
            sourceFrom: 1,
            sourceTo: 1,
            occurrences: new Dictionary<string, IReadOnlyList<WordRange>>
            {
                ["900:1"] = [new WordRange(1, 2)],
                ["900:2"] = [new WordRange(1, 2)]
            });

        var result = Assemble(phrases);

        var group = result.Groups.Single(group => group.SourceGroupId == 1782);
        group.RepresentativeAyahId.Should().Be(99);
        group.RepresentativeWordFrom.Should().Be(1);
        group.RepresentativeWordTo.Should().Be(1);
        group.Occurrences.Should().HaveCount(2);
        group.Occurrences.Should().OnlyContain(occurrence => !occurrence.IsRepresentative);
    }

    [Fact]
    public void Assemble_throws_when_verse_key_does_not_resolve()
    {
        var phrases = CreatePhrases(
            sourceGroupId: 30,
            sourceKey: "900:1",
            sourceFrom: 1,
            sourceTo: 1,
            occurrences: new Dictionary<string, IReadOnlyList<WordRange>>
            {
                ["900:missing"] = [new WordRange(1, 1)],
                ["900:2"] = [new WordRange(1, 1)]
            });

        var act = () => Assemble(phrases);

        act.Should().Throw<InvalidDataException>()
            .Which.Message.Should().Contain("900:missing");
    }

    private static PhrasesReadResult CreatePhrases(
        int sourceGroupId,
        string sourceKey,
        short sourceFrom,
        short sourceTo,
        IReadOnlyDictionary<string, IReadOnlyList<WordRange>> occurrences,
        string? rawSourceCountsJson = null) =>
        new(
        [
            new ParsedPhraseGroup(
                sourceGroupId,
                new PhraseSourceBlock(sourceKey, sourceFrom, sourceTo),
                rawSourceCountsJson,
                occurrences)
        ],
        occurrences.Values.Sum(ranges => ranges.Count));

    private static MutashabihatSourceData Assemble(PhrasesReadResult phrases) =>
        new MutashabihatAssembler().AssembleGroups(phrases, AyahIds);
}
