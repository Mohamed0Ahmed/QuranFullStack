using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class CategoryContentRevisionTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task NoDedicatedStaleCodeExists_ForCategoryContentRevision()
    {
        var codeFields = typeof(AbwabConflictCodes).GetFields()
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        codeFields.Should().NotContain(code => code.Contains("content_revision", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Edit_BumpsCategoryContentRevisionExactlyOnce()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار عداد المحتوى", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var before = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        before.CategoryContentRevision.Should().Be(0);

        await writePort.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار عداد المحتوى", "وصف جديد", null, before.Version, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var after = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        after.CategoryContentRevision.Should().Be(1);
    }

    [Fact]
    public async Task AliasAddEditRemove_EachBumpCategoryContentRevisionExactlyOnce()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار عداد المرادفات", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var aliasId = await writePort.AddCategoryAliasAsync(new AddCategoryAliasCommand(categoryId, "مرادف أول", ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        (await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId)).CategoryContentRevision.Should().Be(1);

        var alias = await db.AbwabCategorySearchAliases.AsNoTracking().SingleAsync(a => a.CategorySearchAliasId == aliasId);
        await writePort.EditCategoryAliasAsync(new EditCategoryAliasCommand(aliasId, "مرادف محدّث", alias.Version, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        (await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId)).CategoryContentRevision.Should().Be(2);

        var reloadedAlias = await db.AbwabCategorySearchAliases.AsNoTracking().SingleAsync(a => a.CategorySearchAliasId == aliasId);
        await writePort.RemoveCategoryAliasAsync(new RemoveCategoryAliasCommand(aliasId, reloadedAlias.Version, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        (await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId)).CategoryContentRevision.Should().Be(3);
    }

    [Fact]
    public async Task PureMoveOrReorder_Bumps_TreeRevision_ButNeverCategoryContentRevision()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var destinationId = await writePort.AddCategoryAsync(new AddCategoryCommand("وجهة", null, null, null, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childId = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع", null, null, rootId, null, 2, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var revisionBeforeMove = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();
        var child = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childId);

        await writePort.MoveCategoriesAsync(
            new MoveCategoriesCommand([new CategoryMoveEntry(childId, destinationId, null, child.Version)], revisionBeforeMove.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var afterMove = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childId);
        afterMove.CategoryContentRevision.Should().Be(0, "a pure move never bumps CategoryContentRevision");

        var revisionAfterMove = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();
        revisionAfterMove.TreeRevision.Should().Be(revisionBeforeMove.TreeRevision + 1);

        var siblingId = await writePort.AddCategoryAsync(new AddCategoryCommand("شقيق", null, null, destinationId, null, 4, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var revisionBeforeReorder = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();
        var movedChild = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childId);
        var sibling = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == siblingId);

        await writePort.ReorderCategoriesAsync(
            new ReorderCategoriesCommand(
                CategoryOrderScope.Siblings, destinationId, null,
                [new CategoryOrderEntry(childId, 1, movedChild.Version), new CategoryOrderEntry(siblingId, 0, sibling.Version)],
                revisionBeforeReorder.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var afterReorder = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childId);
        afterReorder.CategoryContentRevision.Should().Be(0, "a pure reorder never bumps CategoryContentRevision either");
    }
}
