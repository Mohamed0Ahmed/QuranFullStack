using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;
using QuranDashboard.Application.Quran.DataPipelines.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

[Collection(nameof(TranslationImportTestCollection))]
public sealed class TranslationRefusalForceTests(TranslationImportTestFixture fixture)
{
    [Fact]
    public async Task Normal_rerun_refuses_when_translation_target_tables_contain_data()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.DefaultSources);
        var firstReportDir = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");
        var secondReportDir = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var firstResult = await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            firstReportDir);
        firstResult.Succeeded.Should().BeTrue(firstResult.Message);

        var snapshotBefore = await fixture.CaptureTranslationTableSnapshotAsync();
        snapshotBefore.SourceRows.Should().BeGreaterThan(0);
        snapshotBefore.AyahEntryRows.Should().BeGreaterThan(0);

        var secondResult = await fixture.RunImportAsync(
            packageDir,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            secondReportDir);

        secondResult.Succeeded.Should().BeFalse();
        secondResult.ExitCode.Should().Be(ImportTranslationsResult.RefusedExitCode);
        secondResult.Message.Should().Contain(TranslationInvariants.TargetsNotEmpty);

        var snapshotAfter = await fixture.CaptureTranslationTableSnapshotAsync();
        snapshotAfter.Should().Be(snapshotBefore);

        using var reportDocument = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(secondReportDir, TranslationImportConstants.JsonReportFileName)));
        var checks = reportDocument.RootElement.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().Contain(check =>
            check.GetProperty("id").GetString() == TranslationInvariants.CheckRerunGuard
            && !check.GetProperty("passed").GetBoolean());
    }

    [Fact]
    public async Task Forced_replacement_rebuilds_translation_tables_and_preserves_foundation_rows()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var initialPackage = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.DefaultSources);
        var firstReportDir = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");

        var firstResult = await fixture.RunImportAsync(
            initialPackage,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            firstReportDir);
        firstResult.Succeeded.Should().BeTrue(firstResult.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var foundationBefore = await dbContext.QuranAyahs
            .AsNoTracking()
            .OrderBy(ayah => ayah.Id)
            .Select(ayah => new { ayah.Id, ayah.VerseKey, ayah.TextUthmani })
            .ToListAsync();

        var translationSourceKeyBefore = await dbContext.TranslationSources
            .AsNoTracking()
            .Select(source => source.SourceKey)
            .SingleAsync();

        translationSourceKeyBefore.Should().Be("en-test-simple");

        var replacementPackage = await fixture.WriteSyntheticPackageAsync(
            sources:
            [
                new SyntheticTranslationSourceSpec(
                    SourceKey: "en-test-replacement",
                    LanguageCode: "en",
                    LanguageNameEn: "English",
                    LanguageNameAr: "الإنجليزية",
                    NativeName: "English",
                    Direction: "ltr",
                    TranslationType: "simple",
                    DisplayNameEn: "Synthetic Replacement",
                    DisplayNameAr: "ترجمة بديلة اختبارية",
                    TranslatorKey: "test-replacement",
                    TranslatorNameEn: "Replacement Translator",
                    TranslatorNameAr: "مترجم بديل",
                    ContentCoverageCount: TranslationInvariants.ExpectedAyahsPerSource,
                    Entries: TranslationSyntheticSeed.BuildEntries(
                        "en-test-replacement",
                        TranslationInvariants.ExpectedAyahsPerSource))
            ]);

        var secondReportDir = Path.Combine(Path.GetTempPath(), $"translation-report-{Guid.NewGuid():N}");
        var secondResult = await fixture.RunImportAsync(
            replacementPackage,
            TranslationSyntheticSeed.DefaultTestExpectedCounts,
            secondReportDir,
            force: true);

        secondResult.Succeeded.Should().BeTrue(secondResult.Message);

        var foundationAfter = await dbContext.QuranAyahs
            .AsNoTracking()
            .OrderBy(ayah => ayah.Id)
            .Select(ayah => new { ayah.Id, ayah.VerseKey, ayah.TextUthmani })
            .ToListAsync();
        foundationAfter.Should().BeEquivalentTo(foundationBefore);

        var translationSourceKeyAfter = await dbContext.TranslationSources
            .AsNoTracking()
            .Select(source => source.SourceKey)
            .SingleAsync();
        translationSourceKeyAfter.Should().Be("en-test-replacement");
        translationSourceKeyAfter.Should().NotBe(translationSourceKeyBefore);

        var replacementEntry = await dbContext.TranslationAyahEntries
            .AsNoTracking()
            .SingleAsync(entry => entry.VerseKey == "901:1");
        replacementEntry.Text.Should().Be(
            TranslationSyntheticSeed.SyntheticText("en-test-replacement", "901:1"));

        var snapshot = await fixture.CaptureTranslationTableSnapshotAsync();
        snapshot.SourceRows.Should().Be(1);
        snapshot.AyahEntryRows.Should().Be(TranslationInvariants.ExpectedAyahsPerSource);
    }
}
