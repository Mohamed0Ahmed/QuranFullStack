using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Application.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Domain.Quran.Mutashabihat;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

[Collection(nameof(MutashabihatImportTestCollection))]
public sealed class MutashabihatImportTests(MutashabihatImportTestFixture fixture)
{
    [Fact]
    public async Task Import_produces_expected_groups_and_occurrences_for_synthetic_fixture()
    {
        await fixture.SeedSyntheticAyahsAsync((1, "900:1"), (2, "900:2"));
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var expected = new MutashabihatExpectedCounts(
            Groups: 1,
            RawOccurrences: 2,
            StoredOccurrences: 2,
            SimilarSources: 1,
            SimilarLinks: 1,
            DistinctAyahs: 2);

        var result = await fixture.RunImportAsync(sourcePath, expectedCounts: expected);

        result.ExitCode.Should().Be(ImportMutashabihatResult.SuccessExitCode);
        result.Totals.Should().NotBeNull();
        result.Totals!.GroupRows.Should().Be(1);
        result.Totals.StoredOccurrenceRows.Should().Be(2);
        result.Totals.RawOccurrenceEntries.Should().Be(2);
        result.Totals.LinkRows.Should().Be(1);
        result.Totals.DistinctSimilarSources.Should().Be(1);

        await using var scope = fixture.CreateImportScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var groups = await dbContext.MutashabihatGroups.AsNoTracking().ToListAsync();
        var occurrences = await dbContext.MutashabihatOccurrences.AsNoTracking().ToListAsync();
        var links = await dbContext.SimilarAyahLinks.AsNoTracking().ToListAsync();

        groups.Should().HaveCount(1);
        groups.Single().OccurrenceCount.Should().Be(2);
        groups.Single().DistinctAyahCount.Should().Be(2);
        occurrences.Should().HaveCount(2);
        occurrences.Should().OnlyContain(occurrence => occurrence.AyahId == 1 || occurrence.AyahId == 2);
        occurrences.Count(occurrence => occurrence.IsRepresentative).Should().Be(1);

        links.Should().ContainSingle();
        links.Single().SourceAyahId.Should().Be(1);
        links.Single().TargetAyahId.Should().Be(2);
        links.Single().Score.Should().Be(80);
        links.Single().Coverage.Should().Be(50);
        links.Single().MatchedWordsCount.Should().Be(2);
        links.Single().MatchWords.Should().Be("""[[1, 2]]""");
    }

    [Fact]
    public async Task Import_collapses_duplicate_occurrence_and_recomputes_counters()
    {
        await fixture.SeedSyntheticAyahsAsync((1, "900:1"), (2, "900:2"));
        var phrases = new Dictionary<string, object>
        {
            ["75"] = new
            {
                surahs = 9,
                ayahs = 99,
                count = 999,
                source = new { key = "900:1", from = 17, to = 19 },
                ayah = new Dictionary<string, object[]>
                {
                    ["900:1"] = [new[] { 17, 19 }, new[] { 17, 19 }],
                    ["900:2"] = [new[] { 17, 19 }]
                }
            }
        };

        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync(
            phrases: phrases,
            similarAyahs: new Dictionary<string, object>());
        var expected = new MutashabihatExpectedCounts(1, 3, 2, 0, 0, 2);

        var result = await fixture.RunImportAsync(sourcePath, expectedCounts: expected);

        result.ExitCode.Should().Be(ImportMutashabihatResult.SuccessExitCode);

        await using var scope = fixture.CreateImportScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var group = await dbContext.MutashabihatGroups.AsNoTracking().SingleAsync();
        group.OccurrenceCount.Should().Be(2);
        group.DistinctAyahCount.Should().Be(2);
        group.RawSourceCounts.Should().Contain("999");

        var occurrences = await dbContext.MutashabihatOccurrences.AsNoTracking().ToListAsync();
        occurrences.Should().HaveCount(2);
    }

