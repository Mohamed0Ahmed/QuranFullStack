using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

namespace QuranDashboard.Tests.Quran.Translations;

[Collection(nameof(TranslationImportTestCollection))]
public sealed class TranslationExcludedSourceTests(TranslationImportTestFixture fixture)
{
    private static TranslationExpectedCounts ExpectedWithOneExcluded =>
        TranslationSyntheticSeed.DefaultTestExpectedCounts with { ExcludedSources = 1 };

    [Fact]
    public async Task Excluded_word_by_word_source_is_not_persisted()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.DefaultSources,
            excludedSourceKeys: ["en-excluded-wbw"],
            excludedCategory: "wordByWord");

        var result = await fixture.RunImportAsync(packageDir, ExpectedWithOneExcluded);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await dbContext.TranslationSources.AsNoTracking()
            .AnyAsync(s => s.SourceKey == "en-excluded-wbw"))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Excluded_empty_text_source_is_not_persisted()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.DefaultSources,
            excludedSourceKeys: ["en-excluded-empty"],
            excludedCategory: "emptyText");

        var result = await fixture.RunImportAsync(packageDir, ExpectedWithOneExcluded);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await dbContext.TranslationSources.AsNoTracking()
            .AnyAsync(s => s.SourceKey == "en-excluded-empty"))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Excluded_unattributed_near_duplicate_source_is_not_persisted()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.DefaultSources,
            excludedSourceKeys: ["en-excluded-dup"],
            excludedCategory: "unattributedNearDuplicate");

        var result = await fixture.RunImportAsync(packageDir, ExpectedWithOneExcluded);

        result.Succeeded.Should().BeTrue(result.Message);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await dbContext.TranslationSources.AsNoTracking()
            .AnyAsync(s => s.SourceKey == "en-excluded-dup"))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Approved_source_key_overlapping_excluded_set_fails_tr_no_excluded_sources()
    {
        await fixture.TruncateTranslationTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TranslationSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TranslationSyntheticSeed.DefaultSources,
            excludedSourceKeys: ["en-test-simple"]);

        var result = await fixture.RunImportAsync(packageDir, ExpectedWithOneExcluded);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(TranslationInvariants.CheckNoExcludedSources);

        var snapshot = await fixture.CaptureTranslationTableSnapshotAsync();
        snapshot.SourceRows.Should().Be(0);
        snapshot.AyahEntryRows.Should().Be(0);
    }
}
