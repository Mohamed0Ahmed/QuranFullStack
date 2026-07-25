using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Relationships;

[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipShapeConstraintTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string CheckViolation = "23514";

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AMutualRowAndADirectionalRow_BothPersist_WhenEachCarriesExactlyOneShape()
    {
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var mutual = AbwabRelationshipTemplateSeeding.NewMutualRelationship(lower.CategoryId, higher.CategoryId);
        var directional = AbwabRelationshipTemplateSeeding.NewDirectionalRelationship(lower.CategoryId, higher.CategoryId);

        await AbwabTreeSeeding.InsertAsync(fixture, mutual, directional);

        await using var db = SecurityTestHarness.CreateContext(fixture);
        var persisted = await db.Set<CategoryRelationship>().AsNoTracking().ToListAsync();

        persisted.Should().HaveCount(2);
        persisted.Should().OnlyContain(row => row.HasValidShape());
    }

    [Fact]
    public async Task ARowCarryingBothShapes_IsRejectedByTheOneShapeCheck()
    {
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var bothShapes = AbwabRelationshipTemplateSeeding.NewMutualRelationship(lower.CategoryId, higher.CategoryId);
        bothShapes.SourceCategoryId = lower.CategoryId;
        bothShapes.TargetCategoryId = higher.CategoryId;

        var violation = await ExpectCheckViolationAsync(bothShapes);

        violation.ConstraintName.Should().Contain("one_shape");
    }

    [Fact]
    public async Task ARowCarryingNeitherShape_IsRejectedByTheOneShapeCheck()
    {
        await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var neitherShape = new CategoryRelationship
        {
            CategoryRelationshipId = Guid.NewGuid(),
            RelationshipType = RelationshipType.Similar,
        };

        var violation = await ExpectCheckViolationAsync(neitherShape);

        violation.ConstraintName.Should().Contain("one_shape");
    }

    // The shape is bound to the TYPE, not merely mutually exclusive: a mutual type stored in the
    // directional columns would otherwise join the Broader/Narrower graph that cycle validation
    // walks, and a directional type stored in the mutual columns would escape the directional index.
    [Theory]
    [InlineData(RelationshipType.Similar)]
    [InlineData(RelationshipType.Opposite)]
    public async Task AMutualTypeStoredInTheDirectionalColumns_IsRejectedByTheOneShapeCheck(RelationshipType mutualType)
    {
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var mistyped = AbwabRelationshipTemplateSeeding.NewDirectionalRelationship(lower.CategoryId, higher.CategoryId);
        mistyped.RelationshipType = mutualType;

        var violation = await ExpectCheckViolationAsync(mistyped);

        violation.ConstraintName.Should().Contain("one_shape");
    }

    [Fact]
    public async Task TheDirectionalTypeStoredInTheMutualColumns_IsRejectedByTheOneShapeCheck()
    {
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var mistyped = AbwabRelationshipTemplateSeeding.NewMutualRelationship(
            lower.CategoryId, higher.CategoryId, RelationshipType.BroaderNarrower);

        var violation = await ExpectCheckViolationAsync(mistyped);

        violation.ConstraintName.Should().Contain("one_shape");
    }

    [Fact]
    public async Task AMutualRowStoredInNonCanonicalOrder_IsRejectedByTheCanonicalOrderCheck()
    {
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var reversed = AbwabRelationshipTemplateSeeding.NewMutualRelationship(higher.CategoryId, lower.CategoryId);

        var violation = await ExpectCheckViolationAsync(reversed);

        violation.ConstraintName.Should().Contain("canonical_order");
    }

    [Fact]
    public async Task AMutualSelfLink_IsRejected()
    {
        var (lower, _) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var selfLink = AbwabRelationshipTemplateSeeding.NewMutualRelationship(lower.CategoryId, lower.CategoryId);

        var violation = await ExpectCheckViolationAsync(selfLink);

        violation.SqlState.Should().Be(CheckViolation);
    }

    [Fact]
    public async Task ADirectionalSelfLink_IsRejectedByTheNoSelfLinkCheck()
    {
        var (lower, _) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);

        var selfLink = AbwabRelationshipTemplateSeeding.NewDirectionalRelationship(lower.CategoryId, lower.CategoryId);

        var violation = await ExpectCheckViolationAsync(selfLink);

        violation.ConstraintName.Should().Contain("no_self_link");
    }

    private async Task<PostgresException> ExpectCheckViolationAsync(CategoryRelationship relationship)
    {
        var act = () => AbwabTreeSeeding.InsertAsync(fixture, relationship);

        var thrown = await act.Should().ThrowAsync<DbUpdateException>();
        var postgres = thrown.Which.InnerException.Should().BeOfType<PostgresException>().Which;
        postgres.SqlState.Should().Be(CheckViolation);
        return postgres;
    }
}
