using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirExcludedSourceTests(TafsirImportTestFixture fixture)
{
    public static IEnumerable<object[]> LockedExcludedSourceKeys() =>
        TafsirInvariants.LockedExcludedSourceKeys.Select(key => new object[] { key });

    [Theory]
    [MemberData(nameof(LockedExcludedSourceKeys))]
    public async Task Import_refuses_locked_excluded_source_key_in_approved_set(string excludedKey)
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var sources = new List<SyntheticTafsirSourceSpec>
        {
            TafsirSyntheticSeed.IntegrationSources[0] with { SourceKey = excludedKey }
        };

        var packageDir = await fixture.WriteSyntheticPackageAsync(sources: sources);
        var before = await fixture.CaptureTafsirTableSnapshotAsync();

        var result = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain(TafsirInvariants.CheckNoExcludedSources);

        var after = await fixture.CaptureTafsirTableSnapshotAsync();
        after.Should().Be(before);
    }
}
