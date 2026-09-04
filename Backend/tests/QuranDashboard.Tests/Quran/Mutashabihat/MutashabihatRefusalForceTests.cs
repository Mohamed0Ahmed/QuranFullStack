using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Application.Quran.DataPipelines.Mutashabihat;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

[Collection(nameof(MutashabihatRebuildTestCollection))]
public sealed class MutashabihatRefusalForceTests(MutashabihatRebuildTestFixture fixture)
{
    private static readonly MutashabihatExpectedCounts SyntheticExpected = new(1, 2, 2, 1, 1, 2);

    [Fact]
    public async Task Second_Run_Without_Force_Refuses_And_Writes_Nothing()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var first = await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);
        first.Succeeded.Should().BeTrue();
        first.ExitCode.Should().Be(ImportMutashabihatResult.SuccessExitCode);

        var snapshotBeforeSecondRun = await fixture.CaptureTableSnapshotAsync();

        var second = await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);
        second.Succeeded.Should().BeFalse();
        second.ExitCode.Should().Be(ImportMutashabihatResult.RefusedExitCode);
        second.Message.Should().Be(MutashabihatInvariants.TargetsNotEmpty);

        var snapshotAfterSecondRun = await fixture.CaptureTableSnapshotAsync();
        snapshotAfterSecondRun.GroupRows.Should().Be(snapshotBeforeSecondRun.GroupRows);
        snapshotAfterSecondRun.OccurrenceRows.Should().Be(snapshotBeforeSecondRun.OccurrenceRows);
        snapshotAfterSecondRun.LinkRows.Should().Be(snapshotBeforeSecondRun.LinkRows);
        snapshotAfterSecondRun.GroupContentHash.Should().Be(snapshotBeforeSecondRun.GroupContentHash);
        snapshotAfterSecondRun.OccurrenceContentHash.Should().Be(snapshotBeforeSecondRun.OccurrenceContentHash);
        snapshotAfterSecondRun.LinkContentHash.Should().Be(snapshotBeforeSecondRun.LinkContentHash);
    }

    [Fact]
    public async Task Missing_Foundation_Refuses_Cleanly_No_Report()
    {
        await fixture.SeedEmptyFoundationAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var result = await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);
        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(ImportMutashabihatResult.RefusedExitCode);
        result.Message.Should().Be(MutashabihatInvariants.AyahsMissing);
    }

    [Fact]
    public async Task Force_Truncates_And_Rebuilds_Only_Mutashabihat_Tables()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var first = await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);
        first.Succeeded.Should().BeTrue();

        var snapshotBefore = await fixture.CaptureTableSnapshotAsync();

        var forced = await fixture.RunImportAsync(sourcePath, force: true, expectedCounts: SyntheticExpected);
        forced.Succeeded.Should().BeTrue();
        forced.ExitCode.Should().Be(ImportMutashabihatResult.SuccessExitCode);

        var snapshotAfter = await fixture.CaptureTableSnapshotAsync();
        snapshotAfter.AyahRows.Should().Be(snapshotBefore.AyahRows);
        snapshotAfter.WordRows.Should().Be(snapshotBefore.WordRows);
        snapshotAfter.AyahContentHash.Should().Be(snapshotBefore.AyahContentHash);
        snapshotAfter.WordContentHash.Should().Be(snapshotBefore.WordContentHash);
        snapshotAfter.GroupRows.Should().Be(1);
        snapshotAfter.OccurrenceRows.Should().Be(2);
        snapshotAfter.LinkRows.Should().Be(1);
    }

    [Fact]
    public async Task Force_Rerun_Idempotent_Stable_Snapshots()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var first = await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);
        first.Succeeded.Should().BeTrue();

        var snapshotBefore = await fixture.CaptureTableSnapshotAsync();

        var forced = await fixture.RunImportAsync(sourcePath, force: true, expectedCounts: SyntheticExpected);
        forced.Succeeded.Should().BeTrue();

        var snapshotAfter = await fixture.CaptureTableSnapshotAsync();

        snapshotAfter.GroupContentHash.Should().Be(snapshotBefore.GroupContentHash);
        snapshotAfter.OccurrenceContentHash.Should().Be(snapshotBefore.OccurrenceContentHash);
        snapshotAfter.LinkContentHash.Should().Be(snapshotBefore.LinkContentHash);
        snapshotAfter.AyahRows.Should().Be(snapshotBefore.AyahRows);
        snapshotAfter.WordRows.Should().Be(snapshotBefore.WordRows);
    }

    [Fact]
    public async Task Quran_Ayahs_And_Words_Unchanged_After_Run()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var snapshotBefore = await fixture.CaptureTableSnapshotAsync();

        await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);

        var snapshotAfter = await fixture.CaptureTableSnapshotAsync();
        snapshotAfter.AyahRows.Should().Be(snapshotBefore.AyahRows);
        snapshotAfter.WordRows.Should().Be(snapshotBefore.WordRows);
        snapshotAfter.AyahContentHash.Should().Be(snapshotBefore.AyahContentHash);
        snapshotAfter.WordContentHash.Should().Be(snapshotBefore.WordContentHash);
    }

    [Fact]
    public async Task Source_Files_Unchanged_After_Run()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var digestsBefore = await fixture.CaptureSourceDigestsAsync(sourcePath);

        await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);

        var unchanged = await fixture.AreSourceFilesUnchangedAsync(sourcePath, digestsBefore);
        unchanged.Should().BeTrue();
    }

    [Fact]
    public async Task Source_Mismatch_Refuses_Early_No_Report()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSourceFolderWithChecksumMismatchAsync();

        var result = await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);
        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(ImportMutashabihatResult.RefusedExitCode);
        result.Message.Should().Be(MutashabihatInvariants.SourceMismatch);
    }

    [Fact]
    public async Task Populated_Targets_With_Bad_Source_Reports_Source_Mismatch_Not_Targets_Not_Empty()
    {
        await fixture.SeedSyntheticWordsAsync();
        var goodSource = await fixture.WriteSyntheticSourceFolderAsync();

        var first = await fixture.RunImportAsync(goodSource, expectedCounts: SyntheticExpected);
        first.Succeeded.Should().BeTrue();

        var badSource = await fixture.WriteSourceFolderWithChecksumMismatchAsync();

        var second = await fixture.RunImportAsync(badSource, expectedCounts: SyntheticExpected);
        second.Succeeded.Should().BeFalse();
        second.ExitCode.Should().Be(ImportMutashabihatResult.RefusedExitCode);
        second.Message.Should().Be(MutashabihatInvariants.SourceMismatch);
    }
}
