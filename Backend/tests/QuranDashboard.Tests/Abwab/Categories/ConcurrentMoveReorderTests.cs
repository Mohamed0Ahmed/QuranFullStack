using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class ConcurrentMoveReorderTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MoveAfterAConcurrentStructuralChange_MapsToTreeRevisionStale_WithNoCorruption()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var destinationId = await writePort.AddCategoryAsync(new AddCategoryCommand("وجهة", null, null, null, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var root = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var staleRevision = (await db.AbwabRevisionStates.AsNoTracking().SingleAsync()).TreeRevision;

        // A concurrent reorder-like structural op (another Add) bumps TreeRevision before the move commits.
        await writePort.AddCategoryAsync(new AddCategoryCommand("جذر ثالث", null, null, null, null, 2, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(rootId, destinationId, null, root.Version)], staleRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.TreeRevisionStale);

        var reloadedRoot = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        reloadedRoot.ParentCategoryId.Should().BeNull("the rejected move must leave the tree unchanged");
    }

    [Fact]
    public async Task Reorder_WithAStaleExpectedRowVersion_MapsToRowStale_WithNoCorruption()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childOne = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع أول", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childTwo = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع ثان", null, null, rootId, null, 2, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var staleOne = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childOne);
        var two = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childTwo);

        // A concurrent edit bumps childOne's xmin before the reorder commits.
        var revisionBeforeEdit = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();
        await writePort.EditCategoryAsync(
            new EditCategoryCommand(childOne, "فرع أول محدّث", null, null, staleOne.Version, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var revisionForReorder = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var act = () => writePort.ReorderCategoriesAsync(
            new ReorderCategoriesCommand(
                CategoryOrderScope.Siblings,
                rootId,
                null,
                [
                    new CategoryOrderEntry(childOne, 1, staleOne.Version),
                    new CategoryOrderEntry(childTwo, 0, two.Version),
                ],
                revisionForReorder.TreeRevision,
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.RowStale);

        var reloadedOne = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childOne);
        var reloadedTwo = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childTwo);
        reloadedOne.SiblingOrder.Should().Be(0, "the rejected reorder must leave sibling order unchanged");
        reloadedTwo.SiblingOrder.Should().Be(1);
        revisionBeforeEdit.TreeRevision.Should().Be(revisionForReorder.TreeRevision, "an edit is content-only and never bumps TreeRevision");
    }

    [Fact]
    public async Task GenuinelyConcurrentMoveAndReorder_OnTheSameTree_OneWinsAndOneGetsASafe409()
    {
        var rootId = Guid.Empty;
        Guid childId;
        Guid destinationId;

        {
            var (setupPort, setupDb) = AbwabWriterTestHarness.CreateWritePort(fixture);
            await using var __ = setupDb;
            rootId = await setupPort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
            childId = await setupPort.AddCategoryAsync(new AddCategoryCommand("فرع", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
            destinationId = await setupPort.AddCategoryAsync(new AddCategoryCommand("وجهة", null, null, null, null, 2, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        }

        var (portA, dbA) = AbwabWriterTestHarness.CreateWritePort(fixture);
        var (portB, dbB) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _a = dbA;
        await using var _b = dbB;

        var child = await dbA.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childId);
        var revision = await dbA.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var moveTask = portA.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(childId, destinationId, null, child.Version)], revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "actor-a"),
            CancellationToken.None);

        var reorderTask = portB.ReorderCategoriesAsync(
            new ReorderCategoriesCommand(
                CategoryOrderScope.Siblings,
                rootId,
                null,
                [new CategoryOrderEntry(childId, 0, child.Version)],
                revision.TreeRevision,
                ExpectedTimelineGeneration.Of(0),
                "actor-b"),
            CancellationToken.None);

        var results = await Task.WhenAll(moveTask.ContinueWith(TranslateOutcome), reorderTask.ContinueWith(TranslateOutcome));

        results.Count(r => r == "ok").Should().Be(1, "exactly one of the two concurrent structural operations commits");
        results.Count(r => r is AbwabConflictCodes.TreeRevisionStale or AbwabConflictCodes.RowStale).Should().Be(1, "the loser must fail with a safe 409, not corrupt state");
    }

    private static string TranslateOutcome(Task task)
    {
        if (task.Exception?.InnerException is AbwabWriteConflictException conflict)
        {
            return conflict.Code;
        }

        return task.IsFaulted ? "unexpected-error" : "ok";
    }
}
