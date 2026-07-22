using Microsoft.Extensions.Logging.Abstractions;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class WordAnalysisRedundancyReadTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetWordAnalysis_fully_populated_issues_far_fewer_commands_than_the_prior_fan_out()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfWordAnalysisReader(dbContext, NullLogger<EfWordAnalysisReader>.Instance);

        var outcome = await reader.GetWordAnalysisAsync("2:25:3", CancellationToken.None);

        interceptor.CommandCount.Should().Be(3);

        var response = outcome.Should().BeOfType<WordAnalysisOutcome.Found>().Subject.Response;
        response.Word.WordLocation.Should().Be("2:25:3");
        response.Word.VerseKey.Should().Be("2:25");

        response.Morphology.HeadPos.Should().Be("V");
        response.Morphology.HeadPosLabel.Should().Be(new LocalizedLabel("فعل", "Verb"));
        response.Morphology.IsVerb.Should().BeTrue();
        response.Morphology.Root.Should().Be(new WordMorphologyRoot(9001, "أ م ن", "Amn"));
        response.Morphology.Lemma.Should().BeNull();
        response.Morphology.Stem.Should().BeNull();

        response.Identity.UniqueTashkeel.Id.Should().Be(2003);
        response.Identity.UniqueSimple.WordKeyImlaeiSimple.Should().Be("امنوا");

        response.RenderedWordSegments.Should().HaveCount(2);
        response.RenderedWordSegments[0].SegmentPosLabel.Should().Be(new LocalizedLabel("فعل", "Verb"));
        response.RenderedWordSegments[1].SegmentPosLabel.Should().Be(new LocalizedLabel("ضمير", "Pronoun"));
    }

    [Fact]
    public async Task GetWordAnalysis_missing_morphology_row_short_circuits_after_one_command()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfWordAnalysisReader(dbContext, NullLogger<EfWordAnalysisReader>.Instance);

        var outcome = await reader.GetWordAnalysisAsync("2:25:1", CancellationToken.None);

        interceptor.CommandCount.Should().Be(1, "a missing morphology row is detected from the core projection alone");
        outcome.Should().BeOfType<WordAnalysisOutcome.IncompleteData>();
    }
}
