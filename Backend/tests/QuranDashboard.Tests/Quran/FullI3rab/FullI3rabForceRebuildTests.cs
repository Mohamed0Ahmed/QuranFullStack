using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Tests.Quran.FullI3rab;

[Collection(nameof(FullI3rabImportTestCollection))]
public sealed class FullI3rabForceRebuildTests(FullI3rabImportTestFixture fixture) : IDisposable
{
    private readonly FullI3rabSyntheticPackage packages = new();

    [Fact]
    public async Task Force_rebuild_replaces_full_i3rab_owned_tables_and_preserves_quran_foundation()
    {
        await fixture.TruncateFullI3rabTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(FullI3rabSyntheticSeed.DefaultAyahs);

        var initialPackageDir = await packages.WriteAsync();
        var reportDir = fixture.CreateTempDir();

        var firstRun = await fixture.RunImportAsync(
            initialPackageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3),
            reportDir);

        firstRun.Succeeded.Should().BeTrue(firstRun.Message);

        var foundationBeforeForce = await fixture.CaptureQuranFoundationSnapshotAsync();
        var contentBeforeForce = await CaptureFullI3rabContentFingerprintAsync(fixture);

        IReadOnlyList<SyntheticFullI3rabSourceSpec> replacementSources =
        [
            FullI3rabSyntheticSeed.DefaultSources[0] with
            {
                Entries = new Dictionary<string, object>
                {
                    ["900:1"] = new
                    {
                        text = "<div class=\"ar\"><p>إعراب اختباري مُستبدَل بعد إعادة البناء بالقوة.</p></div>",
                        ayah_keys = new[] { "900:1", "900:2" }
                    },
                    ["900:2"] = "900:1",
                    ["900:3"] = new
                    {
                        text = "<div class=\"ar\"><p>إعراب اختباري مُستبدَل للمفتاح 900:3.</p></div>"
                    }
                }
            }
        ];

        var replacementPackageDir = await packages.WriteAsync(sources: replacementSources);

        var forceRun = await fixture.RunImportAsync(
            replacementPackageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3),
            reportDir,
            force: true);

        forceRun.Succeeded.Should().BeTrue(forceRun.Message);

        var foundationAfterForce = await fixture.CaptureQuranFoundationSnapshotAsync();
        foundationAfterForce.Should().Be(foundationBeforeForce);

        var contentAfterForce = await CaptureFullI3rabContentFingerprintAsync(fixture);
        contentAfterForce.Should().NotBe(contentBeforeForce);

        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var leaderEntry = await dbContext.FullI3rabEntries
            .AsNoTracking()
            .SingleAsync(entry => entry.SourceEntryKey == "900:1");

        leaderEntry.I3rabHtml.Should().Be(
            "<div class=\"ar\"><p>إعراب اختباري مُستبدَل بعد إعادة البناء بالقوة.</p></div>");
        leaderEntry.TextHash.Should().Be(FullI3rabAssembler.ComputeTextHash(leaderEntry.I3rabHtml));

        var snapshot = await fixture.CaptureFullI3rabTableSnapshotAsync();
        snapshot.SourceRows.Should().Be(1);
        snapshot.EntryRows.Should().Be(2);
        snapshot.AyahLinkRows.Should().Be(3);
    }

    [Fact]
    public async Task Force_rebuild_clears_only_full_i3rab_tables_not_quran_foundation()
    {
        await fixture.TruncateFullI3rabTablesAsync();
        await fixture.SeedSyntheticAyahsAsync(FullI3rabSyntheticSeed.DefaultAyahs);

        var packageDir = await packages.WriteAsync();

        var firstRun = await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3));

        firstRun.Succeeded.Should().BeTrue(firstRun.Message);

        var foundationBefore = await fixture.CaptureQuranFoundationSnapshotAsync();
        var ayahCountBefore = foundationBefore.AyahRows;

        var forceRun = await fixture.RunImportAsync(
            packageDir,
            new FullI3rabExpectedCounts(Sources: 1, AyahsPerSource: 3),
            force: true);

        forceRun.Succeeded.Should().BeTrue(forceRun.Message);

        var foundationAfter = await fixture.CaptureQuranFoundationSnapshotAsync();
        foundationAfter.Should().Be(foundationBefore);
        foundationAfter.AyahRows.Should().Be(ayahCountBefore);
    }

    public void Dispose() => packages.Dispose();

    private static async Task<string> CaptureFullI3rabContentFingerprintAsync(FullI3rabImportTestFixture fixture)
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        var rows = await dbContext.FullI3rabEntries
            .AsNoTracking()
            .Join(
                dbContext.FullI3rabSources.AsNoTracking(),
                entry => entry.SourceId,
                source => source.Id,
                (entry, source) => new { source.SourceKey, entry.SourceEntryKey, entry.I3rabHtml, entry.TextHash })
            .OrderBy(row => row.SourceKey)
            .ThenBy(row => row.SourceEntryKey)
            .ToListAsync();

        return string.Join(
            '|',
            rows.Select(row => $"{row.SourceKey}:{row.SourceEntryKey}:{row.TextHash}:{row.I3rabHtml}"));
    }
}
