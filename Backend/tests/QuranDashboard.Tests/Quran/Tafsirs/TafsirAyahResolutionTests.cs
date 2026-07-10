using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirAyahResolutionTests(TafsirImportTestFixture fixture)
{
    private readonly TafsirAssembler assembler = new();

    [Fact]
    public async Task Import_fails_TAFSIR_AYAH_KEYS_RESOLVE_for_unresolved_verse_key()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var tamperedSources = new List<SyntheticTafsirSourceSpec>
        {
            TafsirSyntheticSeed.IntegrationSources[0] with
            {
                Entries = new Dictionary<string, object>
                {
                    ["900:1"] = new
                    {
                        text = TafsirSyntheticSeed.SyntheticText("900:1"),
                        ayah_keys = new[] { "900:1", "900:2" }
                    },
                    ["900:2"] = "900:1",
                    ["900:99"] = new { text = TafsirSyntheticSeed.SyntheticText("900:99") }
                }
            }
        };

        var packageDir = await fixture.WriteSyntheticPackageAsync(sources: tamperedSources);
        await fixture.RefreshManifestChecksumsAsync(packageDir);

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(TafsirInvariants.CheckAyahKeysResolve);
    }

    [Fact]
    public async Task Assemble_fails_TAFSIR_POINTERS_RESOLVE_for_missing_pointer_target()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var manifestReader = new TafsirManifestReader();
        var sourceReader = new JsonTafsirSourceReader();
        var manifest = await manifestReader.ReadAsync(packageDir, CancellationToken.None);
        var parsed = await sourceReader.ReadAsync(
            Path.Combine(packageDir, "sources", "ar-test-tafsir.json"),
            CancellationToken.None);

        var tamperedEntries = parsed.Entries.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        tamperedEntries["900:2"] = new ParsedTafsirSourceEntry.Pointer("900:2", "900:99");

        var tampered = parsed with
        {
            Entries = tamperedEntries
        };

        var ayahIds = TafsirSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey,
            pair => pair.Id,
            StringComparer.Ordinal);
        var ayahTexts = TafsirSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey,
            pair => $"different-{pair.VerseKey}",
            StringComparer.Ordinal);

        var act = () => assembler.AssembleSource(
            manifest.ApprovedSources[0],
            tampered,
            manifest.ManifestJson,
            ayahIds,
            ayahTexts,
            new HashSet<(string SourceKey, int AyahId)>(),
            expectedAyahsPerSource: 3);

        act.Should().Throw<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckPointersResolve));
    }

    [Fact]
    public async Task Import_fails_TAFSIR_NO_EMPTY_TEXT_for_empty_resolved_text()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var tamperedSources = new List<SyntheticTafsirSourceSpec>
        {
            TafsirSyntheticSeed.IntegrationSources[0] with
            {
                Entries = new Dictionary<string, object>
                {
                    ["900:1"] = new { text = TafsirSyntheticSeed.SyntheticText("900:1") },
                    ["900:2"] = new { text = " " },
                    ["900:3"] = new { text = TafsirSyntheticSeed.SyntheticText("900:3") }
                }
            }
        };

        var packageDir = await fixture.WriteSyntheticPackageAsync(sources: tamperedSources);
        await fixture.RefreshManifestChecksumsAsync(packageDir);

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(TafsirInvariants.CheckNoEmptyText);
    }

    [Fact]
    public async Task Assemble_fails_TAFSIR_NO_DUPLICATE_AYAH_ENTRY_for_duplicate_source_ayah_mapping()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync();
        var manifestReader = new TafsirManifestReader();
        var sourceReader = new JsonTafsirSourceReader();
        var manifest = await manifestReader.ReadAsync(packageDir, CancellationToken.None);
        var parsed = await sourceReader.ReadAsync(
            Path.Combine(packageDir, "sources", "ar-test-tafsir.json"),
            CancellationToken.None);

        var ayahIds = TafsirSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey,
            pair => pair.Id,
            StringComparer.Ordinal);
        var ayahTexts = TafsirSyntheticSeed.DefaultAyahs.ToDictionary(
            pair => pair.VerseKey,
            pair => $"different-{pair.VerseKey}",
            StringComparer.Ordinal);
        var seen = new HashSet<(string SourceKey, int AyahId)>
        {
            ("ar-test-tafsir", 1)
        };

        var act = () => assembler.AssembleSource(
            manifest.ApprovedSources[0],
            parsed,
            manifest.ManifestJson,
            ayahIds,
            ayahTexts,
            seen,
            expectedAyahsPerSource: 3);

        act.Should().Throw<TafsirValidationException>()
            .Where(ex => ex.FailedChecks.Any(check => check.Id == TafsirInvariants.CheckNoDuplicateAyahEntry));
    }
}
