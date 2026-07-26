using System.Text.Json;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

// T064's mandated regression assertion. `030` extracted the `029` in-transaction creation core into
// CategoryGroupedCreation so template application could reuse it; this pins the single-add path's
// observable behaviour across that extraction — one operation, one bump, one audit draft of the same
// shape, and a returned id that is the persisted row's id (the seam now allocates it INSIDE the
// operation rather than before it).
[Collection(nameof(AbwabDbCollection))]
public sealed class CategoryGroupedCreationSeamTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ASingleAdd_StillProducesOneChangeSetOneTreeRevisionBumpAndTheSameAuditDraft()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;
        var (parent, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();
        var auditEventsBefore = await db.AbwabAuditEvents.AsNoTracking().CountAsync();
        var treeRevisionBefore = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand(
                "باب مضاف",
                "وصف",
                "مقطع",
                parent.CategoryId,
                null,
                treeRevisionBefore,
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        (await db.AbwabChangeSets.AsNoTracking().CountAsync() - changeSetsBefore).Should().Be(1);
        (await db.AbwabAuditEvents.AsNoTracking().CountAsync() - auditEventsBefore).Should().Be(1);
        (await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db)).Should().Be(treeRevisionBefore + 1);

        var persisted = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.ParentCategoryId == parent.CategoryId);
        persisted.CategoryId.Should().Be(categoryId, "the returned id must be the persisted row's id");
        persisted.SiblingOrder.Should().Be(0);
        persisted.Depth.Should().Be(parent.Depth + 1);

        var payload = JsonDocument.Parse(
            (await db.AbwabAuditEvents.AsNoTracking().OrderByDescending(e => e.ServerTimestampUtc).FirstAsync()).Payload).RootElement;
        payload.GetProperty("type").GetString().Should().Be("category.added");
        var data = payload.GetProperty("data");
        data.GetProperty("CategoryId").GetGuid().Should().Be(categoryId);
        data.GetProperty("Name").GetString().Should().Be("باب مضاف");
        data.GetProperty("ParentCategoryId").GetGuid().Should().Be(parent.CategoryId);
        data.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["CategoryId", "Name", "ParentCategoryId", "SectionId"],
                "the 029 audit draft shape is unchanged by the seam extraction");
    }

    [Fact]
    public async Task AFailedSingleAdd_StillCommitsNothing_SoTheSeamNeverLeavesAHalfCreatedRow()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;
        var (parent, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);
        var treeRevision = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب مكرر", null, null, parent.CategoryId, null, treeRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();
        var treeRevisionAfterFirst = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        var act = () => writePort.AddCategoryAsync(
            new AddCategoryCommand("باب مكرر", null, null, parent.CategoryId, null, treeRevisionAfterFirst, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.CategoryNameConflict);

        (await db.AbwabCategories.AsNoTracking().CountAsync(c => c.ParentCategoryId == parent.CategoryId)).Should().Be(1);
        (await db.AbwabChangeSets.AsNoTracking().CountAsync()).Should().Be(changeSetsBefore);
        (await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db)).Should().Be(treeRevisionAfterFirst);
    }
}
