using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

public sealed class TranslationValidationFailureTests : IDisposable
{
    private readonly TranslationManifestReader manifestReader = new();
    private readonly TranslationAssembler assembler = new();
    private readonly TranslationSyntheticPackage packages = new();

    private static readonly TranslationExpectedCounts SingleSourceExpected = new(
        ApprovedSources: 1,
        SimpleSources: 1,
        WithFootnotesSources: 0,
        ExcludedSources: 0,
        Languages: 1,
        AyahsPerSource: 1,
        SourceAyahMappings: 1);

    public void Dispose() => packages.Dispose();

    [Fact]
    public async Task Missing_package_file_fails_with_tr_package_shape()
    {
        var packageDir = await packages.WriteAsync(
            sources:
            [
                new SyntheticTranslationSourceSpec(
                    SourceKey: "en-missing-pkg",
                    LanguageCode: "en",
                    LanguageNameEn: "English",
                    LanguageNameAr: "الإنجليزية",
                    NativeName: "English",
                    Direction: "ltr",
                    TranslationType: "simple",
                    DisplayNameEn: "Test",
                    DisplayNameAr: "اختبار",
                    TranslatorKey: null,
                    TranslatorNameEn: null,
                    TranslatorNameAr: null,
                    ContentCoverageCount: 1,
                    Entries: new Dictionary<string, string> { ["901:1"] = "SYNTHETIC-en-missing-pkg-901:1" })
            ]);

        File.Delete(Path.Combine(packageDir, "README.md"));

        var act = () => manifestReader.ReadAsync(packageDir, CancellationToken.None, SingleSourceExpected);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckPackageShape && !check.Passed);
    }

    [Fact]
    public async Task Non_final_manifest_fails_with_tr_manifest_final()
    {
        var packageDir = await packages.WriteAsync(
            sources: TranslationSyntheticSeed.MinimalSources,
            isFinalImportManifest: false);

        var act = () => manifestReader.ReadAsync(
            packageDir, CancellationToken.None, TranslationSyntheticSeed.MinimalTestExpectedCounts);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckManifestFinal && !check.Passed);
    }

    [Fact]
    public async Task Wrong_source_count_fails_with_tr_source_count()
    {
        var packageDir = await packages.WriteAsync();

        var wrongCounts = TranslationSyntheticSeed.DefaultTestExpectedCounts with { ApprovedSources = 999 };

        var act = () => manifestReader.ReadAsync(packageDir, CancellationToken.None, wrongCounts);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckSourceCount && !check.Passed);
    }

    [Fact]
    public async Task Extra_source_file_fails_with_tr_source_set()
    {
        var packageDir = await packages.WriteAsync();

        await File.WriteAllTextAsync(
            Path.Combine(packageDir, "sources", "en-extra-unlisted.json"),
            "{\"901:1\":{\"t\":\"extra\"}}");

        var act = () => manifestReader.ReadAsync(
            packageDir, CancellationToken.None, TranslationSyntheticSeed.DefaultTestExpectedCounts);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckSourceSet && !check.Passed);
    }

    [Fact]
    public async Task Tampered_source_file_fails_with_tr_source_hash()
    {
        var packageDir = await packages.WriteAsync();

        var sourceFile = Path.Combine(packageDir, "sources", "en-test-simple.json");
        await File.AppendAllTextAsync(sourceFile, "TAMPERED");

        var act = () => manifestReader.ReadAsync(
            packageDir, CancellationToken.None, TranslationSyntheticSeed.DefaultTestExpectedCounts);

        (await act.Should().ThrowAsync<TranslationValidationException>())
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckSourceHash && !check.Passed);
    }

    [Fact]
    public void Unresolved_ayah_key_fails_with_tr_ayah_keys_resolve()
    {
        var (manifestSource, display, parsed) = BuildAssemblerInputs(
            sourceKey: "en-unresolved",
            verseKey: "901:1",
            text: "SYNTHETIC-unresolved");

        var emptyAyahIds = new Dictionary<string, int>(StringComparer.Ordinal);
        var emptyAyahTexts = new Dictionary<string, string>(StringComparer.Ordinal);

        var act = () => assembler.AssembleSource(
            manifestSource, display, parsed, emptyAyahIds, emptyAyahTexts,
            new HashSet<(string SourceKey, int AyahId)>(), expectedAyahsPerSource: 1);

        act.Should().Throw<TranslationValidationException>()
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckAyahKeysResolve && !check.Passed);
    }

    [Fact]
    public void Duplicate_source_ayah_mapping_fails_with_tr_no_duplicate_ayah_entry()
    {
        var (manifestSource, display, parsed) = BuildAssemblerInputs(
            sourceKey: "en-duplicate",
            verseKey: "901:1",
            text: "SYNTHETIC-duplicate");

        var ayahIds = new Dictionary<string, int>(StringComparer.Ordinal) { ["901:1"] = 1 };
        var ayahTexts = new Dictionary<string, string>(StringComparer.Ordinal) { ["901:1"] = "DIFFERENT-QURAN" };

        var seen = new HashSet<(string SourceKey, int AyahId)> { ("en-duplicate", 1) };

        var act = () => assembler.AssembleSource(
            manifestSource, display, parsed, ayahIds, ayahTexts, seen, expectedAyahsPerSource: 1);

        act.Should().Throw<TranslationValidationException>()
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckNoDuplicateAyahEntry && !check.Passed);
    }

    [Fact]
    public void Copied_quran_text_fails_with_tr_no_quran_text_copy()
    {
        const string copiedText = "TEST-QURAN-TEXT-901-1";

        var (manifestSource, display, parsed) = BuildAssemblerInputs(
            sourceKey: "en-quran-copy",
            verseKey: "901:1",
            text: copiedText);

        var ayahIds = new Dictionary<string, int>(StringComparer.Ordinal) { ["901:1"] = 1 };
        var ayahTexts = new Dictionary<string, string>(StringComparer.Ordinal) { ["901:1"] = copiedText };

        var act = () => assembler.AssembleSource(
            manifestSource, display, parsed, ayahIds, ayahTexts,
            new HashSet<(string SourceKey, int AyahId)>(), expectedAyahsPerSource: 1);

        act.Should().Throw<TranslationValidationException>()
            .Which.FailedChecks.Should().Contain(check =>
                check.Id == TranslationInvariants.CheckNoQuranTextCopy && !check.Passed);
    }

    private static (TranslationManifestSourceRecord Manifest, TranslationDisplayMetadataRecord Display, ParsedTranslationSourceFile Parsed)
        BuildAssemblerInputs(string sourceKey, string verseKey, string text)
    {
        var manifest = new TranslationManifestSourceRecord(
            sourceKey, "en", "simple", 1,
            $"sources/{sourceKey}.json", "ABCD", 100)
        {
            FullPath = $"/synthetic/{sourceKey}.json"
        };

        var display = new TranslationDisplayMetadataRecord(
            sourceKey,
            $"sources/{sourceKey}.json",
            "en", "English", "الإنجليزية", "English",
            "ltr", "simple", "Test", "اختبار",
            TranslatorKey: null, TranslatorNameEn: null, TranslatorNameAr: null,
            MetadataStatus: TranslationImportConstants.DisplayRecordFinalStatus);

        var parsed = new ParsedTranslationSourceFile(
            $"/synthetic/{sourceKey}.json",
            new Dictionary<string, string>(StringComparer.Ordinal) { [verseKey] = text });

        return (manifest, display, parsed);
    }
}