    [Fact]
    public async Task Import_keeps_source_key_absent_group_with_zero_representative_occurrences()
    {
        await fixture.SeedSyntheticAyahsAsync((1, "900:1"), (2, "900:2"), (99, "900:99"));
        var phrases = new Dictionary<string, object>
        {
            ["1782"] = new
            {
                source = new { key = "900:99", from = 1, to = 1 },
                ayah = new Dictionary<string, object[]>
                {
                    ["900:1"] = [new[] { 1, 2 }],
                    ["900:2"] = [new[] { 1, 2 }]
                }
            }
        };

        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync(
            phrases: phrases,
            similarAyahs: new Dictionary<string, object>());
        var expected = new MutashabihatExpectedCounts(1, 2, 2, 0, 0, 3);

        var result = await fixture.RunImportAsync(sourcePath, expectedCounts: expected);

        result.ExitCode.Should().Be(ImportMutashabihatResult.SuccessExitCode);

        await using var scope = fixture.CreateImportScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var group = await dbContext.MutashabihatGroups.AsNoTracking().SingleAsync();
        group.SourceGroupId.Should().Be(1782);
        group.RepresentativeAyahId.Should().Be(99);
        group.RepresentativeWordFrom.Should().Be(1);
        group.RepresentativeWordTo.Should().Be(1);

        var occurrences = await dbContext.MutashabihatOccurrences.AsNoTracking().ToListAsync();
        occurrences.Should().OnlyContain(occurrence => !occurrence.IsRepresentative);
    }

    [Fact]
    public async Task Import_stores_similar_links_with_raw_coverage_above_100()
    {
        await fixture.SeedSyntheticAyahsAsync((1, "900:1"), (2, "900:2"));
        var similarAyahs = new Dictionary<string, object>
        {
            ["900:1"] = new object[]
            {
                new
                {
                    matched_ayah_key = "900:2",
                    score = 90,
                    coverage = 200,
                    matched_words_count = 1,
                    match_words = new[] { new[] { 1 } }
                }
            }
        };

        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync(similarAyahs: similarAyahs);
        var expected = new MutashabihatExpectedCounts(1, 2, 2, 1, 1, 2);

        var result = await fixture.RunImportAsync(sourcePath, expectedCounts: expected);

        result.ExitCode.Should().Be(ImportMutashabihatResult.SuccessExitCode);

        await using var scope = fixture.CreateImportScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var link = await dbContext.SimilarAyahLinks.AsNoTracking().SingleAsync();
        link.Coverage.Should().Be(200);
        link.SourceAyahId.Should().Be(1);
        link.TargetAyahId.Should().Be(2);
    }

    [Theory]
    [MemberData(nameof(LinkHardCheckFailureCases))]
    public async Task Import_returns_failure_when_similar_link_hard_check_fails(
        short score,
        short coverage,
        int[][] matchWords,
        string expectedCheckId)
    {
        await fixture.SeedSyntheticAyahsAsync((1, "900:1"), (2, "900:2"));
        var similarAyahs = CreateSingleSimilarLink(score, coverage, matchWords);
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync(similarAyahs: similarAyahs);
        var expected = new MutashabihatExpectedCounts(1, 2, 2, 1, 1, 2);

        var result = await fixture.RunImportAsync(sourcePath, expectedCounts: expected);

        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(ImportMutashabihatResult.FailureExitCode);
        result.Message.Should().Contain(expectedCheckId);

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.GroupRows.Should().Be(0);
        snapshot.OccurrenceRows.Should().Be(0);
        snapshot.LinkRows.Should().Be(0);
    }

    public static IEnumerable<object[]> LinkHardCheckFailureCases()
    {
        yield return
        [
            (short)40,
            (short)50,
            new[] { new[] { 1 } },
            "MUT-SCORE-RANGE"
        ];

        yield return
        [
            (short)90,
            (short)50,
            new[] { new[] { 2, 1 } },
            "MUT-WORD-RANGE-SHAPE"
        ];
    }

    private static Dictionary<string, object> CreateSingleSimilarLink(
        short score,
        short coverage,
        int[][] matchWords) => new()
        {
            ["900:1"] = new object[]
            {
                new
                {
                    matched_ayah_key = "900:2",
                    score,
                    coverage,
                    matched_words_count = 1,
                    match_words = matchWords
                }
            }
        };
}

[CollectionDefinition(nameof(MutashabihatImportTestCollection))]
public sealed class MutashabihatImportTestCollection : ICollectionFixture<MutashabihatImportTestFixture>;
