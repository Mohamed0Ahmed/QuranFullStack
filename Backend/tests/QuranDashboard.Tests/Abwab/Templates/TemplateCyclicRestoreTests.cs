using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Infrastructure.Abwab.Restore;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Templates;

[Collection(nameof(AbwabDbCollection))]
public sealed class TemplateCyclicRestoreTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DoorTemplateRestoreAdapter Adapter = new();

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    // §7.4: no cyclic template can be saved, applied, RENDERED, or RESTORED — the reconstruct path
    // respects the writer's invariant instead of persisting an unreachable node ring.
    [Fact]
    public async Task ReconstructingASnapshotWhoseParentLinksFormACycle_IsRejected()
    {
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 2);
        var snapshot = Adapter.Capture(await LoadAggregateAsync(template.DoorTemplateId));

        var cyclic = snapshot with
        {
            Nodes =
            [
                snapshot.Nodes[0] with { ParentTemplateNodeId = nodes[2].TemplateNodeId },
                .. snapshot.Nodes.Skip(1),
            ],
        };

        var act = () => Adapter.Reconstruct(cyclic);

        act.Should().Throw<AbwabWriteConflictException>()
            .Which.Code.Should().Be(AbwabConflictCodes.TemplateCycle,
                "the restore path refuses a cyclic template through the same §11 code as the writer, not an untyped message");
    }

    [Fact]
    public async Task ReconstructingASnapshotWhoseParentLinksAreAcyclic_Succeeds()
    {
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 2);
        var snapshot = Adapter.Capture(await LoadAggregateAsync(template.DoorTemplateId));

        var reconstructed = Adapter.Reconstruct(snapshot);

        reconstructed.Nodes.Should().HaveCount(snapshot.Nodes.Count);
    }

    private Task<DoorTemplateAggregate> LoadAggregateAsync(Guid templateId) =>
        AbwabRelationshipTemplateSeeding.LoadTemplateAggregateAsync(fixture, templateId);
}
