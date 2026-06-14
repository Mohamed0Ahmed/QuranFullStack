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

    [Fact]
    public async Task Multi_stem_verb_relative_word_uses_head_verb_features_only()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchCorpusSegmentsAsync(
            sourcePath,
            "1:2:1",
            [
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "V",
                    form = "bi}osa",
                    features = "STEM|POS:V|PERF|LEM:bi}osa|ROOT:bAs|3MS",
                    root = "bAs",
                    lemma = "bi}osa"
                },
                new
                {
                    segmentNumber = (short)2,
                    kind = "STEM",
                    pos = "REL",
                    form = "maA",
                    features = "STEM|POS:REL|LEM:maA",
                    root = (string?)null,
                    lemma = "maA"
                }
            ]);

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var verbRow = await dbContext.WordMorphologies
            .AsNoTracking()
            .SingleAsync(row => row.Location == "1:2:1");

        verbRow.HeadPos.Should().Be("V");
        verbRow.IsVerb.Should().BeTrue();
        verbRow.VerbTense.Should().Be("past");
        verbRow.VerbVoice.Should().Be("active");
    }

    [Fact]
    public async Task Multi_stem_non_verb_head_keeps_word_level_verb_fields_null()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await fixture.PatchCorpusSegmentsAsync(
            sourcePath,
            "1:1:3",
            [
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "P",
                    form = "mi",
                    features = "STEM|POS:P|LEM:min",
                    root = (string?)null,
                    lemma = "min"
                },
                new
                {
                    segmentNumber = (short)2,
                    kind = "STEM",
                    pos = "REL",
                    form = "m~aA",
                    features = "STEM|POS:REL|LEM:maA",
                    root = (string?)null,
                    lemma = "maA"
                }
            ]);

        var result = await fixture.RunImportAsync(
            sourcePath,
            expectedReadableWords: fixture.GetReadableWordCount());

        result.ExitCode.Should().Be(ImportMorphologyResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var row = await dbContext.WordMorphologies
            .AsNoTracking()
            .SingleAsync(word => word.Location == "1:1:3");

        row.HeadPos.Should().Be("P");
        row.IsVerb.Should().BeFalse();
        row.VerbTense.Should().BeNull();
        row.VerbVoice.Should().BeNull();
    }
}
