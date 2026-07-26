using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Templates;

[Collection(nameof(AbwabDbCollection))]
public sealed class TemplateOrderingTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AValidReparent_AppendsToTheDestinationSiblings_AndClosesTheGapItLeftBehind()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, roots) = await SiblingsAsync(count: 3);
        var moved = await ReloadAsync(db, roots[0].TemplateNodeId);

        await writePort.ReparentTemplateNodeAsync(
            new ReparentTemplateNodeCommand(
                moved.TemplateNodeId, roots[2].TemplateNodeId, moved.Version, template.TemplateRevision,
                ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await ReloadAsync(db, moved.TemplateNodeId)).SiblingOrder.Should().Be(0, "it is the first child of its new parent");
        (await SiblingOrdersAsync(db, template.DoorTemplateId, parentTemplateNodeId: null)).Should().Equal([0, 1]);
        (await CurrentTemplateRevisionAsync(db, template.DoorTemplateId)).Should().Be(template.TemplateRevision + 1,
            "a reparent rewrites the moved row AND the vacated siblings, yet stays ONE grouped operation");
    }

    [Fact]
    public async Task AGroupedReorder_RewritesEverySiblingOrder_AndBumpsTemplateRevisionExactlyOnce()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, roots) = await SiblingsAsync(count: 3);

        await writePort.ReorderTemplateNodesAsync(
            new ReorderTemplateNodesCommand(
                template.DoorTemplateId,
                null,
                [roots[2].TemplateNodeId, roots[0].TemplateNodeId, roots[1].TemplateNodeId],
                template.TemplateRevision,
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);

        (await ReloadAsync(db, roots[2].TemplateNodeId)).SiblingOrder.Should().Be(0);
        (await ReloadAsync(db, roots[0].TemplateNodeId)).SiblingOrder.Should().Be(1);
        (await ReloadAsync(db, roots[1].TemplateNodeId)).SiblingOrder.Should().Be(2);

        (await CurrentTemplateRevisionAsync(db, template.DoorTemplateId)).Should().Be(template.TemplateRevision + 1,
            "one grouped operation bumps TemplateRevision exactly once, however many siblings it rewrote");
    }

    [Fact]
    public async Task AddingANode_BumpsTemplateRevisionExactlyOnce_AndAppendsAfterItsSiblings()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await SiblingsAsync(count: 2);

        var addedId = await writePort.AddTemplateNodeAsync(
            new AddTemplateNodeCommand(
                template.DoorTemplateId, null, "عقدة جديدة", null, null, template.TemplateRevision,
                ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await ReloadAsync(db, addedId)).SiblingOrder.Should().Be(2);
        (await CurrentTemplateRevisionAsync(db, template.DoorTemplateId)).Should().Be(template.TemplateRevision + 1);
    }

    private async Task<(DoorTemplate Template, IReadOnlyList<TemplateNode> Roots)> SiblingsAsync(int count)
    {
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: count, depth: 0);
        return (template, nodes);
    }

    private static async Task<TemplateNode> ReloadAsync(QuranDashboardDbContext db, Guid nodeId) =>
        await db.Set<TemplateNode>().AsNoTracking().SingleAsync(n => n.TemplateNodeId == nodeId);

    private static async Task<IReadOnlyList<int>> SiblingOrdersAsync(QuranDashboardDbContext db, Guid templateId, Guid? parentTemplateNodeId) =>
        await db.Set<TemplateNode>().AsNoTracking()
            .Where(n => n.DoorTemplateId == templateId && n.ParentTemplateNodeId == parentTemplateNodeId && !n.IsDeleted)
            .OrderBy(n => n.SiblingOrder)
            .Select(n => n.SiblingOrder)
            .ToListAsync();

    private static async Task<long> CurrentTemplateRevisionAsync(QuranDashboardDbContext db, Guid templateId) =>
        (await db.Set<DoorTemplate>().AsNoTracking().SingleAsync(t => t.DoorTemplateId == templateId)).TemplateRevision;
}
