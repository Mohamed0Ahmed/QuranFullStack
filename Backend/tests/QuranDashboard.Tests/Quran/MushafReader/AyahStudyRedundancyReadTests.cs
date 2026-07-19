using Microsoft.Extensions.Logging.Abstractions;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.MushafReader;

/// <summary>
/// Regression coverage for performance-review finding B4 (ayah study): each source/mapping/text
/// family is now one projected query instead of up to three sequential point queries, and the
/// similarity summary's three counts are combined into one query. These tests pin both the
/// reduced EF command count and the exact response shape across the present / source-missing /
/// mapping-missing cases, so a future regression that reintroduces the fan-out or changes
/// null/not-found semantics is caught.
/// </summary>
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

        // Prior sequential fan-out: ayah(1) + sajda(1) + tafsir(source+mapping+text=3) +
        // translation(source+mapping=2) + full i3rab(source+mapping+text=3) + similarity(2-3,
        // this ayah has mutashabihat groups so 3) = 13 commands (matches the report's measured
        // "13 commands for an ayah with mutashabihat groups"). The collapsed reader issues one
        // projection per family plus one combined similarity-summary query: ayah(1) + sajda(1) +
        // tafsir(1) + translation(1) + full i3rab(1) + similarity(1) = 6.
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

        // 1:1 has no tafsir/translation/full-i3rab ayah mapping in the fixture, so every family's
        // LEFT JOIN resolves the source row but not the mapping/text rows.
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

        // Tafsir/translation are skipped entirely (their source keys are null), so only the
        // full-i3rab family, ayah, sajda, and similarity queries run.
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
