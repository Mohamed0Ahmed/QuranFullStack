using QuranDashboard.Infrastructure.Abwab.Restore;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.RestoreAdapters;

[Collection(nameof(AbwabDbCollection))]
public sealed class DoorTemplateRoundTripTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DoorTemplateRestoreAdapter Adapter = new();

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TheOneAggregateAdapter_RoundTripsTemplateNodeTreeAndAliasHistory_ToProductStateEquality()
    {
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 2);
        var activeAlias = AbwabRelationshipTemplateSeeding.NewTemplateNodeAlias(nodes[0].TemplateNodeId, "مرادف فعّال");
        var removedAlias = AbwabRelationshipTemplateSeeding.NewTemplateNodeAlias(nodes[0].TemplateNodeId, "مرادف مُزال");
        removedAlias.IsDeleted = true;
        removedAlias.DeletedAtUtc = DateTimeOffset.UnixEpoch.AddHours(2);
        await AbwabTreeSeeding.InsertAsync(fixture, activeAlias, removedAlias);

        var aggregate = await AbwabRelationshipTemplateSeeding.LoadTemplateAggregateAsync(fixture, template.DoorTemplateId);

        var reconstructed = Adapter.Reconstruct(Adapter.Capture(aggregate));

        reconstructed.Should().BeEquivalentTo(aggregate, options => options
            .Excluding(memberInfo => memberInfo.Name == "Version" || memberInfo.Name == "TemplateRevision"));
    }

    [Fact]
    public async Task TheAdapter_RoundTripsTheAggregateSoftDeleteState()
    {
        var template = AbwabRelationshipTemplateSeeding.NewDoorTemplate("قالب محذوف");
        template.IsDeleted = true;
        template.DeletedAtUtc = DateTimeOffset.UnixEpoch.AddDays(1);
        await AbwabTreeSeeding.InsertAsync(fixture, template);

        var aggregate = await AbwabRelationshipTemplateSeeding.LoadTemplateAggregateAsync(fixture, template.DoorTemplateId);

        var reconstructed = Adapter.Reconstruct(Adapter.Capture(aggregate));

        reconstructed.Template.IsDeleted.Should().BeTrue();
        reconstructed.Template.DeletedAtUtc.Should().Be(aggregate.Template.DeletedAtUtc);
    }

    [Fact]
    public async Task TheSnapshot_PreservesExplicitSiblingOrderAndParentLinks()
    {
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 3, depth: 1);
        var aggregate = await AbwabRelationshipTemplateSeeding.LoadTemplateAggregateAsync(fixture, template.DoorTemplateId);

        var snapshot = Adapter.Capture(aggregate);

        snapshot.Nodes.Should().HaveCount(nodes.Count);
        snapshot.Nodes.Where(n => n.ParentTemplateNodeId is null).Select(n => n.SiblingOrder).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void TheSnapshot_IsVersionedAndExcludesTechnicalState()
    {
        Adapter.PersistedType.Should().Be("DoorTemplate");
        Adapter.SnapshotSchemaVersion.Should().Be(DoorTemplateRestoreAdapter.Schema);

        typeof(DoorTemplateRestoreSnapshot).GetProperties().Select(p => p.Name).Should().Contain("SchemaVersion");

        // Every record in the aggregate snapshot, not just the outer one: a nested node or alias
        // carrying xmin or a logical counter would inverse-restore technical state all the same.
        var snapshotRecords = new[]
        {
            typeof(DoorTemplateRestoreSnapshot),
            typeof(TemplateNodeRestoreSnapshot),
            typeof(TemplateNodeAliasRestoreSnapshot),
        };

        foreach (var record in snapshotRecords)
        {
            record.GetProperties().Select(p => p.Name).Should().NotContain(
                ["Version", "TemplateRevision", "TreeRevision"],
                $"{record.Name} holds product state only — xmin, logical counters, cache state and realtime "
                + "cursors are never inverse-restored");
        }
    }
}
