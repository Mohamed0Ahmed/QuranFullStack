using Microsoft.Extensions.Logging.Abstractions;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class AyahStudyRedundancyReadTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetAyahStudy_fully_populated_issues_far_fewer_commands_than_the_prior_fan_out()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfAyahStudyReader(dbContext, NullLogger<EfAyahStudyReader>.Instance);

        var response = await reader.GetAyahStudyAsync(
            "2:25", "ar-muyassar", "en-sahih-international", "muyassar", CancellationToken.None);

        interceptor.CommandCount.Should().Be(6);

        response.Should().NotBeNull();
        response!.Ayah.VerseKey.Should().Be("2:25");

        response.Tafsir.Should().NotBeNull();
        response.Tafsir!.SourceKey.Should().Be("ar-muyassar");
        response.Tafsir.TafsirKind.Should().Be("tafsir");
        response.Tafsir.SourceValueKind.Should().Be("leader");
        response.Tafsir.IsGroupLeader.Should().BeTrue();
        response.Tafsir.CoveredAyahCount.Should().Be(2);
        response.Tafsir.CoveredAyahKeys.Should().BeEquivalentTo(["2:25", "2:26"]);
        response.Tafsir.Text.Should().Be("متن تجريبي للتفسير يغطي الآيتين 25 و 26 من سورة البقرة.");

        response.Translation.Should().NotBeNull();
        response.Translation!.SourceKey.Should().Be("en-sahih-international");
        response.Translation.Text.Should().Be(
            "And give good tidings to those who believe and do righteous deeds that for them are gardens.");

        response.FullI3rab.Should().NotBeNull();
        response.FullI3rab!.SourceKey.Should().Be("muyassar");
        response.FullI3rab.CoveredAyahCount.Should().Be(1);
        response.FullI3rab.Html.Should().Be("<p>إعراب تجريبي للآية 2:25.</p>");

        response.SimilaritySummary.Should().Be(new SimilaritySummaryDto(2, 2, 3));
    }

    [Fact]
    public async Task GetAyahStudy_mapping_missing_issues_bounded_commands_and_echoes_resolved_keys_with_null_blocks()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfAyahStudyReader(dbContext, NullLogger<EfAyahStudyReader>.Instance);

        var response = await reader.GetAyahStudyAsync(
            "1:1", "ar-muyassar", "en-sahih-international", "muyassar", CancellationToken.None);

        interceptor.CommandCount.Should().Be(6, "one projection per family plus one similarity query, even when every mapping is missing");

        response.Should().NotBeNull();
        response!.SelectedSources.Should().Be(new SelectedSourcesDto("ar-muyassar", "en-sahih-international", "muyassar"));
        response.Tafsir.Should().BeNull();
        response.Translation.Should().BeNull();
        response.FullI3rab.Should().BeNull();
    }

    [Fact]
    public async Task GetAyahStudy_unknown_source_issues_bounded_commands_and_yields_null_selected_key()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfAyahStudyReader(dbContext, NullLogger<EfAyahStudyReader>.Instance);

        var response = await reader.GetAyahStudyAsync(
            "2:25", null, null, "does-not-exist", CancellationToken.None);

        interceptor.CommandCount.Should().Be(4);

        response.Should().NotBeNull();
        response!.SelectedSources.FullI3rabSource.Should().BeNull();
        response.FullI3rab.Should().BeNull();
        response.SelectedSources.TafsirSource.Should().BeNull();
        response.SelectedSources.TranslationSource.Should().BeNull();
        response.Tafsir.Should().BeNull();
        response.Translation.Should().BeNull();
    }
}
