using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class OrderingTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RootSectionOrderAndGlobalOrder_AreIndependentCounters()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var sectionA = await writePort.AddSectionAsync(new AddSectionCommand("قسم أ", await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var sectionB = await writePort.AddSectionAsync(new AddSectionCommand("قسم ب", await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var rootA1 = await writePort.AddCategoryAsync(new AddCategoryCommand("أ-١", null, null, null, sectionA, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var rootB1 = await writePort.AddCategoryAsync(new AddCategoryCommand("ب-١", null, null, null, sectionB, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var rootA2 = await writePort.AddCategoryAsync(new AddCategoryCommand("أ-٢", null, null, null, sectionA, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var a1 = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootA1);
        var b1 = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootB1);
        var a2 = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootA2);

        a1.SectionOrder.Should().Be(0);
        a2.SectionOrder.Should().Be(1);
        b1.SectionOrder.Should().Be(0, "section order is scoped to its own section");

        a1.GlobalOrder.Should().Be(0);
        b1.GlobalOrder.Should().Be(1);
        a2.GlobalOrder.Should().Be(2, "global order is one counter across every section");
    }

    [Fact]
    public async Task EveryChild_HasAnExplicitSiblingOrder()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childOne = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع أول", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childTwo = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع ثان", null, null, rootId, null, 2, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var one = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childOne);
        var two = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childTwo);

        one.SiblingOrder.Should().Be(0);
        two.SiblingOrder.Should().Be(1);
    }

    [Fact]
    public async Task MovingARootBetweenSections_PreservesGlobalOrder()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var sectionA = await writePort.AddSectionAsync(new AddSectionCommand("قسم أ", await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var sectionB = await writePort.AddSectionAsync(new AddSectionCommand("قسم ب", await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر منقول", null, null, null, sectionA, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var beforeMove = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        await writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand(
                [new CategoryMoveEntry(rootId, null, sectionB, beforeMove.Version)],
                revision.TreeRevision,
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        var afterMove = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        afterMove.SectionId.Should().Be(sectionB);
        afterMove.GlobalOrder.Should().Be(beforeMove.GlobalOrder, "moving a root between sections preserves GlobalOrder");
    }

    [Fact]
    public async Task OneAtomicReorder_BumpsTreeRevisionExactlyOnce()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childOne = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع أول", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childTwo = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع ثان", null, null, rootId, null, 2, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var revisionBefore = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();
        var one = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childOne);
        var two = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childTwo);

        await writePort.ReorderCategoriesAsync(
            new ReorderCategoriesCommand(
                CategoryOrderScope.Siblings,
                rootId,
                null,
                [
                    new CategoryOrderEntry(childOne, 1, one.Version),
                    new CategoryOrderEntry(childTwo, 0, two.Version),
                ],
                revisionBefore.TreeRevision,
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        var revisionAfter = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();
        revisionAfter.TreeRevision.Should().Be(revisionBefore.TreeRevision + 1);

        var reloadedOne = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childOne);
        var reloadedTwo = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childTwo);
        reloadedOne.SiblingOrder.Should().Be(1);
        reloadedTwo.SiblingOrder.Should().Be(0);
    }

    [Fact]
    public async Task Reorder_WithASetThatDoesNotMatchCurrentScopeMembership_MapsToCategoryUnavailable()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childOne = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع أول", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();
        var one = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childOne);

        var act = () => writePort.ReorderCategoriesAsync(
            new ReorderCategoriesCommand(
                CategoryOrderScope.Siblings,
                rootId,
                null,
                [
                    new CategoryOrderEntry(childOne, 0, one.Version),
                    new CategoryOrderEntry(Guid.NewGuid(), 1, 0),
                ],
                revision.TreeRevision,
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.CategoryUnavailable);
    }

    // §9 "Reorder child SiblingOrder": reordered category CategoryData; parent InternalStructure.

    [Fact]
    public async Task ReorderSiblings_BlockedByAReorderedCategorysDirectCategoryDataProtection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childOne = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع محمي بالبيانات", null, null, rootId, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childTwo = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع آخر", null, null, rootId, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(childOne, ManualProtectionType.CategoryData, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var one = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childOne);
        var two = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childTwo);

        var reorderRevision = await Rev(db);
        var act = () => writePort.ReorderCategoriesAsync(
            new ReorderCategoriesCommand(
                CategoryOrderScope.Siblings, rootId, null,
                [new CategoryOrderEntry(childOne, 1, one.Version), new CategoryOrderEntry(childTwo, 0, two.Version)],
                reorderRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);
    }

    [Fact]
    public async Task ReorderSiblings_BlockedByTheParentsDirectInternalStructureProtection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر محمي هيكليًا", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childOne = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع أول", null, null, rootId, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childTwo = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع ثان", null, null, rootId, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(rootId, ManualProtectionType.InternalStructure, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var one = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childOne);
        var two = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childTwo);

        var reorderRevision = await Rev(db);
        var act = () => writePort.ReorderCategoriesAsync(
            new ReorderCategoriesCommand(
                CategoryOrderScope.Siblings, rootId, null,
                [new CategoryOrderEntry(childOne, 1, one.Version), new CategoryOrderEntry(childTwo, 0, two.Version)],
                reorderRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);
    }

    // §9 "Reorder root SectionOrder/GlobalOrder": reordered root CategoryData; no coupling between orders.

    [Fact]
    public async Task ReorderGlobalRoots_BlockedByAReorderedRootsCategoryDataProtection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootOne = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر محمي بالبيانات للترتيب", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var rootTwo = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر آخر للترتيب", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(rootOne, ManualProtectionType.CategoryData, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var one = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootOne);
        var two = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootTwo);

        var reorderRevision = await Rev(db);
        var act = () => writePort.ReorderCategoriesAsync(
            new ReorderCategoriesCommand(
                CategoryOrderScope.GlobalRoots, null, null,
                [new CategoryOrderEntry(rootOne, 1, one.Version), new CategoryOrderEntry(rootTwo, 0, two.Version)],
                reorderRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);
    }

    [Fact]
    public async Task ReorderGlobalRoots_WithNoProtection_Succeeds()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootOne = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر أول غير محمي", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var rootTwo = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر ثان غير محمي", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var one = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootOne);
        var two = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootTwo);

        var reorderRevision = await Rev(db);
        var act = () => writePort.ReorderCategoriesAsync(
            new ReorderCategoriesCommand(
                CategoryOrderScope.GlobalRoots, null, null,
                [new CategoryOrderEntry(rootOne, 1, one.Version), new CategoryOrderEntry(rootTwo, 0, two.Version)],
                reorderRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private static Task<long> Rev(QuranDashboardDbContext db) => AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);
}
