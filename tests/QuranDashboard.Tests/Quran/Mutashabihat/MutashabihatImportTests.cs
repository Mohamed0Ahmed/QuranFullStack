using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;
using QuranDashboard.Application.Quran.Mutashabihat.ImportMutashabihat;
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

        await using var scope = fixture.CreateServiceProvider(services => services.AddMutashabihatImportServices())
            .CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var groups = await dbContext.MutashabihatGroups.AsNoTracking().ToListAsync();
        var occurrences = await dbContext.MutashabihatOccurrences.AsNoTracking().ToListAsync();

        groups.Should().HaveCount(1);
        groups.Single().OccurrenceCount.Should().Be(2);
        groups.Single().DistinctAyahCount.Should().Be(2);
        occurrences.Should().HaveCount(2);
        occurrences.Should().OnlyContain(occurrence => occurrence.AyahId == 1 || occurrence.AyahId == 2);
        occurrences.Count(occurrence => occurrence.IsRepresentative).Should().Be(1);
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

        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync(phrases: phrases);
        var expected = new MutashabihatExpectedCounts(1, 3, 2, 0, 0, 2);

        var result = await fixture.RunImportAsync(sourcePath, expectedCounts: expected);

        result.ExitCode.Should().Be(ImportMutashabihatResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider(services => services.AddMutashabihatImportServices())
            .CreateAsyncScope();
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

        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync(phrases: phrases);
        var expected = new MutashabihatExpectedCounts(1, 2, 2, 0, 0, 3);

        var result = await fixture.RunImportAsync(sourcePath, expectedCounts: expected);

        result.ExitCode.Should().Be(ImportMutashabihatResult.SuccessExitCode);

        await using var scope = fixture.CreateServiceProvider(services => services.AddMutashabihatImportServices())
            .CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var group = await dbContext.MutashabihatGroups.AsNoTracking().SingleAsync();
        group.SourceGroupId.Should().Be(1782);
        group.RepresentativeAyahId.Should().Be(99);
        group.RepresentativeWordFrom.Should().Be(1);
        group.RepresentativeWordTo.Should().Be(1);

        var occurrences = await dbContext.MutashabihatOccurrences.AsNoTracking().ToListAsync();
        occurrences.Should().OnlyContain(occurrence => !occurrence.IsRepresentative);
    }
}

[CollectionDefinition(nameof(MutashabihatImportTestCollection))]
public sealed class MutashabihatImportTestCollection : ICollectionFixture<MutashabihatImportTestFixture>;
