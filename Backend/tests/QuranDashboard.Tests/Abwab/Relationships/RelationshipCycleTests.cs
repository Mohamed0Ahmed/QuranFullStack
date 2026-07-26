using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Relationships;

[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipCycleTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AnEdgeClosingALongerCycle_IsRejectedUnderTheTransaction()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 3);

        await writePort.AddAsync(Edge(endpoints[0], endpoints[1]), CancellationToken.None);
        await writePort.AddAsync(Edge(endpoints[1], endpoints[2]), CancellationToken.None);

        var act = () => writePort.AddAsync(Edge(endpoints[2], endpoints[0]), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RelationshipCycle);

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync(r => !r.IsDeleted)).Should().Be(2);
    }

    [Fact]
    public async Task AnEdgeClosingATwoNodeCycle_IsRejected()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 2);

        await writePort.AddAsync(Edge(endpoints[0], endpoints[1]), CancellationToken.None);

        var act = () => writePort.AddAsync(Edge(endpoints[1], endpoints[0]), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RelationshipCycle);
    }

    [Fact]
    public async Task AnExplicitDirectEdge_IsAllowedAlongsideTheTransitivePath()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 3);

        await writePort.AddAsync(Edge(endpoints[0], endpoints[1]), CancellationToken.None);
        await writePort.AddAsync(Edge(endpoints[1], endpoints[2]), CancellationToken.None);

        await writePort.AddAsync(Edge(endpoints[0], endpoints[2]), CancellationToken.None);

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync(r => !r.IsDeleted)).Should().Be(3);
    }

    [Fact]
    public async Task AnEditThatRepointsAnEdgeIntoACycle_IsRejected()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 3);

        await writePort.AddAsync(Edge(endpoints[0], endpoints[1]), CancellationToken.None);
        var secondId = await writePort.AddAsync(Edge(endpoints[1], endpoints[2]), CancellationToken.None);
        var second = await db.Set<CategoryRelationship>().AsNoTracking().SingleAsync(r => r.CategoryRelationshipId == secondId);

        var act = () => writePort.EditAsync(
            new EditRelationshipCommand(
                secondId,
                RelationshipType.BroaderNarrower,
                endpoints[1].CategoryId,
                endpoints[0].CategoryId,
                second.Version,
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RelationshipCycle);
    }

    [Fact]
    public async Task ADeletedEdge_DoesNotParticipateInCycleValidation()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 2);

        var edgeId = await writePort.AddAsync(Edge(endpoints[0], endpoints[1]), CancellationToken.None);
        var edge = await db.Set<CategoryRelationship>().AsNoTracking().SingleAsync(r => r.CategoryRelationshipId == edgeId);
        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(edgeId, edge.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await writePort.AddAsync(Edge(endpoints[1], endpoints[0]), CancellationToken.None);

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync(r => !r.IsDeleted)).Should().Be(1);
    }

    private static AddRelationshipCommand Edge(Category broader, Category narrower) =>
        new(RelationshipType.BroaderNarrower, broader.CategoryId, narrower.CategoryId, ExpectedTimelineGeneration.Of(0), "tester");
}
