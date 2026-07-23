using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Protection;

[Collection(nameof(AbwabDbCollection))]
public sealed class FullPresetTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly IReadOnlyList<ManualProtectionType> AllTypes = Enum.GetValues<ManualProtectionType>();

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Apply_WithNoPreExistingTypes_InsertsAllFive()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار الحماية الكاملة ١", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyFullProtectionPresetAsync(
            new ApplyFullProtectionPresetCommand(categoryId, ManualProtectionScope.Subtree, new Dictionary<ManualProtectionType, uint>(), ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var records = await db.AbwabManualProtections.AsNoTracking().Where(p => p.CategoryId == categoryId && !p.IsDeleted).ToListAsync();
        records.Should().HaveCount(5);
        records.Select(r => r.ProtectionType).Should().BeEquivalentTo(AllTypes);
        records.Should().OnlyContain(r => r.ProtectionScope == ManualProtectionScope.Subtree);
    }

    [Fact]
    public async Task Apply_WithSomePreExistingMixedScopes_UpsertsOnlyTheDifferentOnes()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار الحماية الكاملة ٢", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.QuranContent, ManualProtectionScope.Subtree, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var deletionRecord = await db.AbwabManualProtections.AsNoTracking().SingleAsync(p => p.CategoryId == categoryId && p.ProtectionType == ManualProtectionType.Deletion);
        var quranContentRecord = await db.AbwabManualProtections.AsNoTracking().SingleAsync(p => p.CategoryId == categoryId && p.ProtectionType == ManualProtectionType.QuranContent);

        var expectedVersions = new Dictionary<ManualProtectionType, uint>
        {
            [ManualProtectionType.Deletion] = deletionRecord.Version,
        };

        await writePort.ApplyFullProtectionPresetAsync(
            new ApplyFullProtectionPresetCommand(categoryId, ManualProtectionScope.Subtree, expectedVersions, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var records = await db.AbwabManualProtections.AsNoTracking().Where(p => p.CategoryId == categoryId && !p.IsDeleted).ToListAsync();
        records.Should().HaveCount(5);
        records.Should().OnlyContain(r => r.ProtectionScope == ManualProtectionScope.Subtree);

        var reloadedQuranContent = records.Single(r => r.ProtectionType == ManualProtectionType.QuranContent);
        reloadedQuranContent.Version.Should().Be(quranContentRecord.Version, "an already-matching scope record is left untouched");
    }

    [Fact]
    public async Task Apply_OneSelectedScope_AppliedToAllFive_RequiresExpectedVersionForEveryChangedScope()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار الحماية الكاملة ٣", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyFullProtectionPresetAsync(
            new ApplyFullProtectionPresetCommand(categoryId, ManualProtectionScope.CategoryOnly, new Dictionary<ManualProtectionType, uint>(), ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var act = () => writePort.ApplyFullProtectionPresetAsync(
            new ApplyFullProtectionPresetCommand(categoryId, ManualProtectionScope.Subtree, new Dictionary<ManualProtectionType, uint>(), ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtectionScopeConflict);
    }

    [Fact]
    public async Task Apply_WhenAllFiveAlreadyMatch_IsIdempotent_WithNoChangeSet()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار الحماية الكاملة ٤", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.ApplyFullProtectionPresetAsync(
            new ApplyFullProtectionPresetCommand(categoryId, ManualProtectionScope.Subtree, new Dictionary<ManualProtectionType, uint>(), ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();

        await writePort.ApplyFullProtectionPresetAsync(
            new ApplyFullProtectionPresetCommand(categoryId, ManualProtectionScope.Subtree, new Dictionary<ManualProtectionType, uint>(), ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var changeSetsAfter = await db.AbwabChangeSets.AsNoTracking().CountAsync();
        changeSetsAfter.Should().Be(changeSetsBefore, "an all-matching preset apply is an idempotent no-op");
    }

    [Fact]
    public async Task EachType_MayLaterBeLiftedIndependently()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار الحماية الكاملة ٥", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.ApplyFullProtectionPresetAsync(
            new ApplyFullProtectionPresetCommand(categoryId, ManualProtectionScope.Subtree, new Dictionary<ManualProtectionType, uint>(), ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var deletionRecord = await db.AbwabManualProtections.AsNoTracking().SingleAsync(p => p.CategoryId == categoryId && p.ProtectionType == ManualProtectionType.Deletion);

        await writePort.LiftManualProtectionAsync(
            new LiftManualProtectionCommand(categoryId, ManualProtectionType.Deletion, deletionRecord.Version, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var activeRecords = await db.AbwabManualProtections.AsNoTracking().Where(p => p.CategoryId == categoryId && !p.IsDeleted).ToListAsync();
        activeRecords.Should().HaveCount(4);
        activeRecords.Should().NotContain(r => r.ProtectionType == ManualProtectionType.Deletion);
    }

    [Fact]
    public async Task AConcurrentStaleScopeEdit_RollsBackTheEntireFiveTypeCommand()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار التراجع الكامل", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Relationship, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var relationshipBefore = await db.AbwabManualProtections.AsNoTracking().SingleAsync(p => p.CategoryId == categoryId && p.ProtectionType == ManualProtectionType.Relationship);

        // A concurrent scope change bumps Relationship's xmin before the preset command runs.
        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Relationship, ManualProtectionScope.Subtree, relationshipBefore.Version, ExpectedTimelineGeneration.Of(0), "someone-else"),
            CancellationToken.None);

        var act = () => writePort.ApplyFullProtectionPresetAsync(
            new ApplyFullProtectionPresetCommand(
                categoryId,
                ManualProtectionScope.CategoryOnly,
                new Dictionary<ManualProtectionType, uint> { [ManualProtectionType.Relationship] = relationshipBefore.Version },
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtectionScopeConflict);

        var recordsAfterRollback = await db.AbwabManualProtections.AsNoTracking().Where(p => p.CategoryId == categoryId && !p.IsDeleted).ToListAsync();
        recordsAfterRollback.Should().ContainSingle("only the pre-existing Relationship record survives — the other four inserts must roll back too");
        recordsAfterRollback[0].ProtectionType.Should().Be(ManualProtectionType.Relationship);
        recordsAfterRollback[0].ProtectionScope.Should().Be(ManualProtectionScope.Subtree, "the concurrent edit that caused the conflict is the one that committed");
    }
}
