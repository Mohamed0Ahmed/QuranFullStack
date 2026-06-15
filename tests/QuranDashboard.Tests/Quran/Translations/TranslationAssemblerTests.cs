using QuranDashboard.Application.Abstractions.Quran.Translations;
using QuranDashboard.Infrastructure.Files.Quran.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

public sealed class TranslationAssemblerTests
{
    private readonly TranslationAssembler assembler = new();
    private readonly TranslationImportTestFixture fixture = new();

    [Fact]
    public async Task AssembleSource_resolves_verse_keys_and_preserves_exact_text()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.MinimalSources);
        var manifestReader = new TranslationManifestReader();
        var displayReader = new TranslationDisplayMetadataReader();
        var sourceReader = new JsonTranslationSourceReader();

        var manifest = await manifestReader.ReadAsync(
            packageDir,
            CancellationToken.None,
            TranslationSyntheticSeed.MinimalTestExpectedCounts);
        var manifestSource = manifest.ApprovedSources.Single();
        var display = await displayReader.ReadAsync(
            packageDir,
            [manifestSource.SourceKey],
            TranslationSyntheticSeed.MinimalTestExpectedCounts,
            CancellationToken.None);
        var parsed = await sourceReader.ReadAsync(manifestSource.FullPath!, CancellationToken.None);

        var ayahIds = TranslationSyntheticSeed.MinimalAyahs.ToDictionary(
            ayah => ayah.VerseKey,
            ayah => ayah.Id,
            StringComparer.Ordinal);
        var ayahTexts = TranslationSyntheticSeed.MinimalAyahs.ToDictionary(
            ayah => ayah.VerseKey,
            _ => "DIFFERENT-QURAN-TEXT",
            StringComparer.Ordinal);
        var seen = new HashSet<(string SourceKey, int AyahId)>();

        var assembled = assembler.AssembleSource(
            manifestSource,
            display.Records.Single(),
            parsed,
            ayahIds,
            ayahTexts,
            seen,
            expectedAyahsPerSource: 3);

        assembled.AyahEntries.Should().HaveCount(3);
        assembled.AyahEntries[0].Text.Should().Be("SYNTHETIC-TRANSLATION-en-test-minimal-901:1");
        assembled.Source.TranslationType.Should().Be("simple");
        assembled.Source.ContainsInlineFootnotes.Should().BeFalse();
        assembled.Source.ContainsHtmlMarkup.Should().BeFalse();
    }

    [Fact]
    public async Task AssembleSource_detects_inline_footnotes_and_html_markup()
    {
        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources:
            [
                new SyntheticTranslationSourceSpec(
                    SourceKey: "en-markup",
                    LanguageCode: "en",
                    LanguageNameEn: "English",
                    LanguageNameAr: "الإنجليزية",
                    NativeName: "English",
                    Direction: "ltr",
                    TranslationType: "simple",
                    DisplayNameEn: "Synthetic Markup",
                    DisplayNameAr: "ترجمة اختبارية",
                    TranslatorKey: "test",
                    TranslatorNameEn: "Test",
                    TranslatorNameAr: "اختبار",
                    ContentCoverageCount: 1,
                    Entries: new Dictionary<string, string>
                    {
                        ["901:1"] = "SYNTHETIC <span>html</span> [[footnote]]"
                    })
            ]);

        var manifestReader = new TranslationManifestReader();
        var displayReader = new TranslationDisplayMetadataReader();
        var sourceReader = new JsonTranslationSourceReader();

        var manifest = await manifestReader.ReadAsync(
            packageDir,
            CancellationToken.None,
            new TranslationExpectedCounts(1, 0, 1, 0, 1, 1, 1));
        var manifestSource = manifest.ApprovedSources.Single();
        var display = await displayReader.ReadAsync(
            packageDir,
            [manifestSource.SourceKey],
            new TranslationExpectedCounts(1, 0, 1, 0, 1, 1, 1),
            CancellationToken.None);
        var parsed = await sourceReader.ReadAsync(manifestSource.FullPath!, CancellationToken.None);

        var assembled = assembler.AssembleSource(
            manifestSource,
            display.Records.Single(),
            parsed,
            new Dictionary<string, int> { ["901:1"] = 1 },
            new Dictionary<string, string> { ["901:1"] = "DIFFERENT" },
            new HashSet<(string SourceKey, int AyahId)>(),
            expectedAyahsPerSource: 1);

        assembled.Source.ContainsInlineFootnotes.Should().BeTrue();
        assembled.Source.ContainsHtmlMarkup.Should().BeTrue();
        assembled.Source.TranslationType.Should().Be("with_footnotes");
        assembled.AyahEntries.Single().Text.Should().Be("SYNTHETIC <span>html</span> [[footnote]]");
    }
}
