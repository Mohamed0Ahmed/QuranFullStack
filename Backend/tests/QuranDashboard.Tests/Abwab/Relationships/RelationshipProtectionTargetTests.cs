using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Relationships;

[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipProtectionTargetTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(ManualProtectionScope.CategoryOnly)]
    [InlineData(ManualProtectionScope.Subtree)]
    public async Task Add_IsBlockedWhenAProposedEndpointCarriesDirectRelationshipProtection(ManualProtectionScope scope)
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (protectedCategory, _) = await AbwabRelationshipTemplateSeeding.ProtectedCategoryAsync(
            fixture, ManualProtectionType.Relationship, scope);
        var other = (await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 1))[0];

        var act = () => writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, protectedCategory.CategoryId, other.CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await ExpectManualProtectionAsync(act);
        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Add_IsBlockedWhenAProposedEndpointInheritsRelationshipProtectionFromASubtreeScopedAncestor()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var chain = await AbwabRelationshipTemplateSeeding.DeepCategoryChainAsync(fixture, depth: 2);
        await AbwabTreeSeeding.InsertAsync(
            fixture,
            AbwabTreeSeeding.NewManualProtection(chain[0].CategoryId, ManualProtectionType.Relationship, ManualProtectionScope.Subtree));
        var other = (await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 1))[0];

        var act = () => writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, chain[2].CategoryId, other.CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await ExpectManualProtectionAsync(act);
        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Add_IsAllowedWhenAnEndpointCarriesADifferentProtectionType()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var (categoryDataProtected, _) = await AbwabRelationshipTemplateSeeding.ProtectedCategoryAsync(
            fixture, ManualProtectionType.CategoryData);
        var other = (await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 1))[0];

        await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, categoryDataProtected.CategoryId, other.CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await db.Set<CategoryRelationship>().AsNoTracking().CountAsync(r => !r.IsDeleted)).Should().Be(1);
    }

    [Fact]
    public async Task Edit_IsBlockedWhenTheProtectedOldEndpointIsReplacedByAnUnprotectedNewOne()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 3);

        var relationshipId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, endpoints[0].CategoryId, endpoints[1].CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var relationship = await ReadAsync(db, relationshipId);

        await ProtectAsync(endpoints[1]);

        var act = () => writePort.EditAsync(
            new EditRelationshipCommand(
                relationshipId, RelationshipType.Similar, endpoints[0].CategoryId, endpoints[2].CategoryId, relationship.Version,
                ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await ExpectManualProtectionAsync(act);

        var unchanged = await ReadAsync(db, relationshipId);
        new[] { unchanged.LowerCategoryId, unchanged.HigherCategoryId }.Should().BeEquivalentTo(
            new Guid?[] { endpoints[0].CategoryId, endpoints[1].CategoryId },
            "an edit cannot escape protection by dropping the protected endpoint");
    }

    [Fact]
    public async Task Edit_IsBlockedWhenTheProposedNewEndpointIsProtected()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 3);

        var relationshipId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, endpoints[0].CategoryId, endpoints[1].CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var relationship = await ReadAsync(db, relationshipId);

        await ProtectAsync(endpoints[2]);

        var act = () => writePort.EditAsync(
            new EditRelationshipCommand(
                relationshipId, RelationshipType.Similar, endpoints[0].CategoryId, endpoints[2].CategoryId, relationship.Version,
                ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await ExpectManualProtectionAsync(act);
    }

    [Fact]
    public async Task DeleteAndRestore_AreBlockedWhenAStoredEndpointIsProtected()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateRelationshipWritePort(fixture);
        await using var _ = db;
        var endpoints = await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, 2);

        var relationshipId = await writePort.AddAsync(
            new AddRelationshipCommand(RelationshipType.Similar, endpoints[0].CategoryId, endpoints[1].CategoryId, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var active = await ReadAsync(db, relationshipId);

        var protection = AbwabTreeSeeding.NewManualProtection(
            endpoints[1].CategoryId, ManualProtectionType.Relationship, ManualProtectionScope.CategoryOnly);
        await AbwabTreeSeeding.InsertAsync(fixture, protection);

        var blockedDelete = () => writePort.DeleteAsync(
            new DeleteRelationshipCommand(relationshipId, active.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        await ExpectManualProtectionAsync(blockedDelete);
        (await ReadAsync(db, relationshipId)).IsDeleted.Should().BeFalse();

        await LiftAsync(protection.ManualProtectionId);
        await writePort.DeleteAsync(
            new DeleteRelationshipCommand(relationshipId, active.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var deleted = await ReadAsync(db, relationshipId);
        await AbwabTreeSeeding.InsertAsync(
            fixture,
            AbwabTreeSeeding.NewManualProtection(endpoints[1].CategoryId, ManualProtectionType.Relationship, ManualProtectionScope.CategoryOnly));

        var blockedRestore = () => writePort.RestoreAsync(
            new RestoreRelationshipCommand(relationshipId, deleted.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        await ExpectManualProtectionAsync(blockedRestore);
        (await ReadAsync(db, relationshipId)).IsDeleted.Should().BeTrue();
    }

    private Task ProtectAsync(Category category) => AbwabTreeSeeding.InsertAsync(
        fixture,
        AbwabTreeSeeding.NewManualProtection(category.CategoryId, ManualProtectionType.Relationship, ManualProtectionScope.CategoryOnly));

    private async Task LiftAsync(Guid manualProtectionId)
    {
        await using var db = SecurityTestHarness.CreateContext(fixture);
        var protection = await db.AbwabManualProtections.SingleAsync(p => p.ManualProtectionId == manualProtectionId);
        protection.IsDeleted = true;
        protection.DeletedAtUtc = DateTimeOffset.UnixEpoch;
        db.AbwabChangeSets.Add(AbwabTreeSeeding.NewChangeSet());
        await db.SaveChangesAsync();
    }

    private static async Task ExpectManualProtectionAsync(Func<Task> act) =>
        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.ManualProtection);

    private static Task<CategoryRelationship> ReadAsync(QuranDashboardDbContext db, Guid relationshipId) =>
        db.Set<CategoryRelationship>().AsNoTracking().SingleAsync(r => r.CategoryRelationshipId == relationshipId);
}
