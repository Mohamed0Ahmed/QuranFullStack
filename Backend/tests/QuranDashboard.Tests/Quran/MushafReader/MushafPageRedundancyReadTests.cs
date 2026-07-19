using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class MushafPageRedundancyReadTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task GetPage_cold_read_issues_fewer_commands_than_the_prior_eight()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfMushafPageReader(dbContext);

        var response = await reader.GetPageAsync(1, CancellationToken.None);

        // Prior reads: pageExists(1) + lines(1) + words(1) + surahNames(1) + juz(1) + hizb(1) +
        // rub(1) + sajda(1) = 8 commands (matches the report's measured "8 commands"). The
        // collapsed reader drops the existence probe (the line read doubles as it) and merges
        // juz/hizb/rub into one UNION ALL: lines(1) + words(1) + surahNames(1) +
        // juz+hizb+rub(1) + sajda(1) = 5.
        interceptor.CommandCount.Should().Be(5);

        response.Should().NotBeNull();
        response!.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetPage_unknown_page_still_short_circuits_after_one_command()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfMushafPageReader(dbContext);

        var response = await reader.GetPageAsync(999, CancellationToken.None);

        interceptor.CommandCount.Should().Be(1, "the line read alone establishes that the page does not exist");
        response.Should().BeNull();
    }

    [Fact]
    public async Task GetPage_markers_preserve_type_order_and_number_after_combining_juz_hizb_rub()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfMushafPageReader(dbContext);

        // Page 1 carries juz #1, hizb #1, and rub #1, all anchored on ayah 1:1 (line 1, word
        // 1:1:1) in the fixture, with no sajda on this page.
        var response = await reader.GetPageAsync(1, CancellationToken.None);

        response.Should().NotBeNull();
        response!.Markers.Should().HaveCount(3);
        response.Markers.Should().BeInAscendingOrder(m => m.LineNumber)
            .And.SatisfyRespectively(
                hizb =>
                {
                    hizb.MarkerType.Should().Be("hizb");
                    hizb.MarkerNumber.Should().Be(1);
                },
                juz =>
                {
                    juz.MarkerType.Should().Be("juz");
                    juz.MarkerNumber.Should().Be(1);
                },
                rub =>
                {
                    rub.MarkerType.Should().Be("rub");
                    rub.MarkerNumber.Should().Be(1);
                });

        foreach (var marker in response.Markers)
        {
            marker.VerseKey.Should().Be("1:1");
            marker.LineNumber.Should().Be(1);
            marker.WordLocation.Should().Be("1:1:1");
            marker.SajdahType.Should().BeNull("no sajda marker exists on this page, and juz/hizb/rub never carry a sajda type");
        }
    }

    [Fact]
    public async Task GetPage_rub_marker_on_a_different_ayah_from_juz_and_hizb_is_still_placed_correctly()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var dbContext = new QuranDashboardDbContext(options);
        var reader = new EfMushafPageReader(dbContext);

        // Page 5 carries only rub #2 (anchored on ayah 2:26), while juz #1, hizb #1, and rub #1
        // are anchored on ayah 1:1 (page 1) and do not appear here — this proves the combined
        // UNION ALL still scopes each marker table to the requested page's ayahs independently.
        var response = await reader.GetPageAsync(5, CancellationToken.None);

        response.Should().NotBeNull();
        response!.Markers.Should().ContainSingle();
        var marker = response.Markers[0];
        marker.MarkerType.Should().Be("rub");
        marker.MarkerNumber.Should().Be(2);
        marker.VerseKey.Should().Be("2:26");
        marker.LineNumber.Should().Be(2);
        marker.WordLocation.Should().Be("2:26:1");
        marker.SajdahType.Should().BeNull();
    }
}
