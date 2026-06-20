using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Application.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Mutashabihat;

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

    [Fact]
    public void AssembleLinks_resolves_both_ends_to_ayah_id()
    {
        var similarSources = CreateSimilarSources(
            ("900:1",
            [
                new ParsedSimilarLinkItem("900:2", 80, 50, 2, """[[1,2]]""")
            ]));

        var links = AssembleLinks(similarSources);

        links.Should().ContainSingle();
        links.Single().SourceAyahId.Should().Be(1);
        links.Single().TargetAyahId.Should().Be(2);
    }

    [Fact]
    public void AssembleLinks_produces_no_reverse_row_for_one_way_link()
    {
        var similarSources = CreateSimilarSources(
            ("900:1",
            [
                new ParsedSimilarLinkItem("900:2", 80, 50, 2, """[[1,2]]""")
            ]));

        var links = AssembleLinks(similarSources);

        links.Should().HaveCount(1);
        links.Should().NotContain(link => link.SourceAyahId == 2 && link.TargetAyahId == 1);
    }

    [Fact]
    public void AssembleLinks_preserves_raw_coverage_above_100()
    {
        var similarSources = CreateSimilarSources(
            ("900:1",
            [
                new ParsedSimilarLinkItem("900:2", 90, 200, 1, """[[1]]""")
            ]));

        var links = AssembleLinks(similarSources);

        links.Single().Coverage.Should().Be(200);
    }

    [Fact]
    public void AssembleLinks_detects_self_link_for_mut_link_no_self_check()
    {
        var similarSources = CreateSimilarSources(
            ("900:1",
            [
                new ParsedSimilarLinkItem("900:1", 80, 50, 1, """[[1]]""")
            ]));

        var links = AssembleLinks(similarSources);

        links.Should().ContainSingle();
        links.Single().SourceAyahId.Should().Be(links.Single().TargetAyahId);
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

    private static IReadOnlyList<ParsedSimilarSource> CreateSimilarSources(
        params (string SourceVerseKey, IReadOnlyList<ParsedSimilarLinkItem> Links)[] sources) =>
        sources.Select(source => new ParsedSimilarSource(source.SourceVerseKey, source.Links)).ToList();

    private static IReadOnlyList<SimilarLinkDto> AssembleLinks(
        IReadOnlyList<ParsedSimilarSource> similarSources) =>
        new MutashabihatAssembler().AssembleLinks(similarSources, AyahIds);
}
