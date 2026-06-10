using FluentAssertions;
using QuranDashboard.Application.Quran.Words.ImportMorphology;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class MorphologyVerbFeatureTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public async Task Import_maps_verb_tense_voice_and_case_features_consistently()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        var expectedReadableCount = fixture.GetReadableWordCount();

        var result = await fixture.RunImportAsync(sourcePath, expectedReadableWords: expectedReadableCount);

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var verbRow = await dbContext.WordMorphologies
            .AsNoTracking()
            .SingleAsync(row => row.Location == "1:2:1");

        verbRow.IsVerb.Should().BeTrue();
        verbRow.HeadPos.Should().Be("V");
        verbRow.VerbTense.Should().Be("past");
        verbRow.VerbVoice.Should().Be("passive");
        verbRow.CaseFeature.Should().BeNull();

        var nominativeRow = await dbContext.WordMorphologies
            .AsNoTracking()
            .SingleAsync(row => row.Location == "1:1:1");

        nominativeRow.IsVerb.Should().BeFalse();
        nominativeRow.VerbTense.Should().BeNull();
        nominativeRow.VerbVoice.Should().BeNull();
        nominativeRow.CaseFeature.Should().Be("nominative");

        var genitiveRow = await dbContext.WordMorphologies
            .AsNoTracking()
            .SingleAsync(row => row.Location == "1:1:2");

        genitiveRow.CaseFeature.Should().Be("genitive");
        genitiveRow.VerbTense.Should().BeNull();
        genitiveRow.VerbVoice.Should().BeNull();

        var accusativeRow = await dbContext.WordMorphologies
            .AsNoTracking()
            .SingleAsync(row => row.Location == "1:2:2");

        accusativeRow.CaseFeature.Should().Be("accusative");
        accusativeRow.IsVerb.Should().BeFalse();
        accusativeRow.VerbTense.Should().BeNull();
        accusativeRow.VerbVoice.Should().BeNull();

        var nonVerbRows = await dbContext.WordMorphologies
            .AsNoTracking()
            .Where(row => !row.IsVerb)
            .ToListAsync();

        nonVerbRows.Should().OnlyContain(row => row.VerbTense == null && row.VerbVoice == null);
    }

    [Fact]
    public async Task Import_assigns_active_voice_when_pass_marker_is_absent()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchCorpusStemFeaturesAsync(sourcePath, "1:2:1", "IMPF");

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var verbRow = await dbContext.WordMorphologies
            .AsNoTracking()
            .SingleAsync(row => row.Location == "1:2:1");

        verbRow.IsVerb.Should().BeTrue();
        verbRow.VerbTense.Should().Be("present");
        verbRow.VerbVoice.Should().Be("active");
    }
}
