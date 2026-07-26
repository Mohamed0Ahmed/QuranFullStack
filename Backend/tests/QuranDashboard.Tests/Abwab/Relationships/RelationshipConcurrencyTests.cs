using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Relationships;

[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipConcurrencyTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AnEditCarryingAStaleRowVersion_MapsToRowStale_WithZeroRowsTouched()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var relationshipId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, lower.CategoryId, higher.CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var staleVersion = (await ReadAsync(db, relationshipId)).Version;

        await writePort.EditAsync(
            new EditRelationshipCommand(
                relationshipId, RelationshipType.Opposite, lower.CategoryId, higher.CategoryId, staleVersion,
                ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var afterFirstEdit = await ReadAsync(db, relationshipId);
        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();

        var act = () => writePort.EditAsync(
            new EditRelationshipCommand(
                relationshipId, RelationshipType.Similar, lower.CategoryId, higher.CategoryId, staleVersion,
                ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RowStale);

        var unchanged = await ReadAsync(db, relationshipId);
        unchanged.RelationshipType.Should().Be(afterFirstEdit.RelationshipType);
        unchanged.Version.Should().Be(afterFirstEdit.Version, "a rejected write must not touch the row");
        (await db.AbwabChangeSets.AsNoTracking().CountAsync()).Should().Be(changeSetsBefore, "a rejected write writes no ChangeSet");
    }

    [Fact]
    public async Task AnEditTargetingASoftDeletedRelationship_MapsToRowStale_WithZeroRowsTouched()
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
        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();

        // A soft-deleted row is not addressable by edit: restore owns re-activation, so an edit that
        // carries the CURRENT version still fails rather than quietly resurrecting the row.
        var act = () => writePort.EditAsync(
            new EditRelationshipCommand(
                relationshipId, RelationshipType.Opposite, lower.CategoryId, higher.CategoryId, deleted.Version,
                ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RowStale);

        var unchanged = await ReadAsync(db, relationshipId);
        unchanged.IsDeleted.Should().BeTrue();
        unchanged.RelationshipType.Should().Be(deleted.RelationshipType);
        unchanged.Version.Should().Be(deleted.Version, "a rejected write must not touch the row");
        (await db.AbwabChangeSets.AsNoTracking().CountAsync()).Should().Be(changeSetsBefore, "a rejected write writes no ChangeSet");
    }

    [Theory]
    [InlineData("delete")]
    [InlineData("restore")]
    public async Task ALifecycleMutationCarryingAStaleRowVersion_MapsToRowStale(string operation)
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var relationshipId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, lower.CategoryId, higher.CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var staleVersion = (await ReadAsync(db, relationshipId)).Version;

        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(relationshipId, staleVersion, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        Func<Task> act = operation == "delete"
            ? () => writePort.DeleteAsync(
                new DeleteRelationshipCommand(relationshipId, staleVersion, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None)
            : () => writePort.RestoreAsync(
                new RestoreRelationshipCommand(relationshipId, staleVersion, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RowStale);
    }

    [Fact]
    public async Task AMutationCarryingAStaleExpectedTimelineGeneration_MapsToTimelineGenerationStale_WithZeroRowsTouched()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var relationshipsBefore = await db.Set<CategoryRelationship>().AsNoTracking().CountAsync();
        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();

        var act = () => writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, lower.CategoryId, higher.CategoryId, ExpectedTimelineGeneration.Of(7), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabTimelineGenerationStaleException>())
            .Which.Code.Should().Be(AbwabConflictCodes.TimelineGenerationStale);

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync()).Should().Be(relationshipsBefore);
        (await db.AbwabChangeSets.AsNoTracking().CountAsync()).Should().Be(changeSetsBefore);
    }

    private static Task<CategoryRelationship> ReadAsync(QuranDashboardDbContext db, Guid relationshipId) =>
        db.Set<CategoryRelationship>().AsNoTracking().SingleAsync(r => r.CategoryRelationshipId == relationshipId);
}
