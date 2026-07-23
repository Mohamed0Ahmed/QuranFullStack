using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Protection;

[Collection(nameof(AbwabDbCollection))]
public sealed class ManualApplyLiftTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Apply_TheSameActiveTypeAndScope_IsIdempotent_WithNoChangeSet()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار التطبيق المكرر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var changeSetsAfter = await db.AbwabChangeSets.AsNoTracking().CountAsync();
        changeSetsAfter.Should().Be(changeSetsBefore, "an idempotent same-scope apply creates no ChangeSet");
    }

    [Fact]
    public async Task Apply_ADifferentScope_WithTheCorrectExpectedVersion_IsOneAuditedReversibleEdit()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار تغيير النطاق", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var record = await db.AbwabManualProtections.AsNoTracking().SingleAsync(p => p.CategoryId == categoryId && p.ProtectionType == ManualProtectionType.Deletion);
        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Deletion, ManualProtectionScope.Subtree, record.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var reloaded = await db.AbwabManualProtections.AsNoTracking().SingleAsync(p => p.CategoryId == categoryId && p.ProtectionType == ManualProtectionType.Deletion);
        reloaded.ProtectionScope.Should().Be(ManualProtectionScope.Subtree);

        var changeSetsAfter = await db.AbwabChangeSets.AsNoTracking().CountAsync();
        changeSetsAfter.Should().Be(changeSetsBefore + 1);
    }

    [Fact]
    public async Task Apply_ADifferentScope_WithAMissingOrStaleExpectedVersion_MapsToManualProtectionScopeConflict()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار تعارض النطاق", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var actMissingVersion = () => writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Deletion, ManualProtectionScope.Subtree, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        (await actMissingVersion.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtectionScopeConflict);

        var actStaleVersion = () => writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Deletion, ManualProtectionScope.Subtree, 999, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        (await actStaleVersion.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtectionScopeConflict);
    }

    [Fact]
    public async Task Lift_AnExistingActiveProtection_SucceedsEvenThoughItIsCurrentlyProtected()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار الرفع", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var record = await db.AbwabManualProtections.AsNoTracking().SingleAsync(p => p.CategoryId == categoryId && p.ProtectionType == ManualProtectionType.Deletion);

        var act = () => writePort.LiftManualProtectionAsync(
            new LiftManualProtectionCommand(categoryId, ManualProtectionType.Deletion, record.Version, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await act.Should().NotThrowAsync();

        var reloaded = await db.AbwabManualProtections.AsNoTracking().SingleAsync(p => p.CategoryId == categoryId && p.ProtectionType == ManualProtectionType.Deletion);
        reloaded.IsDeleted.Should().BeTrue();
        reloaded.LiftedByActorSubject.Should().Be("tester");
    }

    [Fact]
    public async Task Apply_WhileStabilizing_AlwaysMapsToStabilizationActive()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار التثبيت", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await SecurityTestHarness.SetBarrierStabilizingAsync(fixture);

        var act = () => writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabStabilizationActiveException>()).Which.Code.Should().Be(AbwabConflictCodes.StabilizationActive);
    }

    [Fact]
    public async Task Lift_WhileStabilizing_AlwaysMapsToStabilizationActive()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب اختبار تثبيت الرفع", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(categoryId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var record = await db.AbwabManualProtections.AsNoTracking().SingleAsync(p => p.CategoryId == categoryId && p.ProtectionType == ManualProtectionType.Deletion);

        await SecurityTestHarness.SetBarrierStabilizingAsync(fixture);

        var act = () => writePort.LiftManualProtectionAsync(
            new LiftManualProtectionCommand(categoryId, ManualProtectionType.Deletion, record.Version, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabStabilizationActiveException>()).Which.Code.Should().Be(AbwabConflictCodes.StabilizationActive);
    }

    [Fact]
    public async Task PreviewBlockerIdentity_IsStable_AcrossRepeatedResolutions()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر محمي", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childId = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(rootId, ManualProtectionType.Deletion, ManualProtectionScope.Subtree, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var resolver = AbwabWriterTestHarness.CreateProtectionResolver(db);

        var first = await resolver.ResolveTypeAsync(childId, ManualProtectionType.Deletion, CancellationToken.None);
        var second = await resolver.ResolveTypeAsync(childId, ManualProtectionType.Deletion, CancellationToken.None);

        first!.SourceCategoryId.Should().Be(rootId);
        second!.SourceCategoryId.Should().Be(first.SourceCategoryId, "the blocker identity must be stable across repeated resolutions");
    }
}
