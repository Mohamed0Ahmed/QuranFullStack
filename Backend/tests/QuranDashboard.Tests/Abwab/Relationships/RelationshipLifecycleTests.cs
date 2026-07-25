using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Relationships;

[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipLifecycleTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteThenRestore_IsTrackedSoftDeleteAndRestore_PreservingIdentityAndHistory()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var relationshipId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, lower.CategoryId, higher.CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var added = await ReadAsync(db, relationshipId);
        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(relationshipId, added.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var deleted = await ReadAsync(db, relationshipId);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedAtUtc.Should().NotBeNull();
        deleted.LowerCategoryId.Should().Be(lower.CategoryId);
        deleted.HigherCategoryId.Should().Be(higher.CategoryId);

        await writePort.RestoreAsync(
            new RestoreRelationshipCommand(relationshipId, deleted.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var restored = await ReadAsync(db, relationshipId);
        restored.IsDeleted.Should().BeFalse();
        restored.DeletedAtUtc.Should().BeNull();
        restored.CategoryRelationshipId.Should().Be(relationshipId, "restore reuses the same row, it never re-creates one");

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeletingAnAlreadyDeletedRow_AndRestoringAnActiveOne_AreRefusedAsStale_NotReportedAsDone()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var relationshipId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, lower.CategoryId, higher.CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var added = await ReadAsync(db, relationshipId);

        var restoreActive = () => writePort.RestoreAsync(
            new RestoreRelationshipCommand(relationshipId, added.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        (await restoreActive.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RowStale, "restoring a row that is already active did nothing and must not answer success");

        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(relationshipId, added.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var deleted = await ReadAsync(db, relationshipId);

        var deleteAgain = () => writePort.DeleteAsync(
            new DeleteRelationshipCommand(relationshipId, deleted.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        (await deleteAgain.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RowStale);

        (await ReadAsync(db, relationshipId)).Version.Should().Be(deleted.Version, "a refused mutation touches no row");
    }

    [Fact]
    public async Task EveryRelationshipMutation_WritesExactlyOneAuditedChangeSet()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);
        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();

        var relationshipId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, lower.CategoryId, higher.CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var added = await ReadAsync(db, relationshipId);

        await writePort.EditAsync(
            new EditRelationshipCommand(
                relationshipId, RelationshipType.Opposite, lower.CategoryId, higher.CategoryId, added.Version,
                ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var edited = await ReadAsync(db, relationshipId);

        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(relationshipId, edited.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var deleted = await ReadAsync(db, relationshipId);

        await writePort.RestoreAsync(
            new RestoreRelationshipCommand(relationshipId, deleted.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await db.AbwabChangeSets.AsNoTracking().CountAsync() - changeSetsBefore).Should().Be(4);
        (await db.AbwabAuditEvents.AsNoTracking().CountAsync(e => e.Payload.Contains("relationship."))).Should().Be(4);
    }

    [Fact]
    public async Task RelationshipMutations_NeverBumpTreeRevision()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);
        var before = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        var relationshipId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, lower.CategoryId, higher.CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var added = await ReadAsync(db, relationshipId);
        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(relationshipId, added.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db)).Should().Be(before);
    }

    [Fact]
    public async Task APhysicalDelete_IsRejectedByTheWriteGuard()
    {
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);
        var relationship = AbwabRelationshipTemplateSeeding.NewMutualRelationship(lower.CategoryId, higher.CategoryId);
        await AbwabTreeSeeding.InsertAsync(fixture, relationship);

        await using var db = AbwabKernelHarness.CreateProductionContext(
            fixture, new AbwabWriteGuardInterceptor(AbwabPersonalDeletePolicy.Default));

        var tracked = await db.Set<CategoryRelationship>().SingleAsync(r => r.CategoryRelationshipId == relationship.CategoryRelationshipId);
        db.Remove(tracked);

        var act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<AbwabPhysicalDeleteRejectedException>();
    }

    private static Task<CategoryRelationship> ReadAsync(QuranDashboardDbContext db, Guid relationshipId) =>
        db.Set<CategoryRelationship>().AsNoTracking().SingleAsync(r => r.CategoryRelationshipId == relationshipId);
}
