using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class SubtreeDeleteRestoreTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string DependentFixtureTable = "abwab_test_category_dependents";

    public async Task InitializeAsync()
    {
        // Clear the dependent fixture rows BEFORE the substrate reset deletes categories — the fixture's
        // RESTRICT FK would otherwise block that delete with leftover rows from a prior test.
        await EnsureDependentFixtureTableAsync();
        await AbwabSubstrateReset.FullResetAsync(fixture);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Delete_SoftDeletesTheSelectedCategoryAndItsActiveSubtree_Atomically_WithOneDeletionOperationId()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر للحذف", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childId = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع للحذف", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var grandchildId = await writePort.AddCategoryAsync(new AddCategoryCommand("حفيد للحذف", null, null, childId, null, 2, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var root = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var deletionOperationId = await writePort.SubtreeDeleteAsync(
            new SubtreeDeleteCommand(rootId, root.Version, revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var reloadedRoot = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var reloadedChild = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childId);
        var reloadedGrandchild = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == grandchildId);

        foreach (var category in new[] { reloadedRoot, reloadedChild, reloadedGrandchild })
        {
            category.IsDeleted.Should().BeTrue();
            category.DeletionOperationId.Should().Be(deletionOperationId);
        }

        var revisionAfter = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();
        revisionAfter.TreeRevision.Should().Be(revision.TreeRevision + 1, "the whole subtree delete bumps TreeRevision exactly once");
    }

    [Fact]
    public async Task Delete_ChecksDeletionProtectionOnEveryAffectedCategory()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childId = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع محمي من الحذف", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(childId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var root = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var act = () => writePort.SubtreeDeleteAsync(
            new SubtreeDeleteCommand(rootId, root.Version, revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);

        var reloadedRoot = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        reloadedRoot.IsDeleted.Should().BeFalse("the whole subtree delete rolls back when any affected category is Deletion-protected");
    }

    [Fact]
    public async Task Delete_ChecksInternalStructureProtectionOnTheSurvivingParent()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر محمي هيكليًا", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childId = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع يراد حذفه", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(rootId, ManualProtectionType.InternalStructure, ManualProtectionScope.CategoryOnly, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var child = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childId);
        var revision = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var act = () => writePort.SubtreeDeleteAsync(
            new SubtreeDeleteCommand(childId, child.Version, revision.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);
    }

    [Fact]
    public async Task OperationRestore_RestoresExactlyTheCategoriesFromThatOperation_ParentFirst()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر للاستعادة", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var childId = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع للاستعادة", null, null, rootId, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var root = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var revisionBeforeDelete = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var deletionOperationId = await writePort.SubtreeDeleteAsync(
            new SubtreeDeleteCommand(rootId, root.Version, revisionBeforeDelete.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var revisionBeforeRestore = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        await writePort.OperationRestoreAsync(
            new OperationRestoreCommand(deletionOperationId, revisionBeforeRestore.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var reloadedRoot = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var reloadedChild = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == childId);

        reloadedRoot.IsDeleted.Should().BeFalse();
        reloadedRoot.DeletionOperationId.Should().BeNull();
        reloadedChild.IsDeleted.Should().BeFalse();
        reloadedChild.DeletionOperationId.Should().BeNull();

        var revisionAfterRestore = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();
        revisionAfterRestore.TreeRevision.Should().Be(revisionBeforeRestore.TreeRevision + 1);
    }

    [Fact]
    public async Task OperationRestore_RevalidatesDeletionProtection_AndRejectsWhenAnAncestorGainedInheritedDeletionProtectionSinceTheDelete()
    {
        // data-model.md's Category subtree deletion/operation-restore section: restore "revalidates
        // parent existence, normalized names, all three order scopes, Deletion/InternalStructure
        // protection" — mirroring the delete-side Deletion check symmetrically.
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var activeAncestor = await writePort.AddCategoryAsync(new AddCategoryCommand("سلف فعّال", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var toDelete = await writePort.AddCategoryAsync(new AddCategoryCommand("فرع للحذف ثم الاستعادة", null, null, activeAncestor, null, 1, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var toDeleteRow = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == toDelete);
        var revisionBeforeDelete = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var deletionOperationId = await writePort.SubtreeDeleteAsync(
            new SubtreeDeleteCommand(toDelete, toDeleteRow.Version, revisionBeforeDelete.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        // Applied AFTER the delete, on the still-active ancestor — this must still block the restore.
        await writePort.ApplyManualProtectionAsync(
            new ApplyManualProtectionCommand(activeAncestor, ManualProtectionType.Deletion, ManualProtectionScope.Subtree, null, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var revisionBeforeRestore = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var act = () => writePort.OperationRestoreAsync(
            new OperationRestoreCommand(deletionOperationId, revisionBeforeRestore.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);

        var stillDeleted = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == toDelete);
        stillDeleted.IsDeleted.Should().BeTrue("a Deletion-protection revalidation failure must leave the row deleted, not partially restored");
    }

    [Fact]
    public async Task OperationRestore_RevalidatesNames_AndRejectsOnACollision_WithNoPartialRestore()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var rootId = await writePort.AddCategoryAsync(new AddCategoryCommand("جذر متعارض", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var root = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        var revisionBeforeDelete = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var deletionOperationId = await writePort.SubtreeDeleteAsync(
            new SubtreeDeleteCommand(rootId, root.Version, revisionBeforeDelete.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        // A new root reuses the deleted category's name while it is gone.
        var revisionBeforeReuse = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();
        await writePort.AddCategoryAsync(
            new AddCategoryCommand("جذر متعارض", null, null, null, null, revisionBeforeReuse.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var revisionBeforeRestore = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var act = () => writePort.OperationRestoreAsync(
            new OperationRestoreCommand(deletionOperationId, revisionBeforeRestore.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.CategoryNameConflict);

        var stillDeleted = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == rootId);
        stillDeleted.IsDeleted.Should().BeTrue("a revalidation failure changes nothing");
    }

    [Fact]
    public async Task DormantDependents_SurviveTheSoftDelete_ButAreInvisibleToActiveOnlyReads_AndReappearOnRestore()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب له عناصر تابعة", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await InsertDependentAsync(categoryId, "عنصر تابع");

        var category = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        var revisionBeforeDelete = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();

        var deletionOperationId = await writePort.SubtreeDeleteAsync(
            new SubtreeDeleteCommand(categoryId, category.Version, revisionBeforeDelete.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await CountDependentPhysicalRowsAsync()).Should().Be(1, "the dependent is not cascade-deleted");
        (await CountActiveVisibleDependentsAsync()).Should().Be(0, "a dependent whose owning category is soft-deleted is dormant — invisible to active-only reads");

        var revisionBeforeRestore = await db.AbwabRevisionStates.AsNoTracking().SingleAsync();
        await writePort.OperationRestoreAsync(
            new OperationRestoreCommand(deletionOperationId, revisionBeforeRestore.TreeRevision, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await CountActiveVisibleDependentsAsync()).Should().Be(1, "restoring the category reactivates its dependents' visibility");
    }

    [Fact]
    public async Task DependentFixtureTable_UsesRestrictNoCascade_SoAHardDeleteOfAnOwningCategoryWouldFail()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب مقيّد بالحذف", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await InsertDependentAsync(categoryId, "عنصر يمنع الحذف الفعلي");

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM abwab_categories WHERE id = @id";
        command.Parameters.AddWithValue("id", categoryId);

        var act = () => command.ExecuteNonQueryAsync();

        await act.Should().ThrowAsync<PostgresException>("RESTRICT/no-cascade must block a hard delete while a dependent row exists");
    }

    private async Task EnsureDependentFixtureTableAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var create = connection.CreateCommand();
        create.CommandText =
            $"CREATE TABLE IF NOT EXISTS {DependentFixtureTable} (" +
            "id uuid PRIMARY KEY, " +
            "category_id uuid NOT NULL REFERENCES abwab_categories(id) ON DELETE RESTRICT, " +
            "label text NOT NULL)";
        await create.ExecuteNonQueryAsync();

        await using var clear = connection.CreateCommand();
        clear.CommandText = $"DELETE FROM {DependentFixtureTable}";
        await clear.ExecuteNonQueryAsync();
    }

    private async Task InsertDependentAsync(Guid categoryId, string label)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO {DependentFixtureTable} (id, category_id, label) VALUES (@id, @categoryId, @label)";
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("categoryId", categoryId);
        command.Parameters.AddWithValue("label", label);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> CountDependentPhysicalRowsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {DependentFixtureTable}";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private async Task<int> CountActiveVisibleDependentsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT COUNT(*) FROM {DependentFixtureTable} d " +
            "JOIN abwab_categories c ON c.id = d.category_id " +
            "WHERE c.is_deleted = false";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
