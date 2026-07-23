using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

// Master Plan §9 Move row: "Each selected category CategoryData; old/new parent InternalStructure;
// inherited scopes included." A move does not change the moved category's own child-set (its
// InternalStructure is untouched), but it always removes it from the old parent's child-set and
// (for a parent destination) adds it to the new parent's — both parents' InternalStructure must gate.
[Collection(nameof(AbwabDbCollection))]
public sealed class ProtectionGatedMoveTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Move_WithNoProtectionAnywhere_Succeeds()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var oldParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب قديم", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var newParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب جديد", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var moved = await writePort.AddCategoryAsync(new AddCategoryCommand("منقول", null, null, oldParent, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var movedRow = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == moved);

        var moveRevision = await Rev(db);
        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(moved, newParent, null, movedRow.Version)], moveRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Move_WithOnlyTheMovedCategorysOwnInternalStructureProtected_DoesNotBlock()
    {
        // Regression guard for the fixed bypass direction: a move never changes the MOVED category's
        // own child-set, so its own InternalStructure protection must NOT gate the move (only its
        // CategoryData does — covered by the direct/inherited CategoryData tests below).
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var oldParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب قديم", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var newParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب جديد", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var moved = await writePort.AddCategoryAsync(new AddCategoryCommand("منقول محمي هيكليًا لنفسه", null, null, oldParent, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(moved, ManualProtectionType.InternalStructure, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var movedRow = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == moved);

        var moveRevision = await Rev(db);
        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(moved, newParent, null, movedRow.Version)], moveRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Move_BlockedByTheSelectedCategorysDirectCategoryDataProtection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var newParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب جديد", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var moved = await writePort.AddCategoryAsync(new AddCategoryCommand("منقول محمي مباشرة", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(moved, ManualProtectionType.CategoryData, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var movedRow = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == moved);

        var moveRevision = await Rev(db);
        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(moved, newParent, null, movedRow.Version)], moveRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);
    }

    [Fact]
    public async Task Move_BlockedByTheSelectedCategorysInheritedSubtreeCategoryDataProtection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var protectedAncestor = await writePort.AddCategoryAsync(new AddCategoryCommand("سلف محمي بالوراثة", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var moved = await writePort.AddCategoryAsync(new AddCategoryCommand("منقول موروث الحماية", null, null, protectedAncestor, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var newParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب جديد خارج السلف", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(protectedAncestor, ManualProtectionType.CategoryData, ManualProtectionScope.Subtree, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var movedRow = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == moved);

        var moveRevision = await Rev(db);
        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(moved, newParent, null, movedRow.Version)], moveRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);
    }

    [Fact]
    public async Task Move_BlockedByTheOldParentsDirectInternalStructureProtection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var oldParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب قديم محمي هيكليًا", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var newParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب جديد", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var moved = await writePort.AddCategoryAsync(new AddCategoryCommand("منقول من أب محمي", null, null, oldParent, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(oldParent, ManualProtectionType.InternalStructure, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var movedRow = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == moved);

        var moveRevision = await Rev(db);
        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(moved, newParent, null, movedRow.Version)], moveRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);

        var reloadedMoved = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == moved);
        reloadedMoved.ParentCategoryId.Should().Be(oldParent, "the rejected move must leave the tree unchanged");
    }

    [Fact]
    public async Task Move_BlockedByTheOldParentsInheritedSubtreeInternalStructureProtection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var protectedGrandparent = await writePort.AddCategoryAsync(new AddCategoryCommand("جد محمي هيكليًا بالوراثة", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var oldParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب قديم موروث الحماية", null, null, protectedGrandparent, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var newParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب جديد خارج الجد", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var moved = await writePort.AddCategoryAsync(new AddCategoryCommand("منقول من تحت جد محمي", null, null, oldParent, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(protectedGrandparent, ManualProtectionType.InternalStructure, ManualProtectionScope.Subtree, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var movedRow = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == moved);

        var moveRevision = await Rev(db);
        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(moved, newParent, null, movedRow.Version)], moveRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);
    }

    [Fact]
    public async Task Move_BlockedByTheNewDestinationParentsDirectInternalStructureProtection_ClosesTheIntoProtectedParentBypass()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var oldParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب قديم غير محمي", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var newParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب جديد محمي هيكليًا", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var moved = await writePort.AddCategoryAsync(new AddCategoryCommand("منقول إلى أب محمي", null, null, oldParent, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(newParent, ManualProtectionType.InternalStructure, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var movedRow = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == moved);

        var moveRevision = await Rev(db);
        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(moved, newParent, null, movedRow.Version)], moveRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);

        var reloadedMoved = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == moved);
        reloadedMoved.ParentCategoryId.Should().Be(oldParent, "moving into an InternalStructure-protected parent must be rejected, not silently allowed");
    }

    [Fact]
    public async Task Move_BlockedByTheNewDestinationParentsInheritedSubtreeInternalStructureProtection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var oldParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب قديم غير محمي ٢", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var protectedGrandparent = await writePort.AddCategoryAsync(new AddCategoryCommand("جد محمي هيكليًا للوجهة", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var newParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب جديد موروث الحماية", null, null, protectedGrandparent, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var moved = await writePort.AddCategoryAsync(new AddCategoryCommand("منقول إلى وجهة موروثة الحماية", null, null, oldParent, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(protectedGrandparent, ManualProtectionType.InternalStructure, ManualProtectionScope.Subtree, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var movedRow = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == moved);

        var moveRevision = await Rev(db);
        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(moved, newParent, null, movedRow.Version)], moveRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);
    }

    [Fact]
    public async Task Move_ToRoot_BlockedByTheOldParentsInternalStructureProtection_EvenThoughARootHasNoNewParent()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var oldParent = await writePort.AddCategoryAsync(new AddCategoryCommand("أب قديم محمي لرفع فرع منه", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var moved = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع يراد رفعه لجذر", null, null, oldParent, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(oldParent, ManualProtectionType.InternalStructure, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var movedRow = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == moved);

        var moveRevision = await Rev(db);
        var act = () => writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(moved, null, null, movedRow.Version)], moveRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);
    }

    private static Task<long> Rev(QuranDashboardDbContext db) => AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);
}
