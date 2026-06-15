using QuranDashboard.Application.Abstractions.Quran.Tafsirs;
using QuranDashboard.Infrastructure.Files.Quran.Tafsirs;

namespace QuranDashboard.Tests.Quran.Tafsirs;

[Collection(nameof(TafsirImportTestCollection))]
public sealed class TafsirForceRebuildTests(TafsirImportTestFixture fixture)
{
    [Fact]
    public async Task Force_rebuild_replaces_tafsir_owned_tables_and_preserves_quran_foundation()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var initialPackageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.IntegrationSources);
        var reportDir = Path.Combine(Path.GetTempPath(), $"tafsir-report-{Guid.NewGuid():N}");

        var firstRun = await fixture.RunImportAsync(
            initialPackageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts,
            reportDir);

        firstRun.Succeeded.Should().BeTrue(firstRun.Message);

        var foundationBeforeForce = await fixture.CaptureQuranFoundationSnapshotAsync();
        var tafsirBeforeForce = await CaptureTafsirContentFingerprintAsync(fixture);

        IReadOnlyList<SyntheticTafsirSourceSpec> replacementSources =
        [
            TafsirSyntheticSeed.IntegrationSources[0] with
            {
                Entries = new Dictionary<string, object>
                {
                    ["900:1"] = new
                    {
                        text = "<p>نص تفسير اختباري مُستبدَل بعد إعادة البناء بالقوة.</p>",
                        ayah_keys = new[] { "900:1", "900:2" }
                    },
                    ["900:2"] = "900:1",
                    ["900:3"] = new { text = "<p>نص تفسير اختباري مُستبدَل للمفتاح 900:3.</p>" }
                }
            }
        ];

        var replacementPackageDir = await fixture.WriteSyntheticPackageAsync(sources: replacementSources);

        var forceRun = await fixture.RunImportAsync(
            replacementPackageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts,
            reportDir,
            force: true);

        forceRun.Succeeded.Should().BeTrue(forceRun.Message);

        var foundationAfterForce = await fixture.CaptureQuranFoundationSnapshotAsync();
        foundationAfterForce.Should().Be(foundationBeforeForce);

        var tafsirAfterForce = await CaptureTafsirContentFingerprintAsync(fixture);
        tafsirAfterForce.Should().NotBe(tafsirBeforeForce);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var leaderEntry = await dbContext.TafsirEntries
            .AsNoTracking()
            .SingleAsync(entry => entry.SourceEntryKey == "900:1");

        leaderEntry.TafsirText.Should().Be("<p>نص تفسير اختباري مُستبدَل بعد إعادة البناء بالقوة.</p>");
        leaderEntry.TextHash.Should().Be(TafsirAssembler.ComputeTextHash(leaderEntry.TafsirText));

        var snapshot = await fixture.CaptureTafsirTableSnapshotAsync();
        snapshot.SourceRows.Should().Be(1);
        snapshot.EntryRows.Should().Be(2);
        snapshot.AyahLinkRows.Should().Be(3);
    }

    [Fact]
    public async Task Force_rebuild_clears_only_tafsir_tables_not_quran_foundation()
    {
        await fixture.TruncateTafsirTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(TafsirSyntheticSeed.DefaultAyahs);

        var packageDir = await fixture.WriteSyntheticPackageAsync(
            sources: TafsirSyntheticSeed.IntegrationSources);

        var firstRun = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts);

        firstRun.Succeeded.Should().BeTrue(firstRun.Message);

        var foundationBefore = await fixture.CaptureQuranFoundationSnapshotAsync();
        var ayahCountBefore = foundationBefore.AyahRows;

        var forceRun = await fixture.RunImportAsync(
            packageDir,
            TafsirSyntheticSeed.DefaultTestExpectedCounts,
            force: true);

        forceRun.Succeeded.Should().BeTrue(forceRun.Message);

        var foundationAfter = await fixture.CaptureQuranFoundationSnapshotAsync();
        foundationAfter.Should().Be(foundationBefore);
        foundationAfter.AyahRows.Should().Be(ayahCountBefore);
    }

    private static async Task<string> CaptureTafsirContentFingerprintAsync(TafsirImportTestFixture fixture)
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var rows = await dbContext.TafsirEntries
            .AsNoTracking()
            .Join(
                dbContext.TafsirSources.AsNoTracking(),
                entry => entry.SourceId,
                source => source.Id,
                (entry, source) => new { source.SourceKey, entry.SourceEntryKey, entry.TafsirText, entry.TextHash })
            .OrderBy(row => row.SourceKey)
            .ThenBy(row => row.SourceEntryKey)
            .ToListAsync();

        return string.Join(
            '|',
            rows.Select(row => $"{row.SourceKey}:{row.SourceEntryKey}:{row.TextHash}:{row.TafsirText}"));
    }
}
