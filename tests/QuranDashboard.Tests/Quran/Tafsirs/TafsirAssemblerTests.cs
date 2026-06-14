using QuranDashboard.Application.Abstractions.Quran.Tafsirs;
using QuranDashboard.Infrastructure.Files.Quran.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

public sealed class TafsirAssemblerTests
{
    private readonly TafsirAssembler assembler = new();
    private readonly TafsirManifestReader manifestReader = new();
    private readonly JsonTafsirSourceReader sourceReader = new();
    private readonly TafsirImportTestFixture fixture = new();

    [Fact]
    public async Task Assemble_resolves_verse_keys_stores_grouped_text_once_and_expands_ayah_links()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var manifest = await manifestReader.ReadAsync(packageDir, CancellationToken.None);
        var parsedSource = await sourceReader.ReadAsync(
            Path.Combine(packageDir, "sources", "ar-test-tafsir.json"),
            CancellationToken.None);

        var ayahIds = TafsirSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey,
            pair => pair.Id,
            StringComparer.Ordinal);
        var ayahTexts = TafsirSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey,
            pair => $"different-quran-text-{pair.VerseKey}",
            StringComparer.Ordinal);

        var result = assembler.Assemble(
            manifest,
            new Dictionary<string, ParsedTafsirSourceFile>(StringComparer.Ordinal)
            {
                ["ar-test-tafsir"] = parsedSource
            },
            ayahIds,
            ayahTexts);

        result.Sources.Should().ContainSingle();
        result.Entries.Should().HaveCount(2);
        result.AyahEntries.Should().HaveCount(3);

        var groupedEntry = result.Entries.Single(entry => entry.SourceEntryKey == "900:1");
        groupedEntry.SourceShape.Should().Be(TafsirImportConstants.SourceShapeGroupedLeader);
        groupedEntry.CoveredAyahCount.Should().Be(2);
        groupedEntry.TafsirText.Should().Be(TafsirSyntheticSeed.SyntheticText("900:1"));
        groupedEntry.TextHash.Should().Be(TafsirAssembler.ComputeTextHash(groupedEntry.TafsirText));

        result.Entries.Count(entry => entry.TafsirText == groupedEntry.TafsirText).Should().Be(1);

        result.AyahEntries.Should().Contain(entry =>
            entry.VerseKey == "900:2"
            && entry.SourceValueKind == TafsirImportConstants.ValueKindMemberPointer
            && entry.SourceLeaderVerseKey == "900:1");

        result.AyahEntries.Select(entry => entry.AyahId).Distinct().Should().HaveCount(3);
        result.Entries.Should().OnlyContain(entry =>
            !ayahTexts.Values.Contains(entry.TafsirText));
    }

    [Fact]
    public async Task Assemble_refuses_when_tafsir_text_matches_quran_ayah_text()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var manifest = await manifestReader.ReadAsync(packageDir, CancellationToken.None);
        var parsedSource = await sourceReader.ReadAsync(
            Path.Combine(packageDir, "sources", "ar-test-tafsir.json"),
            CancellationToken.None);

        var ayahIds = TafsirSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey,
            pair => pair.Id,
            StringComparer.Ordinal);
        var ayahTexts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["900:1"] = TafsirSyntheticSeed.SyntheticText("900:1")
        };

        var act = () => assembler.Assemble(
            manifest,
            new Dictionary<string, ParsedTafsirSourceFile>(StringComparer.Ordinal)
            {
                ["ar-test-tafsir"] = parsedSource
            },
            ayahIds,
            ayahTexts);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*Quran ayah text*");
    }
}
