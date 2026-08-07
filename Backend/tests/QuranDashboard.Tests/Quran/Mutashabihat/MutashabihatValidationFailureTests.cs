using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Application.Quran.DataPipelines.Mutashabihat;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Mutashabihat;

namespace QuranDashboard.Tests.Quran.Mutashabihat;

[Collection(nameof(MutashabihatImportTestCollection))]
public sealed class MutashabihatValidationFailureTests(MutashabihatImportTestFixture fixture)
{
    private const string EmptyTableContentHash = "empty";
    private static readonly MutashabihatExpectedCounts SyntheticExpected = new(1, 2, 2, 1, 1, 2);

    [Fact]
    public async Task Validation_Violation_On_Empty_Target_Rolls_Back_All_Three_Tables()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSourceFolderWithValidationViolationAsync();

        var handlerResult = await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);
        handlerResult.Succeeded.Should().BeFalse();
        handlerResult.ExitCode.Should().Be(ImportMutashabihatResult.FailureExitCode);
        handlerResult.Message.Should().Contain("MUT-SCORE-RANGE");
        handlerResult.ReportOutDir.Should().NotBeNullOrWhiteSpace();
        File.Exists(MutashabihatImportTestFixture.GetJsonReportPath(handlerResult.ReportOutDir!)).Should().BeTrue();

        var importResult = await fixture.RunImportWriterAsync(sourcePath, expectedCounts: SyntheticExpected);

        importResult.Verdict.Should().Be("fail");
        importResult.Persisted.Should().BeFalse();
        importResult.Checks.Should().Contain(check =>
            check.Id == "MUT-SCORE-RANGE" && !check.Passed);

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.GroupRows.Should().Be(0);
        snapshot.OccurrenceRows.Should().Be(0);
        snapshot.LinkRows.Should().Be(0);
        snapshot.GroupContentHash.Should().Be(EmptyTableContentHash);
        snapshot.OccurrenceContentHash.Should().Be(EmptyTableContentHash);
        snapshot.LinkContentHash.Should().Be(EmptyTableContentHash);
    }

    [Fact]
    public async Task Self_Link_Violation_On_Empty_Target_Returns_Controlled_Failure_Through_Handler()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSourceFolderWithSelfLinkViolationAsync();

        var handlerResult = await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);
        handlerResult.Succeeded.Should().BeFalse();
        handlerResult.ExitCode.Should().Be(ImportMutashabihatResult.FailureExitCode);
        handlerResult.Message.Should().Contain(MutashabihatInvariants.CheckLinkNoSelf);

        var importResult = await fixture.RunImportWriterAsync(sourcePath, expectedCounts: SyntheticExpected);
        importResult.Verdict.Should().Be("fail");
        importResult.Persisted.Should().BeFalse();
        importResult.Checks.Should().Contain(check =>
            check.Id == MutashabihatInvariants.CheckLinkNoSelf && !check.Passed);

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.GroupRows.Should().Be(0);
        snapshot.OccurrenceRows.Should().Be(0);
        snapshot.LinkRows.Should().Be(0);
        snapshot.GroupContentHash.Should().Be(EmptyTableContentHash);
        snapshot.OccurrenceContentHash.Should().Be(EmptyTableContentHash);
        snapshot.LinkContentHash.Should().Be(EmptyTableContentHash);
    }

    [Fact]
    public async Task Forced_Run_Over_Populated_Tables_With_Validation_Failure_Preserves_Previous()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var first = await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);
        first.Succeeded.Should().BeTrue();

        var snapshotBefore = await fixture.CaptureTableSnapshotAsync();

        var violationPath = await fixture.WriteSourceFolderWithValidationViolationAsync();

        var forced = await fixture.RunImportAsync(violationPath, force: true, expectedCounts: SyntheticExpected);
        forced.Succeeded.Should().BeFalse();
        forced.ExitCode.Should().Be(ImportMutashabihatResult.FailureExitCode);
        forced.ReportOutDir.Should().NotBeNullOrWhiteSpace();
        File.Exists(MutashabihatImportTestFixture.GetJsonReportPath(forced.ReportOutDir!)).Should().BeTrue();

        var snapshotAfter = await fixture.CaptureTableSnapshotAsync();

        snapshotAfter.GroupRows.Should().Be(snapshotBefore.GroupRows);
        snapshotAfter.OccurrenceRows.Should().Be(snapshotBefore.OccurrenceRows);
        snapshotAfter.LinkRows.Should().Be(snapshotBefore.LinkRows);
        snapshotAfter.AyahRows.Should().Be(snapshotBefore.AyahRows);
        snapshotAfter.WordRows.Should().Be(snapshotBefore.WordRows);
        snapshotAfter.GroupContentHash.Should().Be(snapshotBefore.GroupContentHash);
        snapshotAfter.OccurrenceContentHash.Should().Be(snapshotBefore.OccurrenceContentHash);
        snapshotAfter.LinkContentHash.Should().Be(snapshotBefore.LinkContentHash);
        snapshotAfter.AyahContentHash.Should().Be(snapshotBefore.AyahContentHash);
        snapshotAfter.WordContentHash.Should().Be(snapshotBefore.WordContentHash);
    }

    [Fact]
    public async Task Forced_Run_With_Source_Failure_Leaves_Previous_Unchanged()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        var first = await fixture.RunImportAsync(sourcePath, expectedCounts: SyntheticExpected);
        first.Succeeded.Should().BeTrue();

        var snapshotBefore = await fixture.CaptureTableSnapshotAsync();

        var badSource = await fixture.WriteSourceFolderWithChecksumMismatchAsync();

        var forced = await fixture.RunImportAsync(badSource, force: true, expectedCounts: SyntheticExpected);
        forced.Succeeded.Should().BeFalse();
        forced.ExitCode.Should().Be(ImportMutashabihatResult.RefusedExitCode);

        var snapshotAfter = await fixture.CaptureTableSnapshotAsync();

        snapshotAfter.GroupRows.Should().Be(snapshotBefore.GroupRows);
        snapshotAfter.OccurrenceRows.Should().Be(snapshotBefore.OccurrenceRows);
        snapshotAfter.LinkRows.Should().Be(snapshotBefore.LinkRows);
        snapshotAfter.AyahRows.Should().Be(snapshotBefore.AyahRows);
        snapshotAfter.WordRows.Should().Be(snapshotBefore.WordRows);
        snapshotAfter.GroupContentHash.Should().Be(snapshotBefore.GroupContentHash);
        snapshotAfter.OccurrenceContentHash.Should().Be(snapshotBefore.OccurrenceContentHash);
        snapshotAfter.LinkContentHash.Should().Be(snapshotBefore.LinkContentHash);
    }

    [Fact]
    public async Task Source_Changed_After_Load_Fails_With_No_Persisted_State()
    {
        await fixture.SeedSyntheticWordsAsync();
        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();

        await using var provider = fixture.CreateCallerDisposedServiceProvider(services =>
        {
            services.AddMutashabihatImportServices();
            services.AddScoped<SourceChangedAfterLoadMutashabihatImportSource>();
            services.AddScoped<IMutashabihatImportSource>(sp =>
                sp.GetRequiredService<SourceChangedAfterLoadMutashabihatImportSource>());
        });
        await using var scope = provider.CreateAsyncScope();

        var reportOutDir = Path.Combine(Path.GetTempPath(), $"mutashabihat-report-{Guid.NewGuid():N}");
        var handler = scope.ServiceProvider.GetRequiredService<ImportMutashabihatHandler>();
        var result = await handler.HandleAsync(
            new ImportMutashabihatCommand(sourcePath, false, SyntheticExpected, reportOutDir),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ExitCode.Should().Be(ImportMutashabihatResult.FailureExitCode);
        result.Message.Should().Contain(MutashabihatInvariants.CheckSourceUnchanged);

        var snapshot = await fixture.CaptureTableSnapshotAsync();
        snapshot.GroupRows.Should().Be(0);
        snapshot.OccurrenceRows.Should().Be(0);
        snapshot.LinkRows.Should().Be(0);
    }
}

internal sealed class SourceChangedAfterLoadMutashabihatImportSource : IMutashabihatImportSource
{
    private readonly MutashabihatImportSource inner;

    public SourceChangedAfterLoadMutashabihatImportSource(MutashabihatImportSource inner)
    {
        this.inner = inner;
    }

    public Task<MutashabihatSourceData> LoadAsync(string sourcePath, CancellationToken ct) =>
        inner.LoadAsync(sourcePath, ct);

    public Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct) =>
        Task.FromResult(false);
}
