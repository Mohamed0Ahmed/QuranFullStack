using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Relationships;

[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipDormancyTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ASubtreeDelete_LeavesEveryRelationshipRowPresentAndUnchanged_AndWritesNoRelationshipAuditEvent()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;
        var graph = await SeedGraphAsync();

        var before = await SnapshotAsync(db);
        var relationshipEventsBefore = await RelationshipEventCountAsync(db);

        await SubtreeDeleteAsync(writePort, db, graph.Root);

        var after = await SnapshotAsync(db);
        after.Should().BeEquivalentTo(before, "a category subtree delete cascade-deletes nothing and stamps no relationship row");
        after.Should().OnlyContain(row => !row.IsDeleted, "dormancy is derived from endpoint state, never a written flag");
        (await RelationshipEventCountAsync(db)).Should().Be(relationshipEventsBefore, "a category subtree delete produces no relationship audit event");
    }

    [Fact]
    public async Task ASubtreeDelete_MakesAttachedRelationshipsDormantInReads_AndOperationRestoreReExposesTheSameIds()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;
        var graph = await SeedGraphAsync();
        var readPort = AbwabWriterTestHarness.CreateRelationshipReadPort(db);

        var visibleBefore = await ActiveRelationshipIdsAsync(readPort, graph.Outsider);
        visibleBefore.Should().HaveCount(2);

        var deletionOperationId = await SubtreeDeleteAsync(writePort, db, graph.Root);

        (await ActiveRelationshipIdsAsync(readPort, graph.Outsider)).Should().BeEmpty(
            "a relationship with a deleted endpoint is dormant: filtered out of reads and not actionable");

        await writePort.OperationRestoreAsync(
            new OperationRestoreCommand(
                deletionOperationId,
                await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db),
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        (await ActiveRelationshipIdsAsync(readPort, graph.Outsider)).Should().BeEquivalentTo(visibleBefore,
            "operation-restore re-exposes the same rows by identity — no re-creation, no new identity");
        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync()).Should().Be(2, "no relationship row is created by a restore");
    }

    [Fact]
    public async Task DeletingOnlyOneEndpoint_MakesTheRowDormantUntilThatEndpointIsRestored()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;
        var graph = await SeedGraphAsync();
        var readPort = AbwabWriterTestHarness.CreateRelationshipReadPort(db);

        var deletionOperationId = await SubtreeDeleteAsync(writePort, db, graph.FirstChild);

        (await ActiveRelationshipIdsAsync(readPort, graph.Outsider)).Should().HaveCount(1,
            "only the row touching the deleted endpoint goes dormant");

        await writePort.OperationRestoreAsync(
            new OperationRestoreCommand(
                deletionOperationId,
                await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db),
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        (await ActiveRelationshipIdsAsync(readPort, graph.Outsider)).Should().HaveCount(2);
    }

    [Fact]
    public async Task APhysicalCategoryDelete_IsRejectedByTheRestrictEndpointForeignKey()
    {
        await SeedGraphAsync();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM abwab_categories WHERE id IN (SELECT lower_category_id FROM abwab_category_relationships WHERE lower_category_id IS NOT NULL)";

        var act = () => command.ExecuteNonQueryAsync();

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23503",
            "endpoint foreign keys are RESTRICT/no-cascade, so no cascade path to a relationship row exists");
    }

    [Fact]
    public async Task StoredEndpointRelationshipProtection_BlocksRelationshipDeleteAndRestore_WhileTheRowIsDormant()
    {
        var (corePort, coreDb) = AbwabWriterTestHarness.CreateWritePort(fixture);
        var (relationshipPort, relationshipDb) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _core = coreDb;
        await using var _relationship = relationshipDb;
        var graph = await SeedGraphAsync();

        await AbwabTreeSeeding.InsertAsync(
            fixture,
            AbwabTreeSeeding.NewManualProtection(graph.Outsider.CategoryId, ManualProtectionType.Relationship, ManualProtectionScope.CategoryOnly));

        await SubtreeDeleteAsync(corePort, coreDb, graph.Root);

        var dormant = await relationshipDb.Set<CategoryRelationship>().AsNoTracking().FirstAsync(r => r.LowerCategoryId != null);

        var blockedDelete = () => relationshipPort.DeleteAsync(
            new DeleteRelationshipCommand(dormant.CategoryRelationshipId, dormant.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await blockedDelete.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.ManualProtection,
                "the stored-endpoint target set is enforced on delete even while the row is dormant");

        var blockedRestore = () => relationshipPort.RestoreAsync(
            new RestoreRelationshipCommand(dormant.CategoryRelationshipId, dormant.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await blockedRestore.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.ManualProtection,
                "the stored-endpoint target set is enforced on restore too");
    }

    [Fact]
    public async Task ADormantRelationship_IsNotActionable_UntilItsEndpointIsRestored()
    {
        var (corePort, coreDb) = AbwabWriterTestHarness.CreateWritePort(fixture);
        var (relationshipPort, relationshipDb) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _core = coreDb;
        await using var _relationship = relationshipDb;
        var graph = await SeedGraphAsync();

        var deletionOperationId = await SubtreeDeleteAsync(corePort, coreDb, graph.Root);
        var dormant = await relationshipDb.Set<CategoryRelationship>().AsNoTracking().FirstAsync(r => r.LowerCategoryId != null);

        var act = () => relationshipPort.DeleteAsync(
            new DeleteRelationshipCommand(dormant.CategoryRelationshipId, dormant.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.CategoryUnavailable);

        await corePort.OperationRestoreAsync(
            new OperationRestoreCommand(
                deletionOperationId,
                await AbwabWriterTestHarness.CurrentTreeRevisionAsync(coreDb),
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        await relationshipPort.DeleteAsync(
            new DeleteRelationshipCommand(dormant.CategoryRelationshipId, dormant.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await relationshipDb.Set<CategoryRelationship>().AsNoTracking()
            .SingleAsync(r => r.CategoryRelationshipId == dormant.CategoryRelationshipId)).IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task TheDormantCountsProjection_ReportsAttachedRelationshipsForTheAffectedCategorySet()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;
        var graph = await SeedGraphAsync();
        var readPort = AbwabWriterTestHarness.CreateRelationshipReadPort(db);

        var affectedIds = new[] { graph.Root.CategoryId, graph.FirstChild.CategoryId, graph.SecondChild.CategoryId };

        (await readPort.GetDormantCountsAsync(affectedIds, CancellationToken.None)).TotalDormant.Should().Be(0,
            "nothing is dormant while every endpoint is active");

        await SubtreeDeleteAsync(writePort, db, graph.Root);

        var counts = await readPort.GetDormantCountsAsync(affectedIds, CancellationToken.None);

        counts.TotalDormant.Should().Be(2);
        counts.ByType.Should().BeEquivalentTo(new[]
        {
            new DormantRelationshipCountDto(RelationshipType.Similar, 1),
            new DormantRelationshipCountDto(RelationshipType.BroaderNarrower, 1),
        });
    }

    private async Task<RelationshipGraph> SeedGraphAsync()
    {
        var (root, children) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 2);
        var outsider = (await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 1))[0];

        var mutualLower = children[0].CategoryId.CompareTo(outsider.CategoryId) < 0 ? children[0].CategoryId : outsider.CategoryId;
        var mutualHigher = mutualLower == children[0].CategoryId ? outsider.CategoryId : children[0].CategoryId;

        await AbwabTreeSeeding.InsertAsync(
            fixture,
            AbwabRelationshipTemplateSeeding.NewMutualRelationship(mutualLower, mutualHigher),
            AbwabRelationshipTemplateSeeding.NewDirectionalRelationship(children[1].CategoryId, outsider.CategoryId));

        return new RelationshipGraph(root, children[0], children[1], outsider);
    }

    private async Task<Guid> SubtreeDeleteAsync(IAbwabCoreWritePort writePort, QuranDashboardDbContext db, Category target)
    {
        var tracked = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == target.CategoryId);
        return await writePort.SubtreeDeleteAsync(
            new SubtreeDeleteCommand(
                target.CategoryId,
                tracked.Version,
                await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db),
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);
    }

    private static async Task<IReadOnlyList<Guid>> ActiveRelationshipIdsAsync(IAbwabRelationshipReadPort readPort, Category category)
    {
        var list = await readPort.GetForCategoryAsync(category.CategoryId, includeDeleted: false, CancellationToken.None);
        return list.Relationships.Select(r => r.CategoryRelationshipId).ToList();
    }

    private static async Task<IReadOnlyList<RelationshipRowFingerprint>> SnapshotAsync(QuranDashboardDbContext db) =>
        await db.Set<CategoryRelationship>().AsNoTracking()
            .OrderBy(r => r.CategoryRelationshipId)
            .Select(r => new RelationshipRowFingerprint(
                r.CategoryRelationshipId, r.RelationshipType, r.LowerCategoryId, r.HigherCategoryId,
                r.SourceCategoryId, r.TargetCategoryId, r.IsDeleted, r.DeletedAtUtc, r.Version))
            .ToListAsync();

    private static Task<int> RelationshipEventCountAsync(QuranDashboardDbContext db) =>
        db.AbwabAuditEvents.AsNoTracking().CountAsync(e => e.Payload.Contains("relationship."));

    private sealed record RelationshipGraph(Category Root, Category FirstChild, Category SecondChild, Category Outsider);

    private sealed record RelationshipRowFingerprint(
        Guid CategoryRelationshipId,
        RelationshipType RelationshipType,
        Guid? LowerCategoryId,
        Guid? HigherCategoryId,
        Guid? SourceCategoryId,
        Guid? TargetCategoryId,
        bool IsDeleted,
        DateTimeOffset? DeletedAtUtc,
        uint Version);
}
