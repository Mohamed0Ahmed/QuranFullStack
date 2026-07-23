using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Security.Owners;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class OrdinaryProtectionWindowTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = DateTimeOffset.UnixEpoch.AddYears(1);

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DirectContentEdit_StartsTheWindow_AndBlocksADifferentActorWithin24Hours()
    {
        var (port1, db1) = AbwabWriterTestHarness.CreateWritePort(fixture, new FixedServerClock(Start));
        await using var _1 = db1;

        var categoryId = await port1.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار النافذة", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);
        var before = await db1.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);

        await port1.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار النافذة", "أول تعديل", null, before.Version, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);

        var (port2, db2) = AbwabWriterTestHarness.CreateWritePort(fixture, new FixedServerClock(Start.AddHours(1)));
        await using var _2 = db2;
        var afterFirstEdit = await db2.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);

        var act = () => port2.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار النافذة", "محاولة ممثل آخر", null, afterFirstEdit.Version, ExpectedTimelineGeneration.Of(0), "actor-2"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.OrdinaryProtection);
    }

    [Fact]
    public async Task DirectContentEdit_AllowsTheSameLastEditor_WithinTheWindow_AndRestartsIt()
    {
        var (port1, db1) = AbwabWriterTestHarness.CreateWritePort(fixture, new FixedServerClock(Start));
        await using var _1 = db1;

        var categoryId = await port1.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار تمديد النافذة", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);
        var before = await db1.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        await port1.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار تمديد النافذة", "أول تعديل", null, before.Version, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);

        var (port2, db2) = AbwabWriterTestHarness.CreateWritePort(fixture, new FixedServerClock(Start.AddHours(20)));
        await using var _2 = db2;
        var afterFirstEdit = await db2.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);

        var act = () => port2.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار تمديد النافذة", "تعديل ثان من نفس الممثل", null, afterFirstEdit.Version, ExpectedTimelineGeneration.Of(0), "actor-1"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();

        var reloaded = await db2.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        reloaded.OrdinaryProtectionLastEditedAtUtc.Should().Be(Start.AddHours(20), "the same actor's edit restarts the window");
    }

    [Fact]
    public async Task DirectContentEdit_AfterTheWindowExpires_IsAllowedForAnyActor()
    {
        var (port1, db1) = AbwabWriterTestHarness.CreateWritePort(fixture, new FixedServerClock(Start));
        await using var _1 = db1;

        var categoryId = await port1.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار انتهاء النافذة", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);
        var before = await db1.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        await port1.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار انتهاء النافذة", "أول تعديل", null, before.Version, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);

        var (port2, db2) = AbwabWriterTestHarness.CreateWritePort(fixture, new FixedServerClock(Start.AddHours(25)));
        await using var _2 = db2;
        var afterFirstEdit = await db2.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);

        var act = () => port2.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار انتهاء النافذة", "تعديل بعد الانتهاء", null, afterFirstEdit.Version, ExpectedTimelineGeneration.Of(0), "actor-2"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SystemOwner_MayEditWithinAnotherActorsActiveWindow()
    {
        await SecurityTestHarness.BootstrapOwnerAsync(fixture, new BootstrapSystemOwnerCommand(
            "https://logto.test", "system-owner", "https://logto.test", true, true, ExpectedTimelineGeneration.Of(0), "system-owner"));

        var (port1, db1) = AbwabWriterTestHarness.CreateWritePort(fixture, new FixedServerClock(Start));
        await using var _1 = db1;

        var categoryId = await port1.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار المالك", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);
        var before = await db1.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        await port1.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار المالك", "أول تعديل", null, before.Version, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);

        var (port2, db2) = AbwabWriterTestHarness.CreateWritePort(fixture, new FixedServerClock(Start.AddHours(1)));
        await using var _2 = db2;
        var afterFirstEdit = await db2.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);

        var act = () => port2.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار المالك", "تعديل المالك", null, afterFirstEdit.Version, ExpectedTimelineGeneration.Of(0), "system-owner"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Move_OfTheSelectedCategory_StartsItsOwnWindow_ButDescendantSideEffectsGetNoWindow()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture, new FixedServerClock(Start));
        await using var _ = db;

        var sourceRoot = await writePort.AddCategoryAsync(new AddCategoryCommand("مصدر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var destinationRoot = await writePort.AddCategoryAsync(new AddCategoryCommand("وجهة", null, null, null, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var movedNode = await writePort.AddCategoryAsync(new AddCategoryCommand("منقول", null, null, sourceRoot, null, 2, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var descendant = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع تابع", null, null, movedNode, null, 3, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var moved = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == movedNode);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        await writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(movedNode, destinationRoot, null, moved.Version)], revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "mover"),
            CancellationToken.None);

        var reloadedMoved = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == movedNode);
        var reloadedDescendant = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == descendant);

        reloadedMoved.OrdinaryProtectionActorSubject.Should().Be("mover");
        reloadedMoved.OrdinaryProtectionLastEditedAtUtc.Should().Be(Start);
        reloadedDescendant.OrdinaryProtectionActorSubject.Should().BeNull("a descendant carried along as a side effect never starts its own window");
    }

    [Fact]
    public async Task Reorder_DoesNotStartOrCheckTheOrdinaryWindow()
    {
        var (port1, db1) = AbwabWriterTestHarness.CreateWritePort(fixture, new FixedServerClock(Start));
        await using var _1 = db1;

        var rootId = await port1.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);
        var childOne = await port1.AddCategoryAsync(new AddCategoryCommand("فرع أول", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);
        var childTwo = await port1.AddCategoryAsync(new AddCategoryCommand("فرع ثان", null, null, rootId, null, 2, ExpectedTimelineGeneration.Of(0), "actor-1"), CancellationToken.None);

        var one = await db1.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childOne);
        var two = await db1.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childTwo);
        var revision = await db1.AbwabRevisionStates.AsNoTracking().SingleAsync();

        // A DIFFERENT actor may reorder immediately after creation with no ordinary-window check at all.
        await port1.ReorderCategoriesAsync(
            new ReorderCategoriesCommand(
                CategoryOrderScope.Siblings, rootId, null,
                [new CategoryOrderEntry(childOne, 1, one.Version), new CategoryOrderEntry(childTwo, 0, two.Version)],
                revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "actor-2"),
            CancellationToken.None);

        var reloadedOne = await db1.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childOne);
        reloadedOne.OrdinaryProtectionActorSubject.Should().BeNull("pure reorder never starts the ordinary window");
    }
}
