using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Infrastructure.Abwab.Restore;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.RestoreAdapters;

[Collection(nameof(AbwabDbCollection))]
public sealed class RelationshipRoundTripTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly RelationshipRestoreAdapter Adapter = new();

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(RelationshipType.Similar)]
    [InlineData(RelationshipType.Opposite)]
    public async Task TheAdapter_RoundTripsAMutualPair_ToProductStateEquality(RelationshipType relationshipType)
    {
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);
        var written = AbwabRelationshipTemplateSeeding.NewMutualRelationship(lower.CategoryId, higher.CategoryId, relationshipType);
        await AbwabTreeSeeding.InsertAsync(fixture, written);

        var reloaded = await ReloadAsync(written.CategoryRelationshipId);

        var reconstructed = Adapter.Reconstruct(Adapter.Capture(reloaded));

        reconstructed.Should().BeEquivalentTo(reloaded, options => options.Excluding(r => r.Version));
    }

    [Fact]
    public async Task TheAdapter_RoundTripsADirectionalEdge_ToProductStateEquality()
    {
        var (broader, narrower) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);
        var written = AbwabRelationshipTemplateSeeding.NewDirectionalRelationship(broader.CategoryId, narrower.CategoryId);
        await AbwabTreeSeeding.InsertAsync(fixture, written);

        var reloaded = await ReloadAsync(written.CategoryRelationshipId);

        var reconstructed = Adapter.Reconstruct(Adapter.Capture(reloaded));

        reconstructed.Should().BeEquivalentTo(reloaded, options => options.Excluding(r => r.Version));
    }

    [Fact]
    public async Task TheAdapter_RoundTripsTheSoftDeletedState_PreservingIdentityAndHistory()
    {
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);
        var written = AbwabRelationshipTemplateSeeding.NewMutualRelationship(lower.CategoryId, higher.CategoryId);
        written.IsDeleted = true;
        written.DeletedAtUtc = DateTimeOffset.UnixEpoch.AddHours(5);
        await AbwabTreeSeeding.InsertAsync(fixture, written);

        var reloaded = await ReloadAsync(written.CategoryRelationshipId);

        var reconstructed = Adapter.Reconstruct(Adapter.Capture(reloaded));

        reconstructed.IsDeleted.Should().BeTrue();
        reconstructed.DeletedAtUtc.Should().Be(reloaded.DeletedAtUtc);
        reconstructed.CategoryRelationshipId.Should().Be(written.CategoryRelationshipId);
    }

    [Fact]
    public void TheSnapshot_IsVersionedAndExcludesTechnicalState()
    {
        Adapter.PersistedType.Should().Be("Relationship");
        Adapter.SnapshotSchemaVersion.Should().Be(RelationshipRestoreAdapter.Schema);

        var snapshotProperties = typeof(RelationshipRestoreSnapshot).GetProperties().Select(p => p.Name).ToList();

        snapshotProperties.Should().Contain("SchemaVersion");
        snapshotProperties.Should().NotContain(["Version", "TreeRevision", "TemplateRevision"],
            "snapshots hold product state only — xmin, logical counters, cache state and realtime cursors are never inverse-restored");
    }

    [Fact]
    public async Task ReconstructingASnapshotOntoAnActiveDuplicate_Fails_RatherThanPersistingASecondActiveRow()
    {
        var (lower, higher) = await AbwabRelationshipTemplateSeeding.TwoCategoryEndpointsAsync(fixture);
        var deleted = AbwabRelationshipTemplateSeeding.NewMutualRelationship(lower.CategoryId, higher.CategoryId);
        deleted.IsDeleted = true;
        deleted.DeletedAtUtc = DateTimeOffset.UnixEpoch;
        await AbwabTreeSeeding.InsertAsync(fixture, deleted);
        await AbwabTreeSeeding.InsertAsync(fixture, AbwabRelationshipTemplateSeeding.NewMutualRelationship(lower.CategoryId, higher.CategoryId));

        var snapshot = Adapter.Capture(await ReloadAsync(deleted.CategoryRelationshipId)) with { IsDeleted = false, DeletedAtUtc = null };
        var reconstructed = Adapter.Reconstruct(snapshot);

        await using var db = AbwabKernelHarness.CreateProductionContext(fixture);
        var tracked = await db.Set<CategoryRelationship>().SingleAsync(r => r.CategoryRelationshipId == deleted.CategoryRelationshipId);
        tracked.IsDeleted = reconstructed.IsDeleted;
        tracked.DeletedAtUtc = reconstructed.DeletedAtUtc;
        db.AbwabChangeSets.Add(AbwabTreeSeeding.NewChangeSet());

        var act = () => db.SaveChangesAsync();

        var thrown = await act.Should().ThrowAsync<DbUpdateException>();
        thrown.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be("23505", "a reconstruction that would duplicate an active relationship must fail, not persist");
    }

    private async Task<CategoryRelationship> ReloadAsync(Guid relationshipId)
    {
        await using var db = SecurityTestHarness.CreateContext(fixture);
        return await db.Set<CategoryRelationship>().AsNoTracking().SingleAsync(r => r.CategoryRelationshipId == relationshipId);
    }
}
