using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Relationships;

[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipRestoreCollisionTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RestoringAMutualPairThatIsActiveAgain_FailsAsADuplicate_AndCreatesNoSecondActiveRow()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var originalId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, lower.CategoryId, higher.CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var original = await ReadAsync(db, originalId);
        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(originalId, original.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, lower.CategoryId, higher.CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var deleted = await ReadAsync(db, originalId);
        var act = () => writePort.RestoreAsync(
            new RestoreRelationshipCommand(originalId, deleted.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RelationshipDuplicate);

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync(r => !r.IsDeleted)).Should().Be(1);
        (await ReadAsync(db, originalId)).IsDeleted.Should().BeTrue("the collided restore must leave the row soft-deleted");
    }

    [Fact]
    public async Task RestoringADirectionalEdgeThatIsActiveAgain_FailsAsADuplicate()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 2);

        var originalId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.BroaderNarrower, endpoints[0].CategoryId, endpoints[1].CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var original = await ReadAsync(db, originalId);
        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(originalId, original.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.BroaderNarrower, endpoints[0].CategoryId, endpoints[1].CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var deleted = await ReadAsync(db, originalId);
        var act = () => writePort.RestoreAsync(
            new RestoreRelationshipCommand(originalId, deleted.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RelationshipDuplicate);

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync(r => !r.IsDeleted)).Should().Be(1);
    }

    [Fact]
    public async Task RestoringAnEdgeWhoseReverseBecameActive_FailsAsACycle()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 2);

        var forwardId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.BroaderNarrower, endpoints[0].CategoryId, endpoints[1].CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var forward = await ReadAsync(db, forwardId);
        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(forwardId, forward.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.BroaderNarrower, endpoints[1].CategoryId, endpoints[0].CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var deleted = await ReadAsync(db, forwardId);
        var act = () => writePort.RestoreAsync(
            new RestoreRelationshipCommand(forwardId, deleted.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RelationshipCycle);
    }

    private static Task<CategoryRelationship> ReadAsync(QuranDashboardDbContext db, Guid relationshipId) =>
        db.Set<CategoryRelationship>().AsNoTracking().SingleAsync(r => r.CategoryRelationshipId == relationshipId);
}
