using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class MoveGuardTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Move_ACategoryUnderItself_MapsToCategoryCycle()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var root = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(rootId, rootId, null, root.Version)], revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.CategoryCycle);
    }

    [Fact]
    public async Task Move_ACategoryIntoItsOwnDescendant_MapsToCategoryCycle()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childId = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var grandchildId = await writePort.AddCategoryAsync(new AddCategoryCommand("حفيد", null, null, childId, null, 2, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var root = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(rootId, grandchildId, null, root.Version)], revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.CategoryCycle);
    }

    [Fact]
    public async Task Move_ToAMissingDestinationParent_MapsToCategoryUnavailable()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var root = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(rootId, Guid.NewGuid(), null, root.Version)], revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.CategoryUnavailable);
    }

    [Fact]
    public async Task Move_OverlappingSelection_MapsToCategoryOverlappingMove()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childId = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var otherRootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر آخر", null, null, null, null, 2, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var root = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var child = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childId);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand(
                [
                    new CategoryMoveEntry(rootId, otherRootId, null, root.Version),
                    new CategoryMoveEntry(childId, otherRootId, null, child.Version),
                ],
                revision.TreeRevision,
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.CategoryOverlappingMove);
    }

    [Fact]
    public async Task ValidMove_RewritesAncestorIdsAndDepthForEveryDescendant_WithNoPartialOrderChange()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var sourceRoot = await writePort.AddCategoryAsync(new AddCategoryCommand("مصدر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var destinationRoot = await writePort.AddCategoryAsync(new AddCategoryCommand("وجهة", null, null, null, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var movedNode = await writePort.AddCategoryAsync(new AddCategoryCommand("منقول", null, null, sourceRoot, null, 2, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var grandchild = await writePort.AddCategoryAsync(new AddCategoryCommand("حفيد منقول", null, null, movedNode, null, 3, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var moved = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == movedNode);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        await writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(movedNode, destinationRoot, null, moved.Version)], revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var reloadedMoved = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == movedNode);
        var reloadedGrandchild = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == grandchild);

        reloadedMoved.ParentCategoryId.Should().Be(destinationRoot);
        reloadedMoved.AncestorIds.Should().Equal(destinationRoot);
        reloadedMoved.Depth.Should().Be(1);

        reloadedGrandchild.AncestorIds.Should().Equal([destinationRoot, movedNode]);
        reloadedGrandchild.Depth.Should().Be(2);
    }
}
