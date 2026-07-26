using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Relationships;

[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipDuplicateTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(RelationshipType.Similar)]
    [InlineData(RelationshipType.Opposite)]
    public async Task ASecondActiveMutualPairOfTheSameType_IsRejectedAsADuplicate(RelationshipType relationshipType)
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        await writePort.AddAsync(Add(relationshipType, lower.CategoryId, higher.CategoryId), CancellationToken.None);

        var act = () => writePort.AddAsync(Add(relationshipType, lower.CategoryId, higher.CategoryId), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RelationshipDuplicate);

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync(r => !r.IsDeleted)).Should().Be(1);
    }

    [Fact]
    public async Task AReverseMutualPair_CollapsesOntoTheSameCanonicalKey_AndIsRejectedAsADuplicate()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        await writePort.AddAsync(Add(RelationshipType.Similar, lower.CategoryId, higher.CategoryId), CancellationToken.None);

        var act = () => writePort.AddAsync(Add(RelationshipType.Similar, higher.CategoryId, lower.CategoryId), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RelationshipDuplicate);

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync(r => !r.IsDeleted)).Should().Be(1);
    }

    [Fact]
    public async Task TheSameMutualPairUnderADifferentType_IsAllowed()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        await writePort.AddAsync(Add(RelationshipType.Similar, lower.CategoryId, higher.CategoryId), CancellationToken.None);
        await writePort.AddAsync(Add(RelationshipType.Opposite, lower.CategoryId, higher.CategoryId), CancellationToken.None);

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync(r => !r.IsDeleted)).Should().Be(2);
    }

    [Fact]
    public async Task ASecondActiveDirectionalEdgeOnTheSameOrderedPair_IsRejectedAsADuplicate()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (broader, narrower) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        await writePort.AddAsync(Add(RelationshipType.BroaderNarrower, broader.CategoryId, narrower.CategoryId), CancellationToken.None);

        var act = () => writePort.AddAsync(Add(RelationshipType.BroaderNarrower, broader.CategoryId, narrower.CategoryId), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RelationshipDuplicate);

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync(r => !r.IsDeleted)).Should().Be(1);
    }

    [Fact]
    public async Task AnEditThatCollidesWithAnotherActiveRow_IsRejectedAsADuplicate()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        await writePort.AddAsync(Add(RelationshipType.Similar, lower.CategoryId, higher.CategoryId), CancellationToken.None);
        var oppositeId = await writePort.AddAsync(Add(RelationshipType.Opposite, lower.CategoryId, higher.CategoryId), CancellationToken.None);
        var opposite = await db.Set<CategoryRelationship>().AsNoTracking().SingleAsync(r => r.CategoryRelationshipId == oppositeId);

        var act = () => writePort.EditAsync(
            new EditRelationshipCommand(
                oppositeId,
                RelationshipType.Similar,
                lower.CategoryId,
                higher.CategoryId,
                opposite.Version,
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RelationshipDuplicate);

        var unchanged = await db.Set<CategoryRelationship>().AsNoTracking().SingleAsync(r => r.CategoryRelationshipId == oppositeId);
        unchanged.RelationshipType.Should().Be(RelationshipType.Opposite);
    }

    private static AddRelationshipCommand Add(RelationshipType relationshipType, Guid first, Guid second) =>
        new(relationshipType, first, second, ExpectedTimelineGeneration.Of(0), "tester");
}
